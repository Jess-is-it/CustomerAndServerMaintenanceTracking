using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace PPPoESyncService.Services
{
    public class PPPoESynchronizationManager : IDisposable
    {
        private readonly SyncManager _syncManager;
        private Timer _syncTimer;
        private Timer _heartbeatTimer;
        private Timer _logCleanupTimer;
        private bool _isDisposed = false;
        private volatile bool _isSyncing = false;

        private readonly TimeSpan _defaultSyncInterval = TimeSpan.FromMinutes(5);
        private TimeSpan _currentSyncInterval;


        private readonly TagRepository _tagRepository; // Added: To pass to NetwatchConfigRepository
        private readonly ServiceLogRepository _logRepository;
        private const string SERVICE_NAME = "PPPoESyncService";
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _logCleanupInterval = TimeSpan.FromHours(24);

        public PPPoESynchronizationManager(ServiceLogRepository logRepository)
        {
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _tagRepository = new TagRepository(); // Instantiate TagRepository
            _syncManager = new SyncManager(_logRepository, SERVICE_NAME);

            Log(LogLevel.INFO, "PPPoESynchronizationManager initializing...");
            LoadConfiguration();
            Log(LogLevel.INFO, "PPPoESynchronizationManager initialized.");
        }

        private void LoadConfiguration()
        {
            string intervalSetting = ConfigurationManager.AppSettings["PPPoESyncIntervalSeconds"];
            if (int.TryParse(intervalSetting, out int intervalSeconds) && intervalSeconds > 0)
            {
                _currentSyncInterval = TimeSpan.FromSeconds(intervalSeconds);
            }
            else
            {
                _currentSyncInterval = _defaultSyncInterval;
                Log(LogLevel.WARN, $"'PPPoESyncIntervalSeconds' not found or invalid in App.config. Defaulting to {_defaultSyncInterval.TotalMinutes} minutes.");
            }
            Log(LogLevel.INFO, $"PPPoE Sync Interval set to {_currentSyncInterval.TotalSeconds} seconds.");
        }

        public void Start()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PPPoESynchronizationManager));
            }

            Log(LogLevel.INFO, $"Starting PPPoE Synchronization Manager. Sync interval: {_currentSyncInterval.TotalSeconds} seconds.");
            _syncTimer = new Timer(SyncTimerCallback, null, TimeSpan.Zero, _currentSyncInterval);

            _heartbeatTimer = new Timer(HeartbeatTimerCallback, null, TimeSpan.FromSeconds(5), _heartbeatInterval);
            Log(LogLevel.INFO, $"Heartbeat timer started. Interval: {_heartbeatInterval.TotalSeconds}s.");

            _logCleanupTimer = new Timer(LogCleanupTimerCallback, null, TimeSpan.FromMinutes(5), _logCleanupInterval);
            Log(LogLevel.INFO, $"Log cleanup timer started. Interval: {_logCleanupInterval.TotalHours} hours.");
        }

        public void Stop()
        {
            Log(LogLevel.INFO, "Stopping PPPoE Synchronization Manager...");
            _syncTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _logCleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            Log(LogLevel.INFO, "Timers stopped.");
        }

        private void SyncTimerCallback(object state)
        {
            if (_isDisposed) return;

            if (_isSyncing)
            {
                Log(LogLevel.DEBUG, "SyncCustomers already in progress. Skipping this interval.");
                return;
            }

            _isSyncing = true;
            Log(LogLevel.INFO, "SyncTimerCallback triggered. Executing SyncCustomers...");
            try
            {
                _syncManager.SyncCustomers();
            }
            catch (Exception ex)
            {
                Log(LogLevel.FATAL, $"Unhandled exception during SyncCustomers execution.", ex);
            }
            finally
            {
                _isSyncing = false;
                Log(LogLevel.INFO, "SyncCustomers execution finished.");
            }
        }

        private void HeartbeatTimerCallback(object state)
        {
            if (_isDisposed) return;
            try
            {
                // Pass the _logRepository and _tagRepository to the NetwatchConfigRepository constructor
                var statusRepo = new NetwatchConfigRepository(_logRepository, _tagRepository);
                statusRepo.UpdateServiceHeartbeat(SERVICE_NAME, DateTime.Now);
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, $"Failed to write heartbeat for {SERVICE_NAME}.", ex);
            }
        }

        private void LogCleanupTimerCallback(object state)
        {
            if (_isDisposed) return;
            Log(LogLevel.INFO, "Log cleanup timer triggered. Deleting old logs...");
            try
            {
                int deletedCount = _logRepository.DeleteOldLogs(7);
                Log(LogLevel.INFO, $"Log cleanup complete. Deleted {deletedCount} old log entries.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Error during automated log cleanup.", ex);
            }
        }

        private void Log(LogLevel level, string message, Exception ex = null)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME}] [{level}] {message}{(ex != null ? " - Exception: " + ex.Message : "")}");
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = SERVICE_NAME,
                LogLevel = level.ToString(),
                Message = message,
                ExceptionDetails = ex?.ToString()
            });
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
                Log(LogLevel.INFO, "Disposing PPPoESynchronizationManager...");
                Stop();

                _syncTimer?.Dispose();
                _syncTimer = null;

                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;

                _logCleanupTimer?.Dispose();
                _logCleanupTimer = null;
                Log(LogLevel.INFO, "PPPoESynchronizationManager disposed.");
            }
            _isDisposed = true;
        }
    }
}

