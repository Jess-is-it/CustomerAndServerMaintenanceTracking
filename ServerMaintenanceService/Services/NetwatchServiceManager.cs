using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading; // For System.Threading.Timer (for periodic refresh)
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System.Diagnostics; // For Debug.WriteLine
using System.Configuration;
using CustomerAndServerMaintenanceTracking.Services;

namespace ServerMaintenanceService.Services
{
    public class NetwatchServiceManager : IDisposable
    {
        private readonly TagRepository _tagRepository;
        private readonly NetwatchConfigRepository _netwatchConfigRepository;
        private readonly Dictionary<int, NetwatchJob> _activeJobs = new Dictionary<int, NetwatchJob>();
        private readonly object _lock = new object(); // For thread-safe access to _activeJobs
        private Timer _configRefreshTimer;
        private bool _isDisposed = false;

        //NetwatchService Heartbeats
        private Timer _heartbeatTimer;
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30); // e.g., update heartbeat every 30 seconds
        private const string PingerServiceName = "NetwatchPingerService";

        // How often to check the database for new/updated/deleted NetwatchConfigs (e.g., every 5 minutes)
        private readonly TimeSpan _configRefreshInterval = TimeSpan.FromMinutes(5);

        public NetwatchServiceManager(TagRepository tagRepository, NetwatchConfigRepository netwatchConfigRepository)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _netwatchConfigRepository = netwatchConfigRepository ?? throw new ArgumentNullException(nameof(netwatchConfigRepository));

            // Read interval from App.config
            if (int.TryParse(ConfigurationManager.AppSettings["NetwatchConfigRefreshIntervalSeconds"], out int intervalSeconds))
            {
                if (intervalSeconds <= 0) intervalSeconds = 300; // Default to 5 minutes if invalid
                _configRefreshInterval = TimeSpan.FromSeconds(intervalSeconds);
            }
            else
            {
                _configRefreshInterval = TimeSpan.FromMinutes(5); // Default if setting is missing or not a number
                Debug.WriteLine("NetwatchServiceManager: 'NetwatchConfigRefreshIntervalSeconds' not found or invalid in App.config. Defaulting to 5 minutes.");
            }
            Debug.WriteLine($"NetwatchServiceManager: Using config refresh interval of {_configRefreshInterval.TotalSeconds} seconds.");
        }

        public void InitializeAndStartAll()
        {
            Debug.WriteLine("NetwatchServiceManager: Initializing and starting all enabled jobs...");
            List<NetwatchConfig> enabledConfigs;
            try
            {
                enabledConfigs = _netwatchConfigRepository.GetAllEnabledNetwatchConfigsWithDetails();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NetwatchServiceManager: Error loading enabled configs during initialization: {ex.Message}");
                // Depending on the severity, you might want to throw or handle differently.
                // For now, log and continue (no jobs will start if this fails).
                enabledConfigs = new List<NetwatchConfig>();
            }

            lock (_lock)
            {
                foreach (var config in enabledConfigs)
                {
                    if (!_activeJobs.ContainsKey(config.Id))
                    {
                        StartJobInternal(config);
                    }
                }
            }
            Debug.WriteLine($"NetwatchServiceManager: Initialization complete. {_activeJobs.Count} jobs started.");

            // Start a timer to periodically refresh configurations from the database
            _configRefreshTimer = new Timer(async _ => await RefreshActiveJobsFromDatabaseAsync(), null, _configRefreshInterval, _configRefreshInterval);
            
            
            // --- ADD HEARTBEAT TIMER START ---
            _heartbeatTimer = new Timer(_ => WriteHeartbeat(), null, TimeSpan.FromSeconds(5), _heartbeatInterval); // Initial heartbeat after 5s, then regular interval
            Debug.WriteLine($"NetwatchServiceManager: Heartbeat timer started. Interval: {_heartbeatInterval.TotalSeconds}s.");
            // --- END HEARTBEAT TIMER START ---
        }

        private async Task RefreshActiveJobsFromDatabaseAsync(object state = null) // object state is for Timer signature
        {
            if (_isDisposed) return;

            Debug.WriteLine("NetwatchServiceManager: Refreshing active jobs from database...");
            List<NetwatchConfig> currentDbEnabledConfigs;
            try
            {
                currentDbEnabledConfigs = _netwatchConfigRepository.GetAllEnabledNetwatchConfigsWithDetails();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NetwatchServiceManager: Error loading enabled configs during refresh: {ex.Message}");
                return; // Don't proceed if we can't get current state
            }

            lock (_lock)
            {
                // Stop jobs for configs that are no longer enabled or have been deleted
                var jobIdsToStop = _activeJobs.Keys.Except(currentDbEnabledConfigs.Select(c => c.Id)).ToList();
                foreach (var jobId in jobIdsToStop)
                {
                    StopJobInternal(jobId, removeFromActiveJobsList: true);
                }

                // Start new jobs or update existing ones
                foreach (var dbConfig in currentDbEnabledConfigs)
                {
                    if (_activeJobs.TryGetValue(dbConfig.Id, out NetwatchJob existingJob))
                    {
                        // Config still exists and is enabled, update its internal config if necessary
                        // The NetwatchJob.UpdateConfig will handle if a restart is needed due to interval/tag changes
                        existingJob.UpdateConfig(dbConfig);
                    }
                    else
                    {
                        // New enabled config, start a new job
                        StartJobInternal(dbConfig);
                    }
                }
            }
            Debug.WriteLine($"NetwatchServiceManager: Refresh complete. {_activeJobs.Count} jobs active.");
        }


        // This method would be called if an external event (e.g., from UI via a service layer)
        // explicitly indicates a config has changed. For now, we rely on the periodic refresh.
        public void NotifyConfigChanged(int configId)
        {
            if (_isDisposed) return;
            Debug.WriteLine($"NetwatchServiceManager: Configuration change notified for ID {configId}.");
            // This could trigger a more immediate refresh for a specific job or overall.
            // For now, the periodic refresh will handle it.
            // Optionally, you could fetch just this one config and update/start/stop it.
            NetwatchConfig updatedConfig = null;
            try
            {
                updatedConfig = _netwatchConfigRepository.GetNetwatchConfigWithDetails(configId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NetwatchServiceManager: Error fetching details for config ID {configId} on notification: {ex.Message}");
                return;
            }

            lock (_lock)
            {
                if (updatedConfig != null && updatedConfig.IsEnabled)
                {
                    if (_activeJobs.TryGetValue(updatedConfig.Id, out NetwatchJob existingJob))
                    {
                        existingJob.UpdateConfig(updatedConfig);
                    }
                    else
                    {
                        StartJobInternal(updatedConfig);
                    }
                }
                else // Config is deleted or disabled
                {
                    StopJobInternal(configId, removeFromActiveJobsList: true);
                }
            }
        }


        private void StartJobInternal(NetwatchConfig config)
        {
            if (_isDisposed || config == null) return;

            if (!_activeJobs.ContainsKey(config.Id))
            {
                Debug.WriteLine($"NetwatchServiceManager: Starting job for Netwatch '{config.NetwatchName}' (ID: {config.Id}).");
                var job = new NetwatchJob(config, _tagRepository, _netwatchConfigRepository);
                try
                {
                    job.Start(); // Start the job's internal timer
                    _activeJobs[config.Id] = job;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NetwatchServiceManager: Failed to start job for '{config.NetwatchName}': {ex.Message}");
                    job.Dispose(); // Clean up if start failed
                }
            }
            else
            {
                Debug.WriteLine($"NetwatchServiceManager: Job for Netwatch '{config.NetwatchName}' (ID: {config.Id}) already exists. Ensuring it's up-to-date.");
                // If it exists, RefreshActiveJobsFromDatabaseAsync or NotifyConfigChanged should handle updates.
                // We could force an update here if desired:
                // _activeJobs[config.Id].UpdateConfig(config);
            }
        }

        private void StopJobInternal(int configId, bool removeFromActiveJobsList = true)
        {
            if (_isDisposed) return;

            if (_activeJobs.TryGetValue(configId, out NetwatchJob job))
            {
                Debug.WriteLine($"NetwatchServiceManager: Stopping job for Netwatch ID {configId}.");
                try
                {
                    job.Stop();
                    job.Dispose(); // Dispose of the NetwatchJob resources (its timer)
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NetwatchServiceManager: Error stopping/disposing job for ID {configId}: {ex.Message}");
                }
                finally
                {
                    if (removeFromActiveJobsList)
                    {
                        _activeJobs.Remove(configId);
                    }
                }
            }
        }

        public void StopAllJobs()
        {
            Debug.WriteLine("NetwatchServiceManager: Stopping all active jobs...");
            lock (_lock)
            {
                // Iterate over a copy of keys if modifying the dictionary
                List<int> jobIds = _activeJobs.Keys.ToList();
                foreach (var jobId in jobIds)
                {
                    StopJobInternal(jobId, removeFromActiveJobsList: true);
                }
                _activeJobs.Clear(); // Ensure it's cleared
            }
            Debug.WriteLine("NetwatchServiceManager: All jobs stopped.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void WriteHeartbeat(object state = null)
        {
            if (_isDisposed) return;

            try
            {
                // This method needs to be added to NetwatchConfigRepository or a new ServiceStatusRepository
                _netwatchConfigRepository.UpdateServiceHeartbeat(PingerServiceName, DateTime.Now);
                Debug.WriteLine($"NetwatchServiceManager: Heartbeat written at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NetwatchServiceManager: Error writing heartbeat: {ex.Message}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return; // Don't dispose more than once

            if (disposing)
            {
                // --- Dispose managed resources here ---
                Debug.WriteLine("NetwatchServiceManager: Disposing managed resources...");

                // Stop and dispose the config refresh timer
                if (_configRefreshTimer != null)
                {
                    _configRefreshTimer.Change(Timeout.Infinite, Timeout.Infinite); // Stop further events
                    _configRefreshTimer.Dispose();
                    _configRefreshTimer = null;
                    Debug.WriteLine("NetwatchServiceManager: ConfigRefreshTimer disposed.");
                }

                // Stop and dispose the heartbeat timer
                if (_heartbeatTimer != null)
                {
                    _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite); // Stop further events
                    _heartbeatTimer.Dispose();
                    _heartbeatTimer = null;
                    Debug.WriteLine("NetwatchServiceManager: HeartbeatTimer disposed.");
                }

                // Stop and dispose all active NetwatchJobs
                StopAllJobs(); // This already iterates and disposes each job

                Debug.WriteLine("NetwatchServiceManager: All managed resources disposed.");
            }

            // --- Dispose unmanaged resources here (if any) ---
            // (We don't have direct unmanaged resources in this class)

            _isDisposed = true;
        }
    }
}
