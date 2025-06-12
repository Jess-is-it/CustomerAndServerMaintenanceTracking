using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.Models.ViewModels
{
    public class NotificationRuleDisplayViewModel
    {
        public int RuleId { get; set; }
        public string Name { get; set; }
        public string TriggerConditionSummary { get; set; }
        public string NotificationTypesSummary { get; set; }
        public string MessageSummary { get; set; }
        public string RecipientsSummary { get; set; }
        public string ScheduleSummary { get; set; }
        public int RunCount { get; set; }
        public DateTime? NextRun { get; set; }
        public bool IsEnabled { get; set; }

        public string SourceFeature { get; set; }
        public int? SourceEntityId { get; set; }

        // Raw JSON strings from NotificationRule model
        public string NotificationChannelsJson { get; set; }
        public string ContentDetailsJson { get; set; }
        public string RecipientDetailsJson { get; set; }
        public string ScheduleDetailsJson { get; set; }

        public bool IsFullyConfigured
        {
            get
            {
                List<string> selectedChannelsList = null;
                bool hasChannels = false;
                if (!string.IsNullOrWhiteSpace(NotificationChannelsJson) && NotificationChannelsJson != "[]")
                {
                    try
                    {
                        selectedChannelsList = JsonConvert.DeserializeObject<List<string>>(NotificationChannelsJson);
                        hasChannels = selectedChannelsList != null && selectedChannelsList.Any();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing NotificationChannelsJson in IsFullyConfigured: {ex.Message}");
                        hasChannels = false;
                    }
                }

                bool hasContent = false;
                if (hasChannels && !string.IsNullOrWhiteSpace(ContentDetailsJson) && ContentDetailsJson != "{}")
                {
                    try
                    {
                        var content = JsonConvert.DeserializeObject<NotificationContentData>(ContentDetailsJson);
                        if (content != null && selectedChannelsList != null)
                        {
                            if (selectedChannelsList.Contains("Email") && (!string.IsNullOrWhiteSpace(content.EmailSubject) || !string.IsNullOrWhiteSpace(content.EmailBody)))
                                hasContent = true;
                            // Add similar checks for SMS, Telegram, etc. if they are mandatory for "hasContent"
                            // For example, if any selected channel has content, then hasContent = true
                            // If only Email is implemented for content for now, this is fine.
                            // If no specific channel content is found, hasContent remains false.
                            if (!hasContent && selectedChannelsList.Contains("SMS") && !string.IsNullOrWhiteSpace(content.SmsContent))
                                hasContent = true;
                            // etc. for other channels
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing ContentDetailsJson in IsFullyConfigured: {ex.Message}");
                        hasContent = false;
                    }
                }

                bool hasRecipients = false;
                if (!string.IsNullOrWhiteSpace(RecipientDetailsJson) && RecipientDetailsJson != "{}")
                {
                    try
                    {
                        var recipients = JsonConvert.DeserializeObject<NotificationRecipientData>(RecipientDetailsJson);
                        if (recipients != null &&
                            ((recipients.RoleIds?.Any() ?? false) ||
                             (recipients.UserAccountIds?.Any() ?? false) ||
                             (recipients.CustomerIdentifiers?.Any() ?? false) ||
                             (recipients.TagIds?.Any() ?? false) ||
                             (recipients.AdditionalEmails?.Any() ?? false) ||
                             (recipients.AdditionalPhones?.Any() ?? false)))
                        {
                            hasRecipients = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing RecipientDetailsJson in IsFullyConfigured: {ex.Message}");
                        hasRecipients = false;
                    }
                }

                bool hasSchedule = false;
                if (!string.IsNullOrWhiteSpace(ScheduleDetailsJson) && ScheduleDetailsJson != "{}")
                {
                    try
                    {
                        var schedule = JsonConvert.DeserializeObject<NotificationScheduleData>(ScheduleDetailsJson);
                        // A schedule is considered set if it deserializes and potentially has a StartDateTime
                        // (or other mandatory fields you define for a "set" schedule)
                        if (schedule != null && schedule.StartDateTime > DateTime.MinValue)
                        {
                            hasSchedule = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing ScheduleDetailsJson in IsFullyConfigured: {ex.Message}");
                        hasSchedule = false;
                    }
                }
                return hasChannels && hasContent && hasRecipients && hasSchedule;
            }

        }
        public bool IsSelectedForDeletion { get; set; } = false;
    }
}

