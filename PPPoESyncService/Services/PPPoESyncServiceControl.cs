using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace PPPoESyncService.Services
{
    public class PPPoESyncServiceControl
    {
        private PPPoESynchronizationManager _syncManagerInstance; // Renamed for clarity
        private readonly ServiceLogRepository _logRepository;
        private const string SERVICE_NAME_FOR_LOGGING = "PPPoESyncService";

        // Constructor now accepts the logger
        public PPPoESyncServiceControl(ServiceLogRepository logRepository)
        {
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        }

        private void Log(LogLevel level, string message, Exception ex = null)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}_Control] [{level}] {message}{(ex != null ? " - Exception: " + ex.Message : "")}");
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = SERVICE_NAME_FOR_LOGGING, // Use the control's specific logging name
                LogLevel = level.ToString(),
                Message = $"ServiceControl: {message}",
                ExceptionDetails = ex?.ToString()
            });
        }

        public bool Start()
        {
            Log(LogLevel.INFO, "PPPoESyncServiceControl.Start called. Initializing and starting PPPoESynchronizationManager...");
            try
            {
                // Pass the logger to PPPoESynchronizationManager
                _syncManagerInstance = new PPPoESynchronizationManager(_logRepository);
                _syncManagerInstance.Start();
                Log(LogLevel.INFO, "PPPoESynchronizationManager started successfully by ServiceControl.");
                return true;
            }
            catch (Exception ex)
            {
                Log(LogLevel.FATAL, "Failed to start PPPoESynchronizationManager within ServiceControl.", ex);
                return false;
            }
        }

        public bool Stop()
        {
            Log(LogLevel.INFO, "PPPoESyncServiceControl.Stop called. Stopping PPPoESynchronizationManager...");
            try
            {
                _syncManagerInstance?.Stop();
                _syncManagerInstance?.Dispose();
                Log(LogLevel.INFO, "PPPoESynchronizationManager stopped and disposed successfully by ServiceControl.");
                return true;
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Exception during PPPoESynchronizationManager stop/dispose in ServiceControl.", ex);
                return false;
            }
        }
    }
}
