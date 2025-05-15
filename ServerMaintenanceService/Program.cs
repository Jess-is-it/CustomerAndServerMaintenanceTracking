using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.DataAccess;
using ServerMaintenanceService.Services;

namespace ServerMaintenanceService
{
    class Program
    {
        private static NetwatchServiceManager _netwatchManager;
        private static TagRepository _tagRepo;
        private static NetwatchConfigRepository _netwatchConfigRepo;
        // DatabaseHelper might be instantiated within repositories or passed to them.
        // Ensure its connection string is read from ServerMaintenanceService.exe.config

        static void Main(string[] args)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Process Started."); // Checkpoint 1

            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Entering TRY block in Main."); // Checkpoint 2

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Attempting to initialize TagRepository...");
                _tagRepo = new TagRepository(); // This might throw if DatabaseHelper or connection string is an issue
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: TagRepository Initialized successfully."); // Checkpoint 3

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Attempting to initialize NetwatchConfigRepository...");
                _netwatchConfigRepo = new NetwatchConfigRepository(); // This might also throw
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: NetwatchConfigRepository Initialized successfully."); // Checkpoint 4

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Attempting to initialize NetwatchServiceManager...");
                _netwatchManager = new NetwatchServiceManager(_tagRepo, _netwatchConfigRepo); // Constructor runs here (reads App.config)
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: NetwatchServiceManager Initialized successfully."); // Checkpoint 5

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Calling NetwatchServiceManager.InitializeAndStartAll()...");
                _netwatchManager.InitializeAndStartAll(); // Internal logs from this method should appear after this point
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: NetwatchServiceManager.InitializeAndStartAll() call completed."); // Checkpoint 6

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Netwatch Pinging Service is running. Press 'Q' to quit.");

                while (Console.ReadKey(true).Key != ConsoleKey.Q)
                {
                    // Keep alive for 'Q' to quit
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER FATAL ERROR in Main: {ex.ToString()}");
                Console.ResetColor();
                Console.WriteLine("The application encountered a fatal error and cannot continue.");
                Console.WriteLine("Please check the error message above. The console will close after you press any key.");
                Console.ReadKey(); // Keep window open to see the error
                return;
            }
            finally
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Entering FINALLY block. Attempting to dispose NetwatchServiceManager...");
                _netwatchManager?.Dispose();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER: Netwatch Pinging Service stopped / Main method ending.");
            }
        }
    }
}
