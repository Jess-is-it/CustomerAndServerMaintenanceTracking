using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading; // For System.Threading.Timer
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System.Diagnostics; // For Console.WriteLine
using System.Collections.Concurrent;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace ServerMaintenanceService.Services
{
    public class NetwatchJob : IDisposable
    {
        private NetwatchConfig _config;
        private Timer _schedulerTimer; // Ticks frequently to check which IPs to ping
        private readonly ConcurrentDictionary<string, MonitoredIpState> _ipStates = new ConcurrentDictionary<string, MonitoredIpState>();
        private readonly SemaphoreSlim _pingConcurrencySemaphore;
        private Timer _aggregateStatusUpdateTimer; // Timer to periodically update the aggregate status to DB

        private bool _isDisposed = false;
        private volatile bool _isRefreshingIpList = false;
        private volatile bool _isUpdatingAggregateStatus = false;

        private readonly TagRepository _tagRepository;
        private readonly NetwatchConfigRepository _netwatchConfigRepository;
        private readonly ServiceLogRepository _logRepository; // For logging
        private readonly string _serviceNameForLogging;


        private string _lastCalculatedAggregateStatusForThisJobInstance = null;
        private DateTime _lastTimeAggregateStatusWasWrittenToDb = DateTime.MinValue;
        private DateTime _lastActivityTimestampForAggregate = DateTime.MinValue;


        public int ConfigId => _config.Id;
        public string NetwatchName => _config?.NetwatchName ?? "Unknown Netwatch";

        public NetwatchJob(NetwatchConfig config, TagRepository tagRepository, NetwatchConfigRepository netwatchConfigRepository, ServiceLogRepository logRepository, string serviceName)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _netwatchConfigRepository = netwatchConfigRepository ?? throw new ArgumentNullException(nameof(netwatchConfigRepository));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _serviceNameForLogging = serviceName ?? "NetwatchJob";

            if (_config.IntervalSeconds <= 0)
            {
                Log(LogLevel.WARN, $"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) has an invalid interval of {_config.IntervalSeconds}s. Defaulting to 60s.");
                _config.IntervalSeconds = 60; // Default to 60s if invalid
            }
            if (_config.TimeoutMilliseconds <= 0)
            {
                Log(LogLevel.WARN, $"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) has an invalid timeout of {_config.TimeoutMilliseconds}ms. Defaulting to 1000ms.");
                _config.TimeoutMilliseconds = 1000; // Default to 1s if invalid
            }

            _pingConcurrencySemaphore = new SemaphoreSlim(10, 10); // Limit to 10 concurrent pings per job
        }

        private void Log(LogLevel level, string message, Exception ex = null)
        {
            string logMessage = $"[NJ {_config.Id} '{_config.NetwatchName}'] {message}";
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_serviceNameForLogging}] [{level}] {logMessage}{(ex != null ? " - Exception: " + ex.Message : "")}");
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = _serviceNameForLogging,
                LogLevel = level.ToString(),
                Message = logMessage,
                ExceptionDetails = ex?.ToString()
            });
        }

        public async Task StartAsync()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(NetwatchJob));

            Log(LogLevel.INFO, $"Starting job. Interval: {_config.IntervalSeconds}s, Timeout: {_config.TimeoutMilliseconds}ms.");

            await RefreshMonitoredIpListAsync();

            _schedulerTimer = new Timer(async _ => await SchedulerTimerCallbackAsync(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));

            int aggregateUpdateIntervalSeconds = Math.Max(5, _config.IntervalSeconds / 2);
            if (_config.IntervalSeconds < 5) aggregateUpdateIntervalSeconds = 5;

            _aggregateStatusUpdateTimer = new Timer(async _ => await UpdateAggregateStatusToDatabaseAsync(), null, TimeSpan.FromSeconds(aggregateUpdateIntervalSeconds), TimeSpan.FromSeconds(aggregateUpdateIntervalSeconds));

            if (_config.RunUponSave)
            {
                Log(LogLevel.DEBUG, "RunUponSave is true, scheduling immediate pings for all IPs.");
                foreach (var ipState in _ipStates.Values)
                {
                    ipState.NextPingTime = DateTime.MinValue;
                }
                _ = Task.Delay(TimeSpan.FromSeconds(Math.Min(5, _config.IntervalSeconds + 2))).ContinueWith(async _ => await UpdateAggregateStatusToDatabaseAsync());
            }
        }

        public void Stop()
        {
            if (_isDisposed) return;
            Log(LogLevel.INFO, "Stopping job.");

            _schedulerTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _schedulerTimer?.Dispose();
            _schedulerTimer = null;

            _aggregateStatusUpdateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _aggregateStatusUpdateTimer?.Dispose();
            _aggregateStatusUpdateTimer = null;

            _pingConcurrencySemaphore?.Dispose();
        }

        public async Task UpdateConfigAsync(NetwatchConfig newConfig)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(NetwatchJob));
            if (newConfig == null) throw new ArgumentNullException(nameof(newConfig));
            if (newConfig.Id != _config.Id) throw new ArgumentException("Cannot update job with a different config ID.");

            Log(LogLevel.DEBUG, $"Configuration update received.");

            bool intervalChanged = _config.IntervalSeconds != newConfig.IntervalSeconds;
            bool timeoutChanged = _config.TimeoutMilliseconds != newConfig.TimeoutMilliseconds;
            // Ensure MonitoredTagIds are not null before comparing
            var oldTags = _config.MonitoredTagIds ?? new List<int>();
            var newTags = newConfig.MonitoredTagIds ?? new List<int>();
            bool tagsChanged = !oldTags.SequenceEqual(newTags);
            bool enabledChanged = _config.IsEnabled != newConfig.IsEnabled;

            _config = newConfig;

            if (enabledChanged)
            {
                if (_config.IsEnabled)
                {
                    Log(LogLevel.INFO, "Job re-enabled by config update. Restarting operations.");
                    await StartAsync();
                }
                else
                {
                    Log(LogLevel.INFO, "Job disabled by config update. Stopping operations.");
                    Stop();
                    try
                    {
                        _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "Disabled", DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        Log(LogLevel.ERROR, $"Failed to update status to 'Disabled' in DB.", ex);
                    }
                }
            }
            else if (_config.IsEnabled)
            {
                if (tagsChanged)
                {
                    Log(LogLevel.INFO, "Monitored tags changed. Refreshing IP list.");
                    await RefreshMonitoredIpListAsync();
                }
            }
        }

        private async Task RefreshMonitoredIpListAsync()
        {
            if (_isRefreshingIpList || _isDisposed) return;
            _isRefreshingIpList = true;

            Log(LogLevel.DEBUG, "Starting IP list refresh.");
            List<MonitoredIpDetail> newIpDetails;
            try
            {
                // Ensure _config.MonitoredTagIds is not null before passing
                newIpDetails = _tagRepository.GetMonitoredIpDetailsForTags(_config.MonitoredTagIds ?? new List<int>());
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Failed to get IP details for tags.", ex);
                _isRefreshingIpList = false;
                try { _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "Error: Failed to get IPs for tags", DateTime.Now); }
                catch (Exception dbEx) { Log(LogLevel.ERROR, "Failed to update DB status for IP fetch error.", dbEx); }
                return;
            }

            var currentIpSet = new HashSet<string>(_ipStates.Keys);

            foreach (var detail in newIpDetails)
            {
                string key = detail.IpAddress ?? detail.EntityName;
                if (_ipStates.TryGetValue(key, out MonitoredIpState existingState))
                {
                    existingState.IpDetail = detail;
                }
                else
                {
                    _ipStates.TryAdd(key, new MonitoredIpState(detail));
                    Log(LogLevel.DEBUG, $"Added new IP/Entity to monitor: {detail.EntityName} ({detail.IpAddress ?? "No IP"})");
                }
            }

            var newIpKeySet = new HashSet<string>(newIpDetails.Select(ipd => ipd.IpAddress ?? ipd.EntityName));
            var ipsToRemove = currentIpSet.Except(newIpKeySet).ToList();
            foreach (var ipKeyToRemove in ipsToRemove)
            {
                if (_ipStates.TryRemove(ipKeyToRemove, out _))
                {
                    Log(LogLevel.DEBUG, $"Removed IP/Entity from monitoring: {ipKeyToRemove}");
                }
            }
            Log(LogLevel.INFO, $"IP list refreshed. Monitoring {_ipStates.Count} targets.");

            try
            {
                _netwatchConfigRepository.PruneNetwatchDataForMissingEntities(_config.Id, newIpDetails);
                Log(LogLevel.DEBUG, "Pruning of stale NetwatchIpResults and NetwatchOutageLog completed.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Error during pruning of stale Netwatch data.", ex);
            }

            _isRefreshingIpList = false;
        }

        private async Task SchedulerTimerCallbackAsync()
        {
            if (_isDisposed || !_config.IsEnabled)
            {
                Stop(); return;
            }

            List<Task> activePingTasks = new List<Task>();
            DateTime now = DateTime.Now;

            foreach (var ipStateEntry in _ipStates)
            {
                MonitoredIpState ipState = ipStateEntry.Value;
                if (ipState.IpDetail == null) // Safety check
                {
                    Log(LogLevel.WARN, $"Scheduler: Found ipState with null IpDetail for key {ipStateEntry.Key}. Skipping.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(ipState.IpDetail.IpAddress))
                {
                    ipState.LastKnownStatus = "No IP";
                    ipState.LastPingAttemptTime = now;
                    ipState.NextPingTime = now.AddSeconds(_config.IntervalSeconds);
                    continue;
                }

                if (!ipState.IsPinging && now >= ipState.NextPingTime)
                {
                    ipState.IsPinging = true;
                    activePingTasks.Add(ExecutePingAndUpdateStateAsync(ipState));
                }
            }

            if (activePingTasks.Any())
            {
                await Task.WhenAll(activePingTasks);
            }
        }

        private async Task ExecutePingAndUpdateStateAsync(MonitoredIpState ipState)
        {
            if (ipState.IpDetail == null) // Safety check
            {
                Log(LogLevel.ERROR, "ExecutePing: IpDetail is null. Cannot proceed.");
                ipState.IsPinging = false; // Reset flag
                return;
            }

            await _pingConcurrencySemaphore.WaitAsync();
            try
            {
                DateTime pingAttemptTime = DateTime.Now;
                ipState.LastPingAttemptTime = pingAttemptTime;
                PingReply reply = null;
                string currentPingStatusText;

                if (string.IsNullOrWhiteSpace(ipState.IpDetail.IpAddress))
                {
                    currentPingStatusText = "No IP";
                }
                else
                {
                    reply = await Pinger.SendPingAsync(ipState.IpDetail.IpAddress, _config.TimeoutMilliseconds);
                    currentPingStatusText = reply?.Status.ToString() ?? "Error (No Reply)";
                }

                if (currentPingStatusText.Length > 50) currentPingStatusText = currentPingStatusText.Substring(0, 50);

                string previousStatus = ipState.LastKnownStatus;
                bool currentIsEffectivelyDown = !(reply?.Status == IPStatus.Success || currentPingStatusText == "No IP");
                bool previousWasEffectivelyDown = !(previousStatus == "Success" || previousStatus == "No IP" || previousStatus == "Pending");

                if (currentIsEffectivelyDown && !previousWasEffectivelyDown && currentPingStatusText != "No IP")
                {
                    _netwatchConfigRepository.StartOutageLog(_config.Id, ipState.IpDetail.IpAddress, ipState.IpDetail.EntityName, pingAttemptTime, currentPingStatusText);
                    Log(LogLevel.WARN, $"OUTAGE START: {ipState.IpDetail.EntityName} ({ipState.IpDetail.IpAddress}) is now {currentPingStatusText}.");
                }
                else if (!currentIsEffectivelyDown && previousWasEffectivelyDown && currentPingStatusText != "No IP")
                {
                    _netwatchConfigRepository.EndOutageLog(_config.Id, ipState.IpDetail.IpAddress, pingAttemptTime);
                    Log(LogLevel.INFO, $"OUTAGE END: {ipState.IpDetail.EntityName} ({ipState.IpDetail.IpAddress}) is now {currentPingStatusText}.");
                }

                ipState.LastKnownStatus = currentPingStatusText;

                _netwatchConfigRepository.SaveIndividualIpPingResult(_config.Id, ipState.IpDetail, reply, pingAttemptTime);

                if (pingAttemptTime > _lastActivityTimestampForAggregate)
                {
                    _lastActivityTimestampForAggregate = pingAttemptTime;
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, $"Error pinging {ipState.IpDetail.EntityName} ({ipState.IpDetail.IpAddress}).", ex);
                ipState.LastKnownStatus = "PingError";
            }
            finally
            {
                ipState.NextPingTime = DateTime.Now.AddSeconds(_config.IntervalSeconds);
                ipState.IsPinging = false;
                _pingConcurrencySemaphore.Release();
            }
        }

        private string CalculateAndFormatAggregateStatus(
            List<MonitoredIpState> currentIpStates,
            out int totalTrackedEntities,
            out int pingableEntitiesCount,
            out int upCountOutput,
            out int timeoutCountOutput,
            out int noIpCountOutput,
            out int otherErrorCountOutput)
        {
            // DIAGNOSTIC LOG ADDED HERE
            Log(LogLevel.DEBUG, $"CalculateAndFormatAggregateStatus V3_DIAG: Starting calculation for {_config.NetwatchName}. Input states count: {currentIpStates.Count}");

            totalTrackedEntities = currentIpStates.Count;
            pingableEntitiesCount = 0;
            upCountOutput = 0;
            timeoutCountOutput = 0;
            noIpCountOutput = 0;
            otherErrorCountOutput = 0;

            if (totalTrackedEntities == 0)
            {
                Log(LogLevel.DEBUG, "CalculateAndFormatAggregateStatus V3_DIAG: No entities tracked. Returning 'No entities configured/found for tags.'");
                return "No entities configured/found for tags.";
            }

            foreach (var state in currentIpStates)
            {
                if (state.IpDetail == null) // Defensive check
                {
                    Log(LogLevel.WARN, "CalculateAndFormatAggregateStatus V3_DIAG: Encountered an IpState with null IpDetail. Skipping.");
                    noIpCountOutput++; // Count it as a "No IP" or unpingable scenario for safety
                    continue;
                }

                if (string.IsNullOrWhiteSpace(state.IpDetail.IpAddress) || state.LastKnownStatus == "No IP")
                {
                    noIpCountOutput++;
                }
                else
                {
                    pingableEntitiesCount++;
                    if (state.LastKnownStatus == IPStatus.Success.ToString())
                    {
                        upCountOutput++;
                    }
                    else if (state.LastKnownStatus == IPStatus.TimedOut.ToString())
                    {
                        timeoutCountOutput++;
                    }
                    else
                    {
                        otherErrorCountOutput++;
                    }
                }
            }

            // Log counts after iteration
            Log(LogLevel.DEBUG, $"CalculateAndFormatAggregateStatus V3_DIAG: Counts - TotalTracked: {totalTrackedEntities}, Pingable: {pingableEntitiesCount}, Up: {upCountOutput}, Timeout: {timeoutCountOutput}, NoIP: {noIpCountOutput}, OtherError: {otherErrorCountOutput}");

            string statusText;

            if (pingableEntitiesCount == 0)
            {
                statusText = $"All {totalTrackedEntities} No IP";
            }
            else if (upCountOutput == pingableEntitiesCount)
            {
                statusText = $"All {upCountOutput}/{totalTrackedEntities} Up";
                if (noIpCountOutput > 0)
                {
                    statusText += $", {noIpCountOutput} No IP";
                }
            }
            else if (upCountOutput == 0)
            {
                statusText = $"All {pingableEntitiesCount}/{totalTrackedEntities} Down";
                List<string> details = new List<string>();
                if (timeoutCountOutput > 0) details.Add($"{timeoutCountOutput} Timeout");
                if (otherErrorCountOutput > 0) details.Add($"{otherErrorCountOutput} Other Error");

                if (details.Any())
                {
                    statusText += $" ({string.Join(", ", details)})";
                }
                if (noIpCountOutput > 0)
                {
                    statusText += $", {noIpCountOutput} No IP";
                }
            }
            else
            {
                statusText = $"Partial {upCountOutput}/{totalTrackedEntities} Up";

                List<string> downDetails = new List<string>();
                if (timeoutCountOutput > 0)
                {
                    downDetails.Add($"{timeoutCountOutput} Timeout");
                }

                int noIpAndOtherErrorCount = noIpCountOutput + otherErrorCountOutput;
                if (noIpAndOtherErrorCount > 0)
                {
                    downDetails.Add($"{noIpAndOtherErrorCount} No IP/Down");
                }

                if (downDetails.Any())
                {
                    statusText += ", " + string.Join(", ", downDetails);
                }
            }
            Log(LogLevel.DEBUG, $"CalculateAndFormatAggregateStatus V3_DIAG: Final status string: '{statusText}'");
            return statusText;
        }


        private async Task UpdateAggregateStatusToDatabaseAsync()
        {
            if (_isDisposed || !_config.IsEnabled || _isUpdatingAggregateStatus) return;
            _isUpdatingAggregateStatus = true;

            try
            {
                var currentStatesSnapshot = _ipStates.Values.ToList();
                if (!currentStatesSnapshot.Any() && !(_config.MonitoredTagIds?.Any() ?? false))
                {
                    if (_lastCalculatedAggregateStatusForThisJobInstance != "No entities configured/found for tags." || DateTime.Now - _lastTimeAggregateStatusWasWrittenToDb > TimeSpan.FromMinutes(1))
                    {
                        _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "No entities configured/found for tags.", DateTime.Now);
                        _lastCalculatedAggregateStatusForThisJobInstance = "No entities configured/found for tags.";
                        _lastTimeAggregateStatusWasWrittenToDb = DateTime.Now;
                        _lastActivityTimestampForAggregate = DateTime.Now; // Ensure activity timestamp is recent
                    }
                    _isUpdatingAggregateStatus = false;
                    return;
                }
                else if (!currentStatesSnapshot.Any() && (_config.MonitoredTagIds?.Any() ?? false))
                {
                    if (_lastCalculatedAggregateStatusForThisJobInstance != "No entities found for current tags." || DateTime.Now - _lastTimeAggregateStatusWasWrittenToDb > TimeSpan.FromMinutes(1))
                    {
                        _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "No entities found for current tags.", DateTime.Now);
                        _lastCalculatedAggregateStatusForThisJobInstance = "No entities found for current tags.";
                        _lastTimeAggregateStatusWasWrittenToDb = DateTime.Now;
                        _lastActivityTimestampForAggregate = DateTime.Now; // Ensure activity timestamp is recent
                    }
                    _isUpdatingAggregateStatus = false;
                    return;
                }

                string newCalculatedAggregatedStatus = CalculateAndFormatAggregateStatus(
                    currentStatesSnapshot,
                    out int totalTracked, out int pingable, out int up,
                    out int timeouts, out int noIps, out int otherErrors);

                // Use the actual last ping activity time for the database update timestamp
                DateTime timestampForDbUpdate = _lastActivityTimestampForAggregate == DateTime.MinValue ? DateTime.Now : _lastActivityTimestampForAggregate;

                bool statusTextChanged = newCalculatedAggregatedStatus != _lastCalculatedAggregateStatusForThisJobInstance;

                // --- START OF MODIFIED SECTION ---
                // New logic for determining if a time-based refresh of LastChecked is needed.
                // This will update LastChecked in the DB if it hasn't been updated for more than the job's interval (with a minimum of 5s).
                // This makes the LastChecked timestamp more reflective of recent activity.
                bool timeBasedForceUpdate = DateTime.Now - _lastTimeAggregateStatusWasWrittenToDb >= TimeSpan.FromSeconds(Math.Max(5, _config.IntervalSeconds)); // Changed > to >=
                // --- END OF MODIFIED SECTION ---

                if (statusTextChanged || timeBasedForceUpdate)
                {
                    // Log why the update is happening
                    if (statusTextChanged)
                    {
                        Log(LogLevel.DEBUG, $"Aggregate status CHANGED. New: '{newCalculatedAggregatedStatus}'. Old Cache: '{_lastCalculatedAggregateStatusForThisJobInstance ?? "N/A"}'. Updating DB with timestamp: {timestampForDbUpdate:O}.");
                    }
                    else // Implies timeBasedForceUpdate is true
                    {
                        Log(LogLevel.DEBUG, $"Aggregate status UNCHANGED ('{newCalculatedAggregatedStatus}'). Forcing DB LastChecked update due to time threshold. Last DB write was at {_lastTimeAggregateStatusWasWrittenToDb:O}. Updating DB with timestamp: {timestampForDbUpdate:O}.");
                    }

                    _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, newCalculatedAggregatedStatus, timestampForDbUpdate);
                    _lastCalculatedAggregateStatusForThisJobInstance = newCalculatedAggregatedStatus;
                    _lastTimeAggregateStatusWasWrittenToDb = DateTime.Now; // Record the time of this DB write
                }
                // Ensure _lastActivityTimestampForAggregate itself is kept current if it's very old and nothing happened
                // This might be redundant if pings are always happening for active jobs.
                if (DateTime.Now - _lastActivityTimestampForAggregate > TimeSpan.FromMinutes(5) && _lastActivityTimestampForAggregate != DateTime.MinValue)
                {
                    Log(LogLevel.WARN, $"_lastActivityTimestampForAggregate for job {_config.Id} ('{_config.NetwatchName}') is older than 5 minutes ({_lastActivityTimestampForAggregate:O}). This might indicate no ping activity.");
                    // If there are IPs, but no activity, this implies issues. If no IPs, it's expected.
                    if (!_ipStates.IsEmpty)
                    {
                        // Force _lastActivityTimestampForAggregate to now, so the "never checked" doesn't persist too long if job is stuck.
                        // This is a failsafe, ideally individual pings update this.
                        // _lastActivityTimestampForAggregate = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Failed to update aggregate DB status.", ex);
            }
            finally
            {
                _isUpdatingAggregateStatus = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                Log(LogLevel.INFO, "Disposing job.");
                Stop();
            }
            _isDisposed = true;
        }
    }
}
