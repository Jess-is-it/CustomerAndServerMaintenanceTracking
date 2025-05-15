using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading; // For System.Threading.Timer
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System.Diagnostics; // For Debug.WriteLine

namespace CustomerAndServerMaintenanceTracking.Services
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
            Debug.WriteLine($"Executing ping cycle for '{_config.NetwatchName}' (ID: {_config.Id}) at {DateTime.Now}");

            List<string> ipAddressesToPing = new List<string>();
            if (_config.MonitoredTagIds != null && _config.MonitoredTagIds.Any())
            {
                try
                {
                    ipAddressesToPing = _tagRepository.GetIpAddressesForMonitoredTags(_config.MonitoredTagIds);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting IP addresses for Netwatch '{_config.NetwatchName}': {ex.Message}");
                    _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "Error: Failed to get IPs for tags", DateTime.Now);
                    return;
                }
            }

            if (!ipAddressesToPing.Any())
            {
                Debug.WriteLine($"No IP addresses to ping for Netwatch '{_config.NetwatchName}'.");
                _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, "No IPs configured/found for tags", DateTime.Now);
                return;
            }

            int totalIps = ipAddressesToPing.Count;
            int upCount = 0;
            int downCount = 0;
            int timeoutCount = 0;
            // List<string> downIpDetails = new List<string>(); // For more detailed status reporting later

            // Ping all IPs concurrently
            var pingTasks = new List<Task<PingReply>>();
            foreach (var ip in ipAddressesToPing)
            {
                pingTasks.Add(Pinger.SendPingAsync(ip, _config.TimeoutMilliseconds));
            }

            await Task.WhenAll(pingTasks);

            foreach (var task in pingTasks)
            {
                PingReply reply = task.Result; // Get the result (already completed)
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    upCount++;
                }
                else if (reply != null && reply.Status == IPStatus.TimedOut)
                {
                    timeoutCount++;
                    downCount++; // Consider timeout as a form of "down"
                    // downIpDetails.Add($"{ipAddressesToPing[pingTasks.IndexOf(task)]}: Timeout");
                }
                else // Other failures or null reply
                {
                    downCount++;
                    // string reason = reply != null ? reply.Status.ToString() : "Ping Failed (No Reply)";
                    // downIpDetails.Add($"{ipAddressesToPing[pingTasks.IndexOf(task)]}: {reason}");
                }
            }

            // Aggregate status
            string aggregatedStatus;
            if (totalIps == 0) // Should have been caught earlier
            {
                aggregatedStatus = "No IPs to monitor";
            }
            else if (upCount == totalIps)
            {
                aggregatedStatus = "All Up";
            }
            else if (upCount > 0 && upCount < totalIps)
            {
                aggregatedStatus = $"Partial: {upCount}/{totalIps} IPs Up";
            }
            else if (upCount == 0 && timeoutCount > 0 && timeoutCount == totalIps)
            {
                aggregatedStatus = $"Timeout: All {totalIps} IPs timed out";
            }
            else if (upCount == 0 && totalIps > 0) // All are down, not necessarily all timeouts
            {
                aggregatedStatus = $"All Down: {downCount}/{totalIps} IPs Down";
            }
            else // Should not happen if logic is correct, but as a fallback
            {
                aggregatedStatus = "Unknown Status";
            }

            // If there were timeouts specifically and not all were up
            if (timeoutCount > 0 && upCount < totalIps && !aggregatedStatus.Contains("Timeout"))
            {
                aggregatedStatus += $" ({timeoutCount} Timeout)";
            }


            Debug.WriteLine($"Netwatch '{_config.NetwatchName}' Result: {aggregatedStatus}. Total: {totalIps}, Up: {upCount}, Down: {downCount}, Timeout: {timeoutCount}");

            // Update database
            try
            {
                _netwatchConfigRepository.UpdateNetwatchLastStatus(_config.Id, aggregatedStatus, DateTime.Now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update database for Netwatch '{_config.NetwatchName}': {ex.Message}");
                // Consider how to handle DB update failures - retry? Log to a more persistent store?
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
