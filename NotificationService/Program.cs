using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NotificationService.Services;
using System.Threading;

namespace NotificationService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                // Now that the Output Type is "Console Application",
                // this check is reliable again.
                if (Environment.UserInteractive)
                {
                    // --- CONSOLE Mode ---
                    NotificationProcessingManager manager = new NotificationProcessingManager();
                    manager.Start();

                    Console.WriteLine("Press any key to stop the service.");
                    Console.ReadKey();

                    manager.Stop();
                }
                else
                {
                    // --- WINDOWS SERVICE Mode ---
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new NotificationServiceControl()
                    };
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception ex)
            {
                // The crash log is still useful for any unexpected errors.
                string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotificationService_CrashLog.txt");
                File.WriteAllText(errorLogPath, $"Service failed to start: {ex.ToString()}");
            }
        }
    }
}
