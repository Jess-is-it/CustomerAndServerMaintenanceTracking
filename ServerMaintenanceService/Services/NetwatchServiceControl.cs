using System;
using System.Threading.Tasks;
using SharedLibrary.DataAccess; // For ServiceLogRepository
using SharedLibrary.Models;    // For ServiceLogEntry, LogLevel
// Assuming NetwatchServiceManager, TagRepository, NetwatchConfigRepository are correctly referenced
// either via project reference to SharedLibrary or are part of this project.
// For this example, assuming NetwatchServiceManager is in this project's Services namespace.
namespace ServerMaintenanceService.Services
{
    public class NetwatchServiceControl
    {
        private NetwatchServiceManager _netwatchManager;
        private readonly ServiceLogRepository _logRepository;
        private const string SERVICE_NAME_FOR_LOGGING = "NetwatchPingerService";

        private readonly TagRepository _tagRepo;
        private readonly NetwatchConfigRepository _netwatchConfigRepo;

        // Constructor now accepts the pre-initialized repositories
        public NetwatchServiceControl(TagRepository tagRepository, NetwatchConfigRepository netwatchConfigRepository, ServiceLogRepository logRepository)
        {
            _tagRepo = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _netwatchConfigRepo = netwatchConfigRepository ?? throw new ArgumentNullException(nameof(netwatchConfigRepository));
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

        public async Task<bool> StartAsync()
        {
            Log(LogLevel.INFO, "NetwatchServiceControl.StartAsync called. Initializing and starting NetwatchServiceManager...");
            try
            {
                // Pass the already initialized repositories to NetwatchServiceManager
                _netwatchManager = new NetwatchServiceManager(_tagRepo, _netwatchConfigRepo, _logRepository);
                await _netwatchManager.InitializeAndStartAllAsync();
                Log(LogLevel.INFO, "NetwatchServiceManager started successfully by ServiceControl.");
                return true;
            }
            catch (Exception ex)
            {
                Log(LogLevel.FATAL, "Failed to start NetwatchServiceManager within ServiceControl.", ex);
                return false;
            }
        }

        public async Task<bool> StopAsync()
        {
            Log(LogLevel.INFO, "NetwatchServiceControl.StopAsync called. Stopping NetwatchServiceManager...");
            try
            {
                if (_netwatchManager != null)
                {
                    await _netwatchManager.StopAllJobsAsync();
                    _netwatchManager.Dispose();
                }
                Log(LogLevel.INFO, "NetwatchServiceManager stopped and disposed successfully by ServiceControl.");
                return true;
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "Exception during NetwatchServiceManager stop/dispose in ServiceControl.", ex);
                return false;
            }
        }
    }
}
