using System;
using System.IO; // For Path.GetDirectoryName
using System.Reflection; // For Assembly.GetExecutingAssembly().Location
using System.Threading.Tasks;
using ServerMaintenanceService.Services; // For NetwatchServiceControl
using SharedLibrary.DataAccess; // For ServiceLogRepository
using SharedLibrary.Models;     // For ServiceLogEntry, LogLevel


namespace ServerMaintenanceService
{
    class Program
    {
        private static NetwatchServiceControl _serviceControl;
        private static ServiceLogRepository _logRepoForProgram;
        private static TagRepository _tagRepo; // Added
        private static NetwatchConfigRepository _netwatchConfigRepo; // Added

        private const string SERVICE_NAME_FOR_LOGGING = "NetwatchPingerServiceHost";

        static async Task Main(string[] args)
        {
            Console.Title = "Netwatch Pinger Service (Manual Console)";

            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                _logRepoForProgram = new ServiceLogRepository();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Starting... Current directory: {Environment.CurrentDirectory}");
                _logRepoForProgram.WriteLog(new ServiceLogEntry { ServiceName = SERVICE_NAME_FOR_LOGGING, LogLevel = LogLevel.INFO.ToString(), Message = "Console Host: Service starting..." });

                // Initialize repositories that will be passed down
                _tagRepo = new TagRepository(); // Assuming TagRepository has a parameterless constructor or handles its own dependencies
                _netwatchConfigRepo = new NetwatchConfigRepository(_logRepoForProgram, _tagRepo); // Pass logger and tag repo

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: CRITICAL error during initial setup (logging or directory set): {ex.ToString()}");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            try
            {
                // Pass the already initialized repositories to NetwatchServiceControl
                // NetwatchServiceControl will then pass them to NetwatchServiceManager
                _serviceControl = new NetwatchServiceControl(_tagRepo, _netwatchConfigRepo, _logRepoForProgram);
                bool started = await _serviceControl.StartAsync();

                if (started)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Netwatch Pinger Service Control started successfully. Press any key to initiate shutdown.");
                    Console.ReadKey();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Netwatch Pinger Service Control FAILED to start. Check logs. Press any key to exit.");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }
            }
            catch (Exception ex)
            {
                string fatalMsg = "Console Host: FATAL UNHANDLED EXCEPTION during service lifecycle.";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] {fatalMsg} {ex.ToString()}");
                Console.ResetColor();
                _logRepoForProgram?.WriteLog(new ServiceLogEntry
                {
                    ServiceName = SERVICE_NAME_FOR_LOGGING,
                    LogLevel = LogLevel.FATAL.ToString(),
                    Message = fatalMsg,
                    ExceptionDetails = ex.ToString()
                });
                Console.WriteLine("The application encountered a fatal error. Press any key to exit.");
                Console.ReadKey();
                return;
            }
            finally
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Shutting down...");
                _logRepoForProgram?.WriteLog(new ServiceLogEntry { ServiceName = SERVICE_NAME_FOR_LOGGING, LogLevel = LogLevel.INFO.ToString(), Message = "Console Host: Initiating service shutdown..." });

                if (_serviceControl != null)
                {
                    await _serviceControl.StopAsync();
                }

                _logRepoForProgram?.WriteLog(new ServiceLogEntry { ServiceName = SERVICE_NAME_FOR_LOGGING, LogLevel = LogLevel.INFO.ToString(), Message = "Console Host: Shutdown complete." });
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Shutdown complete.");
            }
        }
    }
}
