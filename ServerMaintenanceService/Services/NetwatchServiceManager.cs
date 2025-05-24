using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading; // For System.Threading.Timer (for periodic refresh)
using System.Threading.Tasks;
using System.Diagnostics; // For Debug.WriteLine
using System.Configuration;
using CustomerAndServerMaintenanceTracking.Services;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace ServerMaintenanceService.Services
{
    public class NetwatchServiceManager : IDisposable
    {
        private readonly TagRepository _tagRepository;
        private readonly NetwatchConfigRepository _netwatchConfigRepository;
        private readonly ServiceLogRepository _logRepository; // For logging
        private readonly Dictionary<int, NetwatchJob> _activeJobs = new Dictionary<int, NetwatchJob>();
        private readonly object _lock = new object();
        private Timer _configRefreshTimer;
        private Timer _heartbeatTimer;
        private Timer _logCleanupTimer;
        private bool _isDisposed = false;

        private const string SERVICE_NAME = "NetwatchPingerService"; // Service name for logs
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _logCleanupInterval = TimeSpan.FromHours(24);
        private TimeSpan _configRefreshInterval;

        public NetwatchServiceManager(TagRepository tagRepository, NetwatchConfigRepository netwatchConfigRepository, ServiceLogRepository logRepository)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _netwatchConfigRepository = netwatchConfigRepository ?? throw new ArgumentNullException(nameof(netwatchConfigRepository));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository)); // Store the logger

            LoadConfiguration();
            Log(LogLevel.INFO, "NetwatchServiceManager initialized.");
        }

        private void LoadConfiguration()
        {
            string intervalSetting = ConfigurationManager.AppSettings["NetwatchConfigRefreshIntervalSeconds"];
            if (int.TryParse(intervalSetting, out int intervalSeconds) && intervalSeconds > 0)
            {
                _configRefreshInterval = TimeSpan.FromSeconds(intervalSeconds);
            }
            else
            {
                _configRefreshInterval = TimeSpan.FromMinutes(1); // Default to 1 minute if not specified or invalid
                Log(LogLevel.WARN, $"'NetwatchConfigRefreshIntervalSeconds' not found or invalid. Defaulting to {_configRefreshInterval.TotalMinutes} minute(s).");
            }
            Log(LogLevel.INFO, $"Netwatch Config Refresh Interval set to {_configRefreshInterval.TotalSeconds} seconds.");
        }

        private void Log(LogLevel level, string message, Exception ex = null)
        {
            // Use the instance _logRepository
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = SERVICE_NAME, // Use the defined service name
                LogLevel = level.ToString(),
                Message = message,
                ExceptionDetails = ex?.ToString()
            });
            // Also write to console for immediate visibility if running manually
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME}] [{level}] {message}{(ex != null ? " - Exception: " + ex.Message : "")}");
        }

        public async Task InitializeAndStartAllAsync()
        {
            Log(LogLevel.INFO, "Initializing and starting all enabled Netwatch jobs...");
            List<NetwatchConfig> enabledConfigs;
            try
            {
                enabledConfigs = _netwatchConfigRepository.GetAllEnabledNetwatchConfigsWithDetails();
            }
            catch (Exception ex)
            {
                Log(LogLevel.FATAL, "Error loading enabled Netwatch configs during initialization.", ex);
                enabledConfigs = new List<NetwatchConfig>(); // Prevent further issues
            }

            // Use a local list for modifications to avoid issues with collection modification during iteration
            var currentJobIds = new List<int>();
            lock (_lock)
            {
                currentJobIds.AddRange(_activeJobs.Keys);
            }

            // Stop jobs that are no longer in the enabled list
            foreach (var jobId in currentJobIds)
            {
                if (!enabledConfigs.Any(c => c.Id == jobId))
                {
                    await StopJobInternalAsync(jobId);
                }
            }

            // Start or update jobs
            foreach (var config in enabledConfigs)
            {
                NetwatchJob job;
                lock (_lock)
                {
                    _activeJobs.TryGetValue(config.Id, out job);
                }

                if (job != null)
                {
                    await job.UpdateConfigAsync(config); // Update existing job
                }
                else
                {
                    await StartJobInternalAsync(config); // Start new job
                }
            }
            Log(LogLevel.INFO, $"Initialization complete. {_activeJobs.Count} Netwatch jobs active.");

            // Start timers
            _configRefreshTimer = new Timer(async _ => await RefreshActiveJobsFromDatabaseAsync(), null, _configRefreshInterval, _configRefreshInterval);
            _heartbeatTimer = new Timer(HeartbeatTimerCallback, null, TimeSpan.FromSeconds(5), _heartbeatInterval); // Start after 5s, then repeat
            _logCleanupTimer = new Timer(LogCleanupTimerCallback, null, TimeSpan.FromMinutes(5), _logCleanupInterval); // First run after 5 min
        }

        private async Task RefreshActiveJobsFromDatabaseAsync(object state = null)
        {
            if (_isDisposed) return;
            Log(LogLevel.DEBUG, "Refreshing active Netwatch jobs from database...");

            List<NetwatchConfig> currentDbEnabledConfigs;
            try
            {
                currentDbEnabledConfigs = _netwatchConfigRepository.GetAllEnabledNetwatchConfigsWithDetails();
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Error loading enabled Netwatch configs during refresh.", ex);
                return;
            }

            var currentJobIds = new List<int>();
            lock (_lock)
            {
                currentJobIds.AddRange(_activeJobs.Keys);
            }

            // Identify jobs to stop (in DB but not enabled, or not in DB anymore)
            var dbConfigIds = currentDbEnabledConfigs.Select(c => c.Id).ToList();
            var jobIdsToStop = currentJobIds.Except(dbConfigIds).ToList();

            foreach (var jobId in jobIdsToStop)
            {
                await StopJobInternalAsync(jobId);
            }

            // Start new jobs or update existing ones
            foreach (var dbConfig in currentDbEnabledConfigs)
            {
                NetwatchJob existingJob;
                lock (_lock)
                {
                    _activeJobs.TryGetValue(dbConfig.Id, out existingJob);
                }

                if (existingJob != null)
                {
                    // Config still exists and is enabled, update its internal config
                    await existingJob.UpdateConfigAsync(dbConfig);
                }
                else
                {
                    // New enabled config, start a new job
                    await StartJobInternalAsync(dbConfig);
                }
            }
            Log(LogLevel.DEBUG, $"Netwatch job refresh complete. {_activeJobs.Count} jobs active.");
        }

        public async Task NotifyConfigChangedAsync(int configId)
        {
            if (_isDisposed) return;
            Log(LogLevel.INFO, $"Configuration change notified for Netwatch ID {configId}.");

            NetwatchConfig updatedConfig = null;
            try
            {
                updatedConfig = _netwatchConfigRepository.GetNetwatchConfigWithDetails(configId); // This should fetch MonitoredTagIds
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, $"Error fetching details for Netwatch config ID {configId} on notification.", ex);
                return;
            }

            NetwatchJob existingJob;
            lock (_lock)
            {
                _activeJobs.TryGetValue(configId, out existingJob);
            }

            if (updatedConfig != null && updatedConfig.IsEnabled)
            {
                if (existingJob != null)
                {
                    await existingJob.UpdateConfigAsync(updatedConfig);
                }
                else
                {
                    await StartJobInternalAsync(updatedConfig);
                }
            }
            else // Config is deleted or disabled
            {
                if (existingJob != null)
                {
                    await StopJobInternalAsync(configId);
                }
            }
        }

        private async Task StartJobInternalAsync(NetwatchConfig config)
        {
            if (_isDisposed || config == null) return;

            lock (_lock) // Ensure thread-safe check and add
            {
                if (_activeJobs.ContainsKey(config.Id))
                {
                    Log(LogLevel.DEBUG, $"Job for Netwatch '{config.NetwatchName}' (ID: {config.Id}) already exists. Update will be handled if necessary.");
                    // Optionally, trigger an update if it already exists, though RefreshActiveJobsFromDatabaseAsync should handle it.
                    // _activeJobs[config.Id].UpdateConfigAsync(config).Wait(); // Or await if this method can be async
                    return;
                }
            }

            Log(LogLevel.INFO, $"Starting job for Netwatch '{config.NetwatchName}' (ID: {config.Id}).");
            // Pass the log repository and service name to the NetwatchJob constructor
            var job = new NetwatchJob(config, _tagRepository, _netwatchConfigRepository, _logRepository, SERVICE_NAME);
            try
            {
                await job.StartAsync(); // Call the async version
                lock (_lock)
                {
                    _activeJobs[config.Id] = job; // Add after successful start
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, $"Failed to start job for Netwatch '{config.NetwatchName}'.", ex);
                job.Dispose();
            }
        }

        private async Task StopJobInternalAsync(int configId)
        {
            if (_isDisposed) return;
            NetwatchJob job;
            lock (_lock)
            {
                if (!_activeJobs.TryGetValue(configId, out job))
                {
                    return; // Job not found or already removed
                }
                _activeJobs.Remove(configId); // Remove immediately under lock
            }

            Log(LogLevel.INFO, $"Stopping job for Netwatch ID {configId} ('{job?.NetwatchName ?? "Unknown"}').");
            try
            {
                job?.Stop();    // Stop the job's operations
                job?.Dispose(); // Dispose of the NetwatchJob resources
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, $"Error stopping/disposing job for Netwatch ID {configId}.", ex);
            }
        }

        public async Task StopAllJobsAsync()
        {
            Log(LogLevel.INFO, "Stopping all active Netwatch jobs...");
            List<int> jobIds;
            lock (_lock)
            {
                jobIds = _activeJobs.Keys.ToList();
            }

            foreach (var jobId in jobIds)
            {
                await StopJobInternalAsync(jobId); // This will also remove from _activeJobs
            }

            lock (_lock) // Ensure dictionary is clear after all stop attempts
            {
                _activeJobs.Clear();
            }
            Log(LogLevel.INFO, "All Netwatch jobs stopped.");
        }

        private void HeartbeatTimerCallback(object state)
        {
            if (_isDisposed) return;
            try
            {
                _netwatchConfigRepository.UpdateServiceHeartbeat(SERVICE_NAME, DateTime.Now);
                // Log(LogLevel.DEBUG, $"Heartbeat written for {SERVICE_NAME}."); // Usually too verbose
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, $"Failed to write Netwatch service heartbeat.", ex);
            }
        }

        private void LogCleanupTimerCallback(object state)
        {
            if (_isDisposed) return;
            Log(LogLevel.INFO, "Netwatch log cleanup timer triggered. Deleting old logs...");
            try
            {
                int deletedCount = _logRepository.DeleteOldLogs(7); // Keep 7 days of logs
                Log(LogLevel.INFO, $"Netwatch log cleanup complete. Deleted {deletedCount} old log entries.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Error during automated Netwatch log cleanup.", ex);
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
                Log(LogLevel.INFO, "Disposing NetwatchServiceManager...");
                _configRefreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _configRefreshTimer?.Dispose();
                _configRefreshTimer = null;

                _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;

                _logCleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _logCleanupTimer?.Dispose();
                _logCleanupTimer = null;

                // StopAllJobsAsync is async, but Dispose is typically synchronous.
                // We can call it and wait, or if this Dispose is called on a dedicated shutdown thread,
                // Task.Run(...).Wait() or similar might be acceptable.
                // For simplicity in a synchronous Dispose, we'll call the async and not wait,
                // assuming the jobs will stop gracefully.
                // If robust waiting is needed, this Dispose might need to become async or use .Wait().
                StopAllJobsAsync().ConfigureAwait(false).GetAwaiter().GetResult(); // Blocking call for synchronous Dispose
                Log(LogLevel.INFO, "NetwatchServiceManager disposed.");
            }
            _isDisposed = true;
        }
    }
}
