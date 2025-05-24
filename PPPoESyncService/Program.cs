using System;
using System.IO; // For Path.GetDirectoryName
using System.Reflection; // For Assembly.GetExecutingAssembly().Location
using PPPoESyncService.Services; // For PPPoESyncServiceControl
using SharedLibrary.DataAccess; // For ServiceLogRepository in case of unhandled exception logging
using SharedLibrary.Models;     // For ServiceLogEntry, LogLevel

namespace PPPoESyncService
{
    class Program
    {
        private static PPPoESyncServiceControl _serviceControl;
        private static ServiceLogRepository _logRepoForProgram;

        private const string SERVICE_NAME_FOR_LOGGING = "PPPoESyncServiceHost"; // Differentiates Program.cs logs

        static void Main(string[] args) // Correct Main signature for a console app
        {
            Console.Title = "PPPoE Sync Service (Manual Console)";

            try
            {
                // Set current directory to the application's startup path for relative path resolution (e.g., App.config)
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                _logRepoForProgram = new ServiceLogRepository();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Starting... Current directory: {Environment.CurrentDirectory}");
                _logRepoForProgram.WriteLog(new ServiceLogEntry { ServiceName = SERVICE_NAME_FOR_LOGGING, LogLevel = LogLevel.INFO.ToString(), Message = "Console Host: Service starting..." });
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
                // Pass the logger to PPPoESyncServiceControl
                _serviceControl = new PPPoESyncServiceControl(_logRepoForProgram);
                bool started = _serviceControl.Start(); // Start is synchronous

                if (started)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: PPPoE Sync Service Control started successfully. Press any key to initiate shutdown.");
                    Console.ReadKey();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: PPPoE Sync Service Control FAILED to start. Check logs. Press any key to exit.");
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
                    _serviceControl.Stop(); // Stop is synchronous
                }

                _logRepoForProgram?.WriteLog(new ServiceLogEntry { ServiceName = SERVICE_NAME_FOR_LOGGING, LogLevel = LogLevel.INFO.ToString(), Message = "Console Host: Shutdown complete." });
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{SERVICE_NAME_FOR_LOGGING}] CONSOLE: Shutdown complete.");
            }
        }
    }
}
