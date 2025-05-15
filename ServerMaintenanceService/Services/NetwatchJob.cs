using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading; // For System.Threading.Timer
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System.Diagnostics; // For Debug.WriteLine

namespace ServerMaintenanceService.Services
{
    public class NetwatchJob : IDisposable
    {
        private NetwatchConfig _config;
        private Timer _timer;
        private bool _isDisposed = false;
        private volatile bool _isExecutingCycle = false; // To prevent overlapping executions

        // Dependencies - these should be passed in or resolved via a DI container if you use one.
        // For simplicity now, we can instantiate them or pass them.
        // Let's assume they are passed via constructor for better testability.
        private readonly TagRepository _tagRepository;
        private readonly NetwatchConfigRepository _netwatchConfigRepository;

        public int ConfigId => _config.Id;

        public NetwatchJob(NetwatchConfig config, TagRepository tagRepository, NetwatchConfigRepository netwatchConfigRepository)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (tagRepository == null) throw new ArgumentNullException(nameof(tagRepository));
            if (netwatchConfigRepository == null) throw new ArgumentNullException(nameof(netwatchConfigRepository));

            _config = config;
            _tagRepository = tagRepository;
            _netwatchConfigRepository = netwatchConfigRepository;

            // Ensure interval is a positive value, default to 60s if not
            if (_config.IntervalSeconds <= 0)
            {
                Debug.WriteLine($"Warning: NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) has an invalid interval of {_config.IntervalSeconds}s. Defaulting to 60s.");
                _config.IntervalSeconds = 60;
            }
        }

        public void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(NetwatchJob));

            Debug.WriteLine($"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) starting with interval {_config.IntervalSeconds}s.");
            _timer = new Timer(async _ => await TimerCallbackAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_config.IntervalSeconds));

            // If RunUponSave is true, trigger an immediate execution without waiting for the first interval.
            // The TimeSpan.Zero in the Timer constructor above already triggers an immediate first callback.
            // If you want a separate explicit call for RunUponSave, you could do:
            // if (_config.RunUponSave) { Task.Run(async () => await ExecutePingCycleAsync()); }
        }

        public void Stop()
        {
            if (_isDisposed) return;

            Debug.WriteLine($"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) stopping.");
            _timer?.Change(Timeout.Infinite, Timeout.Infinite); // Stop the timer
            _timer?.Dispose();
            _timer = null;
        }

        public void UpdateConfig(NetwatchConfig newConfig)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(NetwatchJob));
            if (newConfig == null) throw new ArgumentNullException(nameof(newConfig));
            if (newConfig.Id != _config.Id) throw new ArgumentException("Cannot update job with a different config ID.");

            bool needsRestart = _config.IntervalSeconds != newConfig.IntervalSeconds ||
                                !_config.MonitoredTagIds.SequenceEqual(newConfig.MonitoredTagIds); // Basic check for tag changes

            _config = newConfig; // Update to the new config

            if (needsRestart && _timer != null) // Only restart if already started
            {
                Debug.WriteLine($"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) configuration updated. Restarting timer.");
                Stop();
                Start();
            }
            else if (_timer == null && _config.IsEnabled) // If it was previously stopped but now enabled by config
            {
                Start();
            }
            else if (!_config.IsEnabled && _timer != null) // If it was running but now disabled by config
            {
                Stop();
            }
        }


        private async Task TimerCallbackAsync()
        {
            if (_isDisposed || !_config.IsEnabled) // Double check if enabled, in case config changed
            {
                Stop(); // Ensure timer is stopped if job is disposed or config disabled
                return;
            }

            if (_isExecutingCycle)
            {
                Debug.WriteLine($"Ping cycle for '{_config.NetwatchName}' (ID: {_config.Id}) skipped due to overlap.");
                return;
            }

            _isExecutingCycle = true;
            try
            {
                await ExecutePingCycleAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception in TimerCallbackAsync for '{_config.NetwatchName}' (ID: {_config.Id}): {ex}");
                // Optionally update status to a general error here
                try
                {
                    _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, $"Error: Cycle execution failed ({ex.Message.Substring(0, Math.Min(ex.Message.Length, 100))})", DateTime.Now);
                }
                catch (Exception dbEx)
                {
                    Debug.WriteLine($"Failed to update DB with cycle execution error for '{_config.NetwatchName}': {dbEx}");
                }
            }
            finally
            {
                _isExecutingCycle = false;
            }
        }

        public async Task ExecutePingCycleAsync()
        {
            Debug.WriteLine($"[NJ {_config.Id}] Executing ping cycle for '{_config.NetwatchName}' at {DateTime.Now}");

            List<MonitoredIpDetail> ipDetailsToPing = new List<MonitoredIpDetail>();
            if (_config.MonitoredTagIds != null && _config.MonitoredTagIds.Any())
            {
                try
                {
                    // Use the new method to get IP details
                    ipDetailsToPing = _tagRepository.GetMonitoredIpDetailsForTags(_config.MonitoredTagIds);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NJ {_config.Id}] ERROR getting IP details for '{_config.NetwatchName}': {ex.Message}");
                    _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "Error: Failed to get IPs for tags", DateTime.Now);
                    return;
                }
            }

            if (!ipDetailsToPing.Any())
            {
                Debug.WriteLine($"[NJ {_config.Id}] No IPs to ping for '{_config.NetwatchName}'.");
                _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "No IPs configured/found for tags", DateTime.Now);
                // Clear any old results for this config if no IPs are currently associated
                try
                {
                    // You might want a method to clear old IP results for a configId if IPs are removed
                    // _netwatchConfigRepository.ClearOldIpResults(_config.Id); // Placeholder for such a method
                }
                catch (Exception ex) { Debug.WriteLine($"[NJ {_config.Id}] Error clearing old IP results: {ex.Message}"); }
                return;
            }

            int totalIps = ipDetailsToPing.Count;
            int upCount = 0;
            int downCount = 0;
            int timeoutCount = 0;
            DateTime cycleStartTime = DateTime.Now;

            // Ping all IPs concurrently
            var pingTasks = new List<Task<(MonitoredIpDetail IpDetail, PingReply Reply)>>();
            foreach (var ipDetail in ipDetailsToPing)
            {
                pingTasks.Add(Pinger.SendPingAsync(ipDetail.IpAddress, _config.TimeoutMilliseconds)
                                    .ContinueWith(task => (ipDetail, task.Result))); // Pair IP detail with its reply
            }

            var results = await Task.WhenAll(pingTasks);

            foreach (var result in results)
            {
                MonitoredIpDetail currentIpDetail = result.IpDetail;
                PingReply reply = result.Reply;
                DateTime pingAttemptTime = cycleStartTime; // Or more granular if Pinger could return it

                // Save individual ping result to the database
                try
                {
                    _netwatchConfigRepository.SaveIndividualIpPingResult(_config.Id, currentIpDetail, reply, pingAttemptTime);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NJ {_config.Id}] FAILED to save individual IP ping result for {currentIpDetail.IpAddress}: {ex.Message}");
                }

                // Aggregate counts
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    upCount++;
                }
                else if (reply != null && reply.Status == IPStatus.TimedOut)
                {
                    timeoutCount++;
                    downCount++;
                }
                else
                {
                    downCount++;
                }
            }

            // Aggregate status (same logic as before)
            string aggregatedStatus;
            if (upCount == totalIps) { aggregatedStatus = "All Up"; }
            else if (upCount > 0) { aggregatedStatus = $"Partial: {upCount}/{totalIps} IPs Up"; }
            else if (timeoutCount == totalIps) { aggregatedStatus = $"Timeout: All {totalIps} IPs timed out"; }
            else if (totalIps > 0) { aggregatedStatus = $"All Down: {downCount}/{totalIps} IPs Down"; }
            else { aggregatedStatus = "No IPs to monitor"; } // Should have been caught earlier by empty ipDetailsToPing

            if (timeoutCount > 0 && upCount < totalIps && !aggregatedStatus.Contains("Timeout"))
            {
                aggregatedStatus += $" ({timeoutCount} Timeout)";
            }

            Debug.WriteLine($"[NJ {_config.Id}] Netwatch '{_config.NetwatchName}' Result: {aggregatedStatus}. Total: {totalIps}, Up: {upCount}, Down: {downCount}, Timeout: {timeoutCount}");

            try
            {
                _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, aggregatedStatus, cycleStartTime);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NJ {_config.Id}] FAILED to update aggregate DB status for '{_config.NetwatchName}': {ex.Message}");
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
                Stop(); // Stop and dispose the timer
            }
            _isDisposed = true;
        }
    }
 }
