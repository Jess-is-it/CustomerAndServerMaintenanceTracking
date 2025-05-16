using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading; // For System.Threading.Timer
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System.Diagnostics; // For Console.WriteLine

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
                Console.WriteLine($"Warning: NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) has an invalid interval of {_config.IntervalSeconds}s. Defaulting to 60s.");
                _config.IntervalSeconds = 60;
            }
        }

        public void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(NetwatchJob));

            Console.WriteLine($"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) starting with interval {_config.IntervalSeconds}s.");
            _timer = new Timer(async _ => await TimerCallbackAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_config.IntervalSeconds));

            // If RunUponSave is true, trigger an immediate execution without waiting for the first interval.
            // The TimeSpan.Zero in the Timer constructor above already triggers an immediate first callback.
            // If you want a separate explicit call for RunUponSave, you could do:
            // if (_config.RunUponSave) { Task.Run(async () => await ExecutePingCycleAsync()); }
        }

        public void Stop()
        {
            if (_isDisposed) return;

            Console.WriteLine($"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) stopping.");
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
                Console.WriteLine($"NetwatchJob for '{_config.NetwatchName}' (ID: {_config.Id}) configuration updated. Restarting timer.");
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
                Console.WriteLine($"Ping cycle for '{_config.NetwatchName}' (ID: {_config.Id}) skipped due to overlap.");
                return;
            }

            _isExecutingCycle = true;
            try
            {
                await ExecutePingCycleAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception in TimerCallbackAsync for '{_config.NetwatchName}' (ID: {_config.Id}): {ex}");
                // Optionally update status to a general error here
                try
                {
                    _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, $"Error: Cycle execution failed ({ex.Message.Substring(0, Math.Min(ex.Message.Length, 100))})", DateTime.Now);
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Failed to update DB with cycle execution error for '{_config.NetwatchName}': {dbEx}");
                }
            }
            finally
            {
                _isExecutingCycle = false;
            }
        }

        //public async Task ExecutePingCycleAsync()
        //{
        //    Console.WriteLine($"[NJ {_config.Id}] Executing ping cycle for '{_config.NetwatchName}' at {DateTime.Now}");

        //    List<MonitoredIpDetail> ipDetailsToPing = new List<MonitoredIpDetail>();
        //    if (_config.MonitoredTagIds != null && _config.MonitoredTagIds.Any())
        //    {
        //        try
        //        {
        //            // Use the new method to get IP details
        //            ipDetailsToPing = _tagRepository.GetMonitoredIpDetailsForTags(_config.MonitoredTagIds);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"[NJ {_config.Id}] ERROR getting IP details for '{_config.NetwatchName}': {ex.Message}");
        //            _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "Error: Failed to get IPs for tags", DateTime.Now);
        //            return;
        //        }
        //    }

        //    if (!ipDetailsToPing.Any())
        //    {
        //        Console.WriteLine($"[NJ {_config.Id}] No IPs to ping for '{_config.NetwatchName}'.");
        //        _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "No IPs configured/found for tags", DateTime.Now);
        //        // Clear any old results for this config if no IPs are currently associated
        //        try
        //        {
        //            // You might want a method to clear old IP results for a configId if IPs are removed
        //            // _netwatchConfigRepository.ClearOldIpResults(_config.Id); // Placeholder for such a method
        //        }
        //        catch (Exception ex) { Console.WriteLine($"[NJ {_config.Id}] Error clearing old IP results: {ex.Message}"); }
        //        return;
        //    }

        //    int totalIps = ipDetailsToPing.Count;
        //    int upCount = 0;
        //    int downCount = 0;
        //    int timeoutCount = 0;
        //    DateTime cycleStartTime = DateTime.Now;

        //    // Ping all IPs concurrently
        //    var pingTasks = new List<Task<(MonitoredIpDetail IpDetail, PingReply Reply)>>();
        //    foreach (var ipDetail in ipDetailsToPing)
        //    {
        //        pingTasks.Add(Pinger.SendPingAsync(ipDetail.IpAddress, _config.TimeoutMilliseconds)
        //                            .ContinueWith(task => (ipDetail, task.Result))); // Pair IP detail with its reply
        //    }

        //    var results = await Task.WhenAll(pingTasks);

        //    foreach (var result in results)
        //    {
        //        MonitoredIpDetail currentIpDetail = result.IpDetail;
        //        PingReply reply = result.Reply;
        //        DateTime pingAttemptTime = cycleStartTime; // Or more granular if Pinger could return it

        //        // Save individual ping result to the database
        //        try
        //        {
        //            _netwatchConfigRepository.SaveIndividualIpPingResult(_config.Id, currentIpDetail, reply, pingAttemptTime);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"[NJ {_config.Id}] FAILED to save individual IP ping result for {currentIpDetail.IpAddress}: {ex.Message}");
        //        }

        //        // Aggregate counts
        //        if (reply != null && reply.Status == IPStatus.Success)
        //        {
        //            upCount++;
        //        }
        //        else if (reply != null && reply.Status == IPStatus.TimedOut)
        //        {
        //            timeoutCount++;
        //            downCount++;
        //        }
        //        else
        //        {
        //            downCount++;
        //        }
        //    }

        //    // Aggregate status (same logic as before)
        //    string aggregatedStatus;
        //    if (upCount == totalIps) { aggregatedStatus = "All Up"; }
        //    else if (upCount > 0) { aggregatedStatus = $"Partial: {upCount}/{totalIps} IPs Up"; }
        //    else if (timeoutCount == totalIps) { aggregatedStatus = $"Timeout: All {totalIps} IPs timed out"; }
        //    else if (totalIps > 0) { aggregatedStatus = $"All Down: {downCount}/{totalIps} IPs Down"; }
        //    else { aggregatedStatus = "No IPs to monitor"; } // Should have been caught earlier by empty ipDetailsToPing

        //    if (timeoutCount > 0 && upCount < totalIps && !aggregatedStatus.Contains("Timeout"))
        //    {
        //        aggregatedStatus += $" ({timeoutCount} Timeout)";
        //    }

        //    Console.WriteLine($"[NJ {_config.Id}] Netwatch '{_config.NetwatchName}' Result: {aggregatedStatus}. Total: {totalIps}, Up: {upCount}, Down: {downCount}, Timeout: {timeoutCount}");

        //    try
        //    {
        //        _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, aggregatedStatus, cycleStartTime);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[NJ {_config.Id}] FAILED to update aggregate DB status for '{_config.NetwatchName}': {ex.Message}");
        //    }
        //}

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

        // In ServerMaintenanceService/Services/NetwatchJob.cs
        // Make sure these using statements are present or add them if needed:
        // using CustomerAndServerMaintenanceTracking.DataAccess; (already there)
        // using CustomerAndServerMaintenanceTracking.Models; (already there)
        // using System.Net.NetworkInformation; (already there)
        // using System.Diagnostics; (already there)

        // ... (within the NetwatchJob class) ...

        public async Task ExecutePingCycleAsync()
        {
            Console.WriteLine($"[NJ {_config.Id}] Executing ping cycle for '{_config.NetwatchName}' at {DateTime.Now}");

            List<MonitoredIpDetail> ipDetailsToPing = new List<MonitoredIpDetail>();
            if (_config.MonitoredTagIds != null && _config.MonitoredTagIds.Any())
            {
                try
                {
                    ipDetailsToPing = _tagRepository.GetMonitoredIpDetailsForTags(_config.MonitoredTagIds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NJ {_config.Id}] ERROR getting IP details for '{_config.NetwatchName}': {ex.Message}");
                    _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "Error: Failed to get IPs for tags", DateTime.Now);
                    return;
                }
            }

            if (!ipDetailsToPing.Any())
            {
                Console.WriteLine($"[NJ {_config.Id}] No IPs to ping for '{_config.NetwatchName}'.");
                _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "No IPs configured/found for tags", DateTime.Now);
                return;
            }

            int totalIps = ipDetailsToPing.Count;
            int upCount = 0;
            int downCount = 0;
            int timeoutCount = 0;
            DateTime cycleStartTime = DateTime.Now; // Use this as the consistent time for this cycle's events

            var pingTasks = new List<Task<(MonitoredIpDetail IpDetail, PingReply Reply, string PreviousStatus)>>(); // Modified to include PreviousStatus

            // --- MODIFICATION: Fetch previous status before pinging OR decide to fetch inside the loop ---
            // Option: Fetch all previous statuses upfront (might be many DB calls if done individually)
            // OR fetch individually before processing each result. Let's do it individually for simplicity here,
            // though for high performance, batching might be considered.

            foreach (var ipDetail in ipDetailsToPing)
            {
                pingTasks.Add(Task.Run(async () => // Wrap in Task.Run to allow async GetLastKnownPingStatusForIp
                {
                    string previousStatus = null;
                    try
                    {
                        // Fetch the previous status from NetwatchIpResults
                        previousStatus = _netwatchConfigRepository.GetLastKnownPingStatusForIp(_config.Id, ipDetail.IpAddress);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NJ {_config.Id}] ERROR getting previous status for {ipDetail.IpAddress}: {ex.Message}");
                        // Continue without previous status, outage logging for this IP might be affected for this cycle
                    }

                    PingReply reply = await Pinger.SendPingAsync(ipDetail.IpAddress, _config.TimeoutMilliseconds);
                    return (ipDetail, reply, previousStatus);
                }));
            }

            var resultsWithPreviousStatus = await Task.WhenAll(pingTasks);

            // --- This part remains largely the same for saving to NetwatchIpResults and calculating aggregate status ---
            // --- BUT we add the outage logging logic ---
            foreach (var result in resultsWithPreviousStatus)
            {
                MonitoredIpDetail currentIpDetail = result.IpDetail;
                PingReply reply = result.Reply;
                string previousStatus = result.PreviousStatus; // The status before this current ping
                DateTime pingAttemptTime = cycleStartTime;

                string currentPingStatusText; // Determine current ping status text
                if (reply == null) { currentPingStatusText = "Error (No Reply)"; }
                else { currentPingStatusText = reply.Status.ToString(); }
                if (currentPingStatusText.Length > 50) currentPingStatusText = currentPingStatusText.Substring(0, 50);


                // --- NEW OUTAGE LOGGING LOGIC ---
                try
                {
                    bool wasPreviouslyUp = previousStatus != null && previousStatus.Equals("Success", StringComparison.OrdinalIgnoreCase);
                    // Consider other statuses like "Pending" or empty string as "UP" for simplicity if they shouldn't trigger an outage start.
                    // For more robust "wasPreviouslyUp", you might have a helper function: IsStatusConsideredUp(previousStatus)
                    if (string.IsNullOrEmpty(previousStatus) || previousStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase)) // Treat null, empty, or "Pending" as if it was up for new outage detection
                    {
                        wasPreviouslyUp = true;
                    }


                    bool isCurrentlyDown = reply == null || reply.Status != IPStatus.Success; // e.g., TimedOut, DestinationHostUnreachable etc.

                    if (wasPreviouslyUp && isCurrentlyDown)
                    {
                        // IP went from UP to DOWN: Start a new outage log
                        _netwatchConfigRepository.StartOutageLog(
                            _config.Id,
                            currentIpDetail.IpAddress,
                            currentIpDetail.EntityName,
                            pingAttemptTime, // This is the time the current "down" status was detected
                            currentPingStatusText // The status that caused the outage to start
                        );
                    }
                    else if (!wasPreviouslyUp && !isCurrentlyDown) // Was DOWN, now is UP
                    {
                        // IP went from DOWN to UP: End the existing outage log
                        _netwatchConfigRepository.EndOutageLog(
                            _config.Id,
                            currentIpDetail.IpAddress,
                            pingAttemptTime // This is the time the current "up" status was detected
                        );
                    }
                    // If UP -> UP or DOWN -> DOWN, no changes to OutageLog
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NJ {_config.Id}] ERROR during outage logging for {currentIpDetail.IpAddress}: {ex.Message}");
                }
                // --- END NEW OUTAGE LOGGING LOGIC ---


                // Save individual ping result to the NetwatchIpResults table (still important for current state)
                try
                {
                    _netwatchConfigRepository.SaveIndividualIpPingResult(_config.Id, currentIpDetail, reply, pingAttemptTime);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NJ {_config.Id}] FAILED to save individual IP ping result to NetwatchIpResults for {currentIpDetail.IpAddress}: {ex.Message}");
                }

                // Aggregate counts for overall status
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
            // ... (your existing logic for determining aggregatedStatus) ...
            // Example (ensure this matches your exact existing logic):
            if (totalIps == 0) { aggregatedStatus = "No IPs to monitor"; } // Should be caught earlier
            else if (upCount == totalIps) { aggregatedStatus = "All Up"; }
            else if (upCount > 0) { aggregatedStatus = $"Partial: {upCount}/{totalIps} IPs Up"; }
            else if (timeoutCount == totalIps) { aggregatedStatus = $"Timeout: All {totalIps} IPs timed out"; } // Or simply "All Timeout"
            else { aggregatedStatus = $"All Down: {downCount}/{totalIps} IPs Down"; }


            if (timeoutCount > 0 && upCount < totalIps && upCount > 0 && !aggregatedStatus.ToLower().Contains("timeout")) // Avoid double "timeout" if already in main status
            {
                aggregatedStatus += $" ({timeoutCount} Timeout)";
            }
            else if (timeoutCount > 0 && upCount == 0 && totalIps > 0 && !aggregatedStatus.ToLower().Contains("timeout")) // All are down, and some are timeouts
            {
                if (downCount == timeoutCount) // All down ARE timeouts
                {
                    // Already covered by "Timeout: All X IPs timed out" or similar if you have that
                }
                else if (!aggregatedStatus.Equals($"Timeout: All {totalIps} IPs timed out", StringComparison.OrdinalIgnoreCase))
                {
                    aggregatedStatus += $" ({timeoutCount} Timeout)";
                }
            }


            Console.WriteLine($"[NJ {_config.Id}] Netwatch '{_config.NetwatchName}' Result: {aggregatedStatus}. Total: {totalIps}, Up: {upCount}, Down: {downCount}, Timeout: {timeoutCount}");

            try
            {
                _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, aggregatedStatus, cycleStartTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NJ {_config.Id}] FAILED to update aggregate DB status for '{_config.NetwatchName}': {ex.Message}");
            }
        }

        // ... (rest of NetwatchJob.cs) ...
    }
}
