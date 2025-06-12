using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public static class NotificationTemplateProvider
    {
        // Method to get available tokens based on the source feature
        public static List<string> GetAvailableTokens(string sourceFeature, string sourceEntityName = null)
        {
            var tokens = new List<string>();

            // Common tokens for all notifications
            tokens.Add("[RuleDescription]");
            tokens.Add("[Timestamp]");
            // tokens.Add("[SystemName]"); // Example of a truly global token

            if (sourceFeature == "Netwatch")
            {
                tokens.Add("[NetwatchName]");
                tokens.Add("[NetwatchStatus]");
                tokens.Add("[TriggeringIP]");
                tokens.Add("[TriggeringEntityName]");
                tokens.Add("[TriggeringIPStatus]");
                tokens.Add("[OutageDuration]");
            }
            // else if (sourceFeature == "AnotherFeature")
            // {
            //     // Add tokens specific to AnotherFeature
            // }
            // ... add more feature-specific tokens as your application grows

            return tokens;
        }

        // Method to get default Email Subject
        public static string GetDefaultEmailSubject(string sourceFeature, string sourceEntityName)
        {
            if (sourceFeature == "Netwatch")
            {
                return $"Notification: Netwatch '{sourceEntityName ?? "Selected Netwatch"}' - Status Change";
            }
            // else if (sourceFeature == "AnotherFeature") { /* ... */ }

            return "System Notification: [RuleDescription]"; // Generic default
        }

        // Method to get default Email Body
        public static string GetDefaultEmailBody(string sourceFeature, string sourceEntityName)
        {
            if (sourceFeature == "Netwatch")
            {
                return $"Dear User,\n\n" +
                       $"This is an automated notification regarding the Netwatch configuration: [NetwatchName].\n\n" +
                       $"Rule Description: [RuleDescription]\n" +
                       $"Current Status: [NetwatchStatus]\n" +
                       $"Timestamp: [Timestamp]\n\n" +
                       $"Details:\n" +
                       $"- Triggering IP: [TriggeringIP]\n" +
                       $"- Entity Name: [TriggeringEntityName]\n" +
                       $"- IP Status: [TriggeringIPStatus]\n" +
                       $"- Outage Duration (if applicable): [OutageDuration]\n\n" +
                       $"Please review the system for more details.\n\n" +
                       $"Thank you,\n" +
                       $"CSMT System";
            }
            // else if (sourceFeature == "AnotherFeature") { /* ... */ }

            return "Details:\n[MessageContent]\nTimestamp: [Timestamp]"; // Generic default
        }

        public static string GetDefaultSmsContent(string sourceFeature, string sourceEntityName)
        {
            if (sourceFeature == "Netwatch")
            {
                return $"Netwatch Alert: [NetwatchName] status is [NetwatchStatus]. Event at [Timestamp]. IP: [TriggeringIP]";
            }
            return "System Alert: [RuleDescription] - [MessageContent] at [Timestamp]";
        }

        public static string GetDefaultTelegramContent(string sourceFeature, string sourceEntityName)
        {
            if (sourceFeature == "Netwatch")
            {
                return $"🔔 *Netwatch Notification* 🔔\n\n" +
                       $"*Rule*: [RuleDescription]\n" +
                       $"*Configuration*: `[NetwatchName]`\n" +
                       $"*Current Status*: `[NetwatchStatus]`\n" +
                       $"*Time*: `[Timestamp]`\n" +
                       $"*Details*:\n" +
                       $"  - IP: `[TriggeringIP]`\n" +
                       $"  - Entity: `[TriggeringEntityName]`\n" +
                       $"  - IP Status: `[TriggeringIPStatus]`";
            }
            return "*System Notification*\nRule: [RuleDescription]\nMessage: [MessageContent]\nTime: [Timestamp]";
        }

        public static string GetDefaultFbMessengerContent(string sourceFeature, string sourceEntityName)
        {
            if (sourceFeature == "Netwatch")
            {
                return $"Messenger Alert: [NetwatchName] is now [NetwatchStatus]. Triggered by [TriggeringIP] ([TriggeringEntityName]) at [Timestamp]. Rule: [RuleDescription].";
            }
            return "FB Messenger Notification: [RuleDescription]. Details: [MessageContent]. Time: [Timestamp]";
        }
    }
}
