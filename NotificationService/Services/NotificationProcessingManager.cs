using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace NotificationService.Services
{
    public class NotificationProcessingManager
    {
        private readonly NotificationRuleRepository _ruleRepository;
        private readonly NetwatchConfigRepository _netwatchConfigRepository;
        private readonly UserAccountRepository _userAccountRepository;
        private readonly CustomerRepository _customerRepository;
        private readonly TagRepository _tagRepository;
        private readonly ServiceLogRepository _serviceLogRepository;
        private readonly NotificationHistoryRepository _historyLogRepository; // <-- Add new repository
        private readonly EmailSender _emailSender;
        private Timer _timer;
        private const string ServiceName = "NotificationService";

        public NotificationProcessingManager()
        {
            _serviceLogRepository = new ServiceLogRepository();
            _historyLogRepository = new NotificationHistoryRepository(); // <-- Instantiate new repository
            _tagRepository = new TagRepository();
            _ruleRepository = new NotificationRuleRepository();
            _netwatchConfigRepository = new NetwatchConfigRepository(_serviceLogRepository, _tagRepository);
            _userAccountRepository = new UserAccountRepository();
            _customerRepository = new CustomerRepository();
            _emailSender = new EmailSender();
        }

        // This new helper method writes to the new history table
        private void LogHistory(int ruleId, LogLevel level, string message, Exception ex = null)
        {
            _historyLogRepository.WriteLog(new NotificationHistoryLog
            {
                RuleId = ruleId,
                LogLevel = level.ToString(),
                Message = message,
                ExceptionDetails = ex?.ToString()
            });
        }

        public void Start()
        {
            _timer = new Timer(60000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();

            _serviceLogRepository.WriteLog(new ServiceLogEntry { ServiceName = ServiceName, LogLevel = "INFO", Message = "Service started. Performing initial check..." });
            ProcessNotifications();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _serviceLogRepository.WriteLog(new ServiceLogEntry { ServiceName = ServiceName, LogLevel = "INFO", Message = "Service stopped." });
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _timer.Stop();
                ProcessNotifications();
            }
            catch (Exception ex)
            {
                _serviceLogRepository.WriteLog(new ServiceLogEntry { ServiceName = ServiceName, LogLevel = "ERROR", Message = $"An unhandled exception occurred in the service tick: {ex.Message}", ExceptionDetails = ex.ToString() });
            }
            finally
            {
                _timer.Start();
            }
        }

        public void ProcessNotifications()
        {
            _serviceLogRepository.WriteLog(new ServiceLogEntry { ServiceName = ServiceName, LogLevel = "INFO", Message = "Starting notification check..." });
            List<NotificationRule> activeRules = _ruleRepository.GetActiveNotificationRules();

            if (!activeRules.Any())
            {
                _serviceLogRepository.WriteLog(new ServiceLogEntry { ServiceName = ServiceName, LogLevel = "INFO", Message = "No active rules found." });
                return;
            }

            foreach (var rule in activeRules)
            {
                if (IsRuleScheduledToRun(rule))
                {
                    LogHistory(rule.Id, LogLevel.INFO, $"Rule is scheduled. Checking trigger...");

                    object triggerContext;
                    bool triggerMet = IsTriggerConditionMet(rule, out triggerContext);

                    if (triggerMet)
                    {
                        LogHistory(rule.Id, LogLevel.INFO, "TRIGGER MET.");
                        SendNotification(rule, triggerContext);
                    }
                    else
                    {
                        LogHistory(rule.Id, LogLevel.INFO, "Trigger condition not met.");
                    }

                    UpdateRuleAfterProcessing(rule);
                }
            }
        }

        private void SendNotification(NotificationRule rule, object triggerContext)
        {
            var recipients = GetRecipients(rule);
            if (!recipients.Any())
            {
                LogHistory(rule.Id, LogLevel.WARN, "No recipients found. Notification will not be sent.");
                return;
            }

            LogHistory(rule.Id, LogLevel.INFO, $"Found {recipients.Count} unique recipient(s): {string.Join(", ", recipients)}");

            var channels = JsonConvert.DeserializeObject<List<string>>(rule.NotificationChannelsJson);

            foreach (string channel in channels)
            {
                if (channel == "Email" && _emailSender.IsConfigured())
                {
                    var content = JsonConvert.DeserializeObject<NotificationContentData>(rule.ContentDetailsJson);
                    if (content == null)
                    {
                        LogHistory(rule.Id, LogLevel.WARN, "Content is not configured for this rule. Skipping email.");
                        continue;
                    };

                    string subject = BuildMessage(content.EmailSubject, rule, triggerContext);
                    string body = BuildMessage(content.EmailBody, rule, triggerContext);

                    foreach (var emailAddress in recipients)
                    {
                        LogHistory(rule.Id, LogLevel.INFO, $"Assembling and sending email to '{emailAddress}'.");
                        _emailSender.SendEmail(emailAddress, subject, body);
                    }
                }
            }
        }

        private void UpdateRuleAfterProcessing(NotificationRule rule)
        {
            DateTime lastRun = DateTime.Now;
            DateTime? nextRun = null;

            if (!string.IsNullOrWhiteSpace(rule.ScheduleDetailsJson) && rule.ScheduleDetailsJson != "{}")
            {
                var schedule = JsonConvert.DeserializeObject<NotificationScheduleData>(rule.ScheduleDetailsJson);
                if (schedule.IsRecurring && schedule.IntervalSeconds.HasValue && schedule.IntervalSeconds > 0)
                {
                    nextRun = lastRun.AddSeconds(schedule.IntervalSeconds.Value);
                }
            }

            _ruleRepository.UpdateRuleAfterProcessing(rule.Id, lastRun, nextRun);
            LogHistory(rule.Id, LogLevel.INFO, $"Rule processing finished. Next Run updated to: {nextRun?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}.");
        }

        // ... All other helper methods (GetRecipients, IsNetwatchTriggerMet, etc.) remain the same ...
        private bool IsTriggerConditionMet(NotificationRule rule, out object triggerContext)
        {
            triggerContext = null;
            if (string.IsNullOrWhiteSpace(rule.TriggerDetailsJson) || rule.TriggerDetailsJson == "{}") return false;

            try
            {
                switch (rule.SourceFeature)
                {
                    case "Netwatch":
                        return IsNetwatchTriggerMet(rule, out triggerContext);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogHistory(rule.Id, LogLevel.ERROR, "Failed to check trigger.", ex);
                return false;
            }
        }

        private bool IsNetwatchTriggerMet(NotificationRule rule, out object triggerContext)
        {
            triggerContext = null;
            try
            {
                var triggerDetails = JsonConvert.DeserializeObject<NetwatchTriggerDetails>(rule.TriggerDetailsJson);
                if (triggerDetails == null || !triggerDetails.SelectedNetwatchConfigIds.Any() || !triggerDetails.SelectedNetwatchStatuses.Any()) return false;

                foreach (var configId in triggerDetails.SelectedNetwatchConfigIds)
                {
                    var netwatchConfig = _netwatchConfigRepository.GetNetwatchConfigStatus(configId);
                    if (netwatchConfig == null || string.IsNullOrWhiteSpace(netwatchConfig.LastStatus)) continue;

                    if (triggerDetails.SelectedNetwatchStatuses.Contains(netwatchConfig.LastStatus))
                    {
                        triggerContext = netwatchConfig;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogHistory(rule.Id, LogLevel.ERROR, $"Error checking Netwatch trigger.", ex);
                return false;
            }
        }

        private string BuildMessage(string template, NotificationRule rule, object triggerContext)
        {
            if (string.IsNullOrWhiteSpace(template)) return "";

            template = template.Replace("[RuleDescription]", rule.Description);
            template = template.Replace("[RuleName]", rule.RuleName);
            template = template.Replace("[Timestamp]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            if (rule.SourceFeature == "Netwatch" && triggerContext is NetwatchConfigDisplay netwatchContext)
            {
                template = template.Replace("[NetwatchName]", netwatchContext.NetwatchName);
                template = template.Replace("[NetwatchStatus]", netwatchContext.LastStatus);
                template = template.Replace("[TriggeringIP]", "N/A (Overall Status Change)");
                template = template.Replace("[TriggeringEntityName]", "N/A");
                template = template.Replace("[TriggeringIPStatus]", "N/A");
                template = template.Replace("[OutageDuration]", "N/A");
            }

            return template;
        }

        private List<string> GetRecipients(NotificationRule rule)
        {
            var emailList = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(rule.RecipientDetailsJson) || rule.RecipientDetailsJson == "{}")
            {
                return new List<string>();
            }

            var recipientData = JsonConvert.DeserializeObject<NotificationRecipientData>(rule.RecipientDetailsJson);
            if (recipientData == null) return new List<string>();

            recipientData.AdditionalEmails?.ForEach(email => {
                if (!string.IsNullOrWhiteSpace(email)) emailList.Add(email);
            });

            if (recipientData.UserAccountIds != null && recipientData.UserAccountIds.Any())
            {
                var users = _userAccountRepository.GetUserAccountsWithRoles()
                                .Where(u => recipientData.UserAccountIds.Contains(u.Id) && !string.IsNullOrWhiteSpace(u.Email));
                foreach (var user in users) emailList.Add(user.Email);
            }

            if (recipientData.RoleIds != null && recipientData.RoleIds.Any())
            {
                foreach (var roleId in recipientData.RoleIds)
                {
                    var usersInRole = _userAccountRepository.GetUsersByRoleId(roleId);
                    foreach (var user in usersInRole)
                    {
                        if (!string.IsNullOrWhiteSpace(user.Email)) emailList.Add(user.Email);
                    }
                }
            }

            if (recipientData.CustomerIdentifiers != null && recipientData.CustomerIdentifiers.Any())
            {
                foreach (var customerIdStr in recipientData.CustomerIdentifiers)
                {
                    if (int.TryParse(customerIdStr, out int customerId))
                    {
                        var customer = _customerRepository.GetCustomerById(customerId);
                        if (customer != null && !string.IsNullOrWhiteSpace(customer.Email)) emailList.Add(customer.Email);
                    }
                }
            }

            if (recipientData.TagIds != null && recipientData.TagIds.Any())
            {
                foreach (var tagId in recipientData.TagIds)
                {
                    var taggedCustomers = _tagRepository.GetCustomersByTagId(tagId);
                    foreach (var customer in taggedCustomers)
                    {
                        if (!string.IsNullOrWhiteSpace(customer.Email)) emailList.Add(customer.Email);
                    }
                }
            }

            return emailList.ToList();
        }

        private bool IsRuleScheduledToRun(NotificationRule rule)
        {
            if (string.IsNullOrWhiteSpace(rule.ScheduleDetailsJson) || rule.ScheduleDetailsJson == "{}")
            {
                if (rule.LastRunTime.HasValue) return false;
                return DateTime.Now >= (rule.NextRunTime ?? DateTime.Now);
            }

            try
            {
                var schedule = JsonConvert.DeserializeObject<NotificationScheduleData>(rule.ScheduleDetailsJson);
                DateTime now = DateTime.Now;

                if (now < schedule.StartDateTime) return false;
                if (!schedule.IsRecurring) return !rule.LastRunTime.HasValue;

                if (schedule.DaysOfWeek != null && schedule.DaysOfWeek.Any())
                {
                    if (!schedule.DaysOfWeek.Contains(now.DayOfWeek)) return false;
                }

                DateTime nextScheduledRun = rule.NextRunTime ?? schedule.StartDateTime;
                return now >= nextScheduledRun;
            }
            catch (Exception ex)
            {
                LogHistory(rule.Id, LogLevel.ERROR, $"Failed to parse schedule.", ex);
                return false;
            }
        }
    }
}
