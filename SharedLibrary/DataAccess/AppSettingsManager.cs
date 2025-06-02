using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public static class AppSettingsManager
    {
        public static EmailSettings LoadEmailSettings()
        {
            EmailSettings settings = new EmailSettings(); // Initializes with defaults
            try
            {
                settings.SmtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "";
                if (int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out int port))
                {
                    settings.SmtpPort = port;
                }
                if (bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"], out bool enableSsl))
                {
                    settings.EnableSsl = enableSsl;
                }
                settings.SmtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                settings.SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? ""; // No decryption here as per user request
                settings.SenderEmail = ConfigurationManager.AppSettings["SenderEmail"] ?? "";
                settings.SenderDisplayName = ConfigurationManager.AppSettings["SenderDisplayName"] ?? settings.SenderDisplayName; // Keep default if not set
            }
            catch (ConfigurationErrorsException ex)
            {
                Console.WriteLine($"Error loading email settings from App.config: {ex.Message}");
                // Return defaults or throw, depending on how critical these settings are at load time.
            }
            return settings;
        }

        public static bool SaveEmailSettings(EmailSettings settings)
        {
            try
            {
                Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppSettingsSection appSettings = (AppSettingsSection)configFile.GetSection("appSettings");

                if (appSettings == null)
                {
                    Console.WriteLine("Error: AppSettings section not found in App.config.");
                    return false;
                }

                // Helper to update or add key
                Action<string, string> updateKey = (key, value) => {
                    if (appSettings.Settings[key] == null)
                        appSettings.Settings.Add(key, value);
                    else
                        appSettings.Settings[key].Value = value;
                };

                updateKey("SmtpServer", settings.SmtpServer);
                updateKey("SmtpPort", settings.SmtpPort.ToString());
                updateKey("SmtpEnableSsl", settings.EnableSsl.ToString().ToLower()); // Save as "true" or "false"
                updateKey("SmtpUsername", settings.SmtpUsername);
                updateKey("SmtpPassword", settings.SmtpPassword); // Saving plain text as per request
                updateKey("SenderEmail", settings.SenderEmail);
                updateKey("SenderDisplayName", settings.SenderDisplayName);

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings"); // Refresh the section to reflect changes
                return true;
            }
            catch (ConfigurationErrorsException ex)
            {
                Console.WriteLine($"Error saving email settings to App.config: {ex.Message}");
                // Consider showing a MessageBox to the user or logging more formally.
                return false;
            }
        }
    }
}
