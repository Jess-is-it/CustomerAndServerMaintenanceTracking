using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NotificationService.Services
{
    public class EmailSender
    {
        private readonly EmailSettings _emailSettings;
        private bool _isConfigured = false;

        public EmailSender()
        {
            try
            {
                Console.WriteLine("[EmailSender] Initializing...");
                var settingsRepo = new EmailSettingsRepository();
                _emailSettings = settingsRepo.GetAllEmailSettings().FirstOrDefault(s => s.IsDefault);

                if (_emailSettings != null && !string.IsNullOrWhiteSpace(_emailSettings.SmtpServer))
                {
                    _isConfigured = true;
                    Console.WriteLine($"[EmailSender] Configuration loaded successfully. Using server '{_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}' with sender '{_emailSettings.SenderEmail}'.");
                }
                else
                {
                    _isConfigured = false;
                    Console.WriteLine("[EmailSender] WARNING: Default email settings not found or are incomplete in the database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailSender] CRITICAL ERROR during initialization: {ex.Message}");
                _isConfigured = false;
            }
        }

        public bool IsConfigured()
        {
            return _isConfigured;
        }

        public void SendEmail(string toAddress, string subject, string body)
        {
            if (!IsConfigured())
            {
                Console.WriteLine("[EmailSender] Cannot send email because sender is not configured.");
                return;
            }

            Console.WriteLine($"[EmailSender] Preparing to send email to '{toAddress}'.");
            Console.WriteLine($"[EmailSender] -> From: {_emailSettings.SenderEmail}");
            Console.WriteLine($"[EmailSender] -> Server: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");
            Console.WriteLine($"[EmailSender] -> EnableSsl: {_emailSettings.EnableSsl}");
            Console.WriteLine($"[EmailSender] -> Username: {_emailSettings.SmtpUsername}");

            try
            {
                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    client.EnableSsl = _emailSettings.EnableSsl;
                    if (!string.IsNullOrWhiteSpace(_emailSettings.SmtpUsername))
                    {
                        client.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                    }

                    var fromAddress = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderDisplayName);
                    var mailMessage = new MailMessage
                    {
                        From = fromAddress,
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false,
                    };
                    mailMessage.To.Add(toAddress);

                    // This line tells the application to accept all SSL certificates.
                    ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

                    Console.WriteLine("[EmailSender] SmtpClient configured. Attempting to send...");
                    client.Send(mailMessage);
                    Console.WriteLine($"[EmailSender] >>> Email successfully sent to '{toAddress}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailSender] FATAL ERROR sending email to {toAddress}: {ex.ToString()}");
            }
        }
    }
}
