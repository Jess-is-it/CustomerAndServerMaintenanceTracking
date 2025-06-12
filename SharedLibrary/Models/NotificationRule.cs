using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class NotificationRule
    {
        public int Id { get; set; } // Primary Key
        public string RuleName { get; set; }
        public string Description { get; set; } // Can be same as RuleName or more detailed

        // Details & Type
        public string NotificationChannelsJson { get; set; } // JSON string of List<string> (e.g., ["Email", "SMS"])

        // Trigger Condition
        public string SourceFeature { get; set; } // e.g., "Netwatch", "CustomerEvent", etc.
        public int? SourceEntityId { get; set; }   // ID of the entity that this rule is primarily associated with (e.g., NetwatchConfig.Id if source is NetwatchList)
                                                   // This can be NULL if the rule is more generic.
        public string TriggerDetailsJson { get; set; } // JSON string for feature-specific trigger conditions

        // Placeholders for data from subsequent forms (will be updated later)
        public string ContentDetailsJson { get; set; }   // Nullable
        public string RecipientDetailsJson { get; set; } // Nullable
        public string ScheduleDetailsJson { get; set; }  // Nullable

        // Operational Fields
        public bool IsEnabled { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; } // For scheduler
        public int RunCount { get; set; }

        public NotificationRule()
        {
            DateCreated = DateTime.Now;
            IsEnabled = true; // Default to enabled
            RunCount = 0;
            // Initialize JSON strings to empty or null as appropriate
            NotificationChannelsJson = "[]"; // Empty JSON array
            TriggerDetailsJson = "{}";       // Empty JSON object
        }
    }

    // Example structure for TriggerDetails (when _sourceFeature is "Netwatch")
    // You might want to define concrete classes for these later for deserialization.
    public class NetwatchTriggerDetails
    {
        public List<int> SelectedNetwatchConfigIds { get; set; } = new List<int>();
        public bool TriggerOnAllIPs { get; set; }
        public List<string> SelectedSpecificIPs { get; set; } = new List<string>();
        public List<string> SelectedNetwatchStatuses { get; set; } = new List<string>();
    }

    // Example for RecipientDetails (can be expanded)
    public class NotificationRecipientData
    {
        public List<int> RoleIds { get; set; } = new List<int>();
        public List<int> UserAccountIds { get; set; } = new List<int>();
        public List<string> CustomerIdentifiers { get; set; } = new List<string>();
        public List<int> TagIds { get; set; } = new List<int>();
        public List<string> AdditionalEmails { get; set; } = new List<string>();
        public List<string> AdditionalPhones { get; set; } = new List<string>();

        // Add this method
        public bool IsEmpty()
        {
            return !(RoleIds?.Any() ?? false) &&
                   !(UserAccountIds?.Any() ?? false) &&
                   !(CustomerIdentifiers?.Any() ?? false) &&
                   !(TagIds?.Any() ?? false) &&
                   !(AdditionalEmails?.Any() ?? false) &&
                   !(AdditionalPhones?.Any() ?? false);
        }
    }

    // Example for ScheduleDetails
    public class NotificationScheduleData
    {
        public DateTime StartDateTime { get; set; }
        public int? IntervalSeconds { get; set; } // Nullable if one-time
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>(); // Empty if not day-specific repeat
        public int TriggerTrueDurationSeconds { get; set; } // How long condition must be true
        public bool IsRecurring => IntervalSeconds.HasValue && IntervalSeconds > 0;
    }

    // Example for ContentDetails
    public class NotificationContentData
    {
        public string SmsContent { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public string TelegramContent { get; set; }
        public string FbMessengerContent { get; set; }
    }
}
