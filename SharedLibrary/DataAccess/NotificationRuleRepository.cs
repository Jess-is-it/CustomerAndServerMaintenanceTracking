using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class NotificationRuleRepository
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ServiceLogRepository _logRepository; // Optional, for logging within the repo
        private const string REPO_LOG_SOURCE = "NotificationRuleRepo";

        public NotificationRuleRepository(ServiceLogRepository logRepository = null)
        {
            _dbHelper = new DatabaseHelper();
            _logRepository = logRepository ?? new ServiceLogRepository(); // Initialize if null
        }

        private void Log(LogLevel level, string message, Exception ex = null, int? ruleId = null)
        {
            string context = ruleId.HasValue ? $"RuleId {ruleId}: " : "";
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = REPO_LOG_SOURCE,
                LogLevel = level.ToString(),
                Message = context + message,
                ExceptionDetails = ex?.ToString()
            });
        }

        public int AddInitialRule(NotificationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            int newRuleId = 0;
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO NotificationRules 
                                (RuleName, Description, NotificationChannelsJson, 
                                 SourceFeature, SourceEntityId, TriggerDetailsJson, 
                                 IsEnabled, DateCreated, RunCount)
                    OUTPUT INSERTED.Id
                    VALUES      (@RuleName, @Description, @NotificationChannelsJson,
                                 @SourceFeature, @SourceEntityId, @TriggerDetailsJson,
                                 @IsEnabled, @DateCreated, @RunCount);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleName", rule.RuleName);
                    cmd.Parameters.AddWithValue("@Description", (object)rule.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NotificationChannelsJson", string.IsNullOrWhiteSpace(rule.NotificationChannelsJson) ? (object)DBNull.Value : rule.NotificationChannelsJson);
                    cmd.Parameters.AddWithValue("@SourceFeature", (object)rule.SourceFeature ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SourceEntityId", rule.SourceEntityId.HasValue ? (object)rule.SourceEntityId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TriggerDetailsJson", string.IsNullOrWhiteSpace(rule.TriggerDetailsJson) ? (object)DBNull.Value : rule.TriggerDetailsJson);
                    cmd.Parameters.AddWithValue("@IsEnabled", rule.IsEnabled);
                    cmd.Parameters.AddWithValue("@DateCreated", rule.DateCreated);
                    cmd.Parameters.AddWithValue("@RunCount", rule.RunCount);
                    // ContentDetailsJson, RecipientDetailsJson, ScheduleDetailsJson will be updated later

                    try
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            newRuleId = Convert.ToInt32(result);
                            Log(LogLevel.INFO, $"Initial notification rule '{rule.RuleName}' added with ID: {newRuleId}.");
                        }
                        else
                        {
                            Log(LogLevel.ERROR, $"Failed to retrieve ID for new notification rule '{rule.RuleName}'. ExecuteScalar returned null or DBNull.");
                            throw new Exception("Failed to create notification rule: Could not retrieve new ID.");
                        }
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error adding initial notification rule '{rule.RuleName}'.", ex);
                        throw; // Re-throw to be handled by calling UI
                    }
                }
            }
            return newRuleId;
        }

        public NotificationRule GetRuleById(int ruleId)
        {
            NotificationRule rule = null;
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Select all relevant fields
                string query = @"SELECT Id, RuleName, Description, NotificationChannelsJson, 
                                SourceFeature, SourceEntityId, TriggerDetailsJson, 
                                ContentDetailsJson, RecipientDetailsJson, ScheduleDetailsJson,
                                IsEnabled, DateCreated, LastModified, LastRunTime, NextRunTime, RunCount
                         FROM NotificationRules WHERE Id = @RuleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                rule = new NotificationRule
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    RuleName = reader["RuleName"].ToString(),
                                    Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                                    NotificationChannelsJson = reader["NotificationChannelsJson"] == DBNull.Value ? "[]" : reader["NotificationChannelsJson"].ToString(),
                                    SourceFeature = reader["SourceFeature"] == DBNull.Value ? null : reader["SourceFeature"].ToString(),
                                    SourceEntityId = reader["SourceEntityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SourceEntityId"]),
                                    TriggerDetailsJson = reader["TriggerDetailsJson"] == DBNull.Value ? "{}" : reader["TriggerDetailsJson"].ToString(),
                                    ContentDetailsJson = reader["ContentDetailsJson"] == DBNull.Value ? null : reader["ContentDetailsJson"].ToString(),
                                    RecipientDetailsJson = reader["RecipientDetailsJson"] == DBNull.Value ? null : reader["RecipientDetailsJson"].ToString(),
                                    ScheduleDetailsJson = reader["ScheduleDetailsJson"] == DBNull.Value ? null : reader["ScheduleDetailsJson"].ToString(),
                                    IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                                    DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                    LastModified = reader["LastModified"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastModified"]),
                                    LastRunTime = reader["LastRunTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastRunTime"]),
                                    NextRunTime = reader["NextRunTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NextRunTime"]),
                                    RunCount = Convert.ToInt32(reader["RunCount"])
                                };
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error getting notification rule by ID {ruleId}.", ex, ruleId);
                        throw;
                    }
                }
            }
            return rule;
        }

        public bool UpdateNotificationRuleContent(int ruleId, string contentDetailsJson)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE NotificationRules 
                         SET ContentDetailsJson = @ContentDetailsJson, LastModified = @LastModified
                         WHERE Id = @RuleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ContentDetailsJson", (object)contentDetailsJson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Content updated for notification rule ID {ruleId}.");
                            return true;
                        }
                        Log(LogLevel.WARN, $"No rule found with ID {ruleId} to update content.", null, ruleId);
                        return false;
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error updating content for notification rule ID {ruleId}.", ex, ruleId);
                        throw;
                    }
                }
            }
        }

        public bool UpdateNotificationRuleRecipients(int ruleId, string recipientDetailsJson)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE NotificationRules 
                         SET RecipientDetailsJson = @RecipientDetailsJson, LastModified = @LastModified
                         WHERE Id = @RuleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RecipientDetailsJson", string.IsNullOrWhiteSpace(recipientDetailsJson) ? (object)DBNull.Value : recipientDetailsJson);
                    cmd.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Recipients updated for rule ID {ruleId}.");
                            return true;
                        }
                        Log(LogLevel.WARN, $"No rule found with ID {ruleId} to update recipients.", null, ruleId);
                        return false;
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error updating recipients for rule ID {ruleId}.", ex, ruleId);
                        throw;
                    }
                }
            }
        }

        public bool UpdateNotificationRuleSchedule(int ruleId, string scheduleDetailsJson)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE NotificationRules 
                         SET ScheduleDetailsJson = @ScheduleDetailsJson, LastModified = @LastModified
                         WHERE Id = @RuleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ScheduleDetailsJson", string.IsNullOrWhiteSpace(scheduleDetailsJson) ? (object)DBNull.Value : scheduleDetailsJson);
                    cmd.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Schedule updated for notification rule ID {ruleId}.");
                            return true;
                        }
                        Log(LogLevel.WARN, $"No rule found with ID {ruleId} to update schedule.", null, ruleId);
                        return false;
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error updating schedule for rule ID {ruleId}.", ex, ruleId);
                        throw;
                    }
                }
            }
        }
        public List<NotificationRule> GetNotificationRulesForManagerDisplay()
        {
            List<NotificationRule> rules = new List<NotificationRule>();
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT Id, RuleName, Description, NotificationChannelsJson, 
                                SourceFeature, SourceEntityId, TriggerDetailsJson, 
                                ContentDetailsJson, RecipientDetailsJson, ScheduleDetailsJson,
                                IsEnabled, DateCreated, LastModified, LastRunTime, NextRunTime, RunCount
                         FROM NotificationRules ORDER BY DateCreated DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var rule = new NotificationRule // Use the main model
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    RuleName = reader["RuleName"].ToString(),
                                    Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                                    NotificationChannelsJson = reader["NotificationChannelsJson"] == DBNull.Value ? "[]" : reader["NotificationChannelsJson"].ToString(),
                                    SourceFeature = reader["SourceFeature"] == DBNull.Value ? null : reader["SourceFeature"].ToString(),
                                    SourceEntityId = reader["SourceEntityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SourceEntityId"]),
                                    TriggerDetailsJson = reader["TriggerDetailsJson"] == DBNull.Value ? "{}" : reader["TriggerDetailsJson"].ToString(),
                                    ContentDetailsJson = reader["ContentDetailsJson"] == DBNull.Value ? null : reader["ContentDetailsJson"].ToString(),
                                    RecipientDetailsJson = reader["RecipientDetailsJson"] == DBNull.Value ? null : reader["RecipientDetailsJson"].ToString(),
                                    ScheduleDetailsJson = reader["ScheduleDetailsJson"] == DBNull.Value ? null : reader["ScheduleDetailsJson"].ToString(),
                                    IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                                    DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                    LastModified = reader["LastModified"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastModified"]),
                                    LastRunTime = reader["LastRunTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastRunTime"]),
                                    NextRunTime = reader["NextRunTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NextRunTime"]),
                                    RunCount = Convert.ToInt32(reader["RunCount"])
                                };
                                rules.Add(rule);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(LogLevel.ERROR, "Error getting notification rules for display.", ex);
                        // Depending on your error handling strategy, you might rethrow or return empty list
                    }
                }
            }
            return rules;
        }

        public bool UpdateNotificationRuleStatus(int ruleId, bool isEnabled)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE NotificationRules SET IsEnabled = @IsEnabled, LastModified = @LastModified WHERE Id = @RuleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IsEnabled", isEnabled);
                    cmd.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        Log(LogLevel.INFO, $"Status updated to {isEnabled} for rule ID {ruleId}.");
                        return rowsAffected > 0;
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error updating status for rule ID {ruleId}.", ex, ruleId);
                        throw;
                    }
                }
            }
        }

        // --- Helper methods for summaries (to be placed in NotificationRuleRepository or a helper class) ---
        private string FormatSummary(List<string> items)
        {
            if (items == null || !items.Any()) return "N/A";
            return string.Join(", ", items);
        }

        private string FormatTriggerSummary(string sourceFeature, string triggerJson, int? sourceEntityId, string ruleNameForContext)
        {
            if (string.IsNullOrWhiteSpace(triggerJson) || triggerJson == "{}") return "Not Set";
            if (sourceFeature == "Netwatch")
            {
                try
                {
                    var details = JsonConvert.DeserializeObject<NetwatchTriggerDetails>(triggerJson);
                    if (details == null || !details.SelectedNetwatchConfigIds.Any() || !details.SelectedNetwatchStatuses.Any()) return "Netwatch: Incomplete";

                    // Fetching Netwatch name here can be slow for a list. Consider passing it or using IDs.
                    // For now, using sourceEntityName passed to AddNotificationRule for initial context, or rule name.
                    string netwatchName = "Netwatch"; // Fallback
                    if (details.SelectedNetwatchConfigIds.Count == 1 && sourceEntityId.HasValue && details.SelectedNetwatchConfigIds.First() == sourceEntityId.Value)
                    {
                        // If the trigger is for the same Netwatch config this rule was created 'for'
                        // We might need to fetch NetwatchConfig name if sourceEntityName is not specific enough
                        // This detail is best handled by passing the sourceEntityName (Netwatch config name) to the DisplayModel
                        // Or, the DisplayModel could fetch it if needed. For now, let's assume ruleNameForContext is good.
                        netwatchName = ruleNameForContext.Replace("Alert for ", "").Replace("Add Notification: ", ""); // Attempt to get original name
                    }
                    else if (details.SelectedNetwatchConfigIds.Count > 0)
                    {
                        netwatchName = $"{details.SelectedNetwatchConfigIds.Count} Config(s)";
                    }


                    string ipSummary = details.TriggerOnAllIPs ? "All IPs" : $"{details.SelectedSpecificIPs?.Count ?? 0} Specific IP(s)";
                    return $"Netwatch: {netwatchName} ({ipSummary}) -> {FormatSummary(details.SelectedNetwatchStatuses)}";
                }
                catch { return "Netwatch: Invalid Trigger Data"; }
            }
            return "Trigger Not Set";
        }

        private string FormatContentSummary(string contentJson, List<string> channels)
        {
            if (string.IsNullOrWhiteSpace(contentJson) || contentJson == "{}") return "Not Set";
            if (channels == null || !channels.Any()) return "Not Set (No Channels)";
            try
            {
                var content = JsonConvert.DeserializeObject<NotificationContentData>(contentJson);
                List<string> populatedChannels = new List<string>();
                if (channels.Contains("Email") && (!string.IsNullOrEmpty(content.EmailSubject) || !string.IsNullOrEmpty(content.EmailBody))) populatedChannels.Add("Email");
                // Add for SMS, etc.
                return populatedChannels.Any() ? $"Content for: {FormatSummary(populatedChannels)}" : "Content Not Set";
            }
            catch { return "Invalid Content Data"; }
        }

        private string FormatRecipientSummary(string recipientsJson)
        {
            if (string.IsNullOrWhiteSpace(recipientsJson) || recipientsJson == "{}") return "Not Set";
            try
            {
                var recipients = JsonConvert.DeserializeObject<NotificationRecipientData>(recipientsJson);
                List<string> parts = new List<string>();
                if (recipients.RoleIds.Any()) parts.Add($"{recipients.RoleIds.Count} Role(s)");
                if (recipients.UserAccountIds.Any()) parts.Add($"{recipients.UserAccountIds.Count} User(s)");
                if (recipients.CustomerIdentifiers.Any()) parts.Add($"{recipients.CustomerIdentifiers.Count} Customer(s)");
                if (recipients.TagIds.Any()) parts.Add($"{recipients.TagIds.Count} Tag(s)");
                if (recipients.AdditionalEmails.Any()) parts.Add($"{recipients.AdditionalEmails.Count} Email(s)");
                if (recipients.AdditionalPhones.Any()) parts.Add($"{recipients.AdditionalPhones.Count} Phone(s)");
                return parts.Any() ? FormatSummary(parts) : "No Recipients Set";
            }
            catch { return "Invalid Recipient Data"; }
        }

        private string FormatScheduleSummary(string scheduleJson)
        {
            if (string.IsNullOrWhiteSpace(scheduleJson) || scheduleJson == "{}") return "Not Set / One Time";
            try
            {
                var schedule = JsonConvert.DeserializeObject<NotificationScheduleData>(scheduleJson);
                if (schedule == null) return "Not Set";

                string summary = $"Starts: {schedule.StartDateTime:g}";
                if (schedule.IsRecurring && schedule.IntervalSeconds.HasValue)
                {
                    summary += $", Repeats every {TimeSpan.FromSeconds(schedule.IntervalSeconds.Value).TotalMinutes:N0} min"; // Example
                    if (schedule.DaysOfWeek.Any() && schedule.DaysOfWeek.Count < 7)
                    {
                        summary += $" on {string.Join(", ", schedule.DaysOfWeek.Select(d => d.ToString().Substring(0, 3)))}";
                    }
                    else if (schedule.DaysOfWeek.Any() && schedule.DaysOfWeek.Count == 7)
                    {
                        summary += " (Daily)";
                    }
                }
                else
                {
                    summary += " (One Time)";
                }
                if (schedule.TriggerTrueDurationSeconds > 0)
                {
                    summary += $", After Cond. True for {schedule.TriggerTrueDurationSeconds}s";
                }
                return summary;
            }
            catch { return "Invalid Schedule Data"; }
        }

        public bool UpdateInitialRuleDetails(NotificationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            UPDATE NotificationRules 
            SET RuleName = @RuleName, 
                Description = @Description, 
                NotificationChannelsJson = @NotificationChannelsJson,
                SourceFeature = @SourceFeature, 
                SourceEntityId = @SourceEntityId, 
                TriggerDetailsJson = @TriggerDetailsJson,
                LastModified = @LastModified
            WHERE Id = @Id;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleName", rule.RuleName);
                    cmd.Parameters.AddWithValue("@Description", (object)rule.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NotificationChannelsJson", string.IsNullOrWhiteSpace(rule.NotificationChannelsJson) ? (object)DBNull.Value : rule.NotificationChannelsJson);
                    cmd.Parameters.AddWithValue("@SourceFeature", (object)rule.SourceFeature ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SourceEntityId", rule.SourceEntityId.HasValue ? (object)rule.SourceEntityId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TriggerDetailsJson", string.IsNullOrWhiteSpace(rule.TriggerDetailsJson) ? (object)DBNull.Value : rule.TriggerDetailsJson);
                    cmd.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Id", rule.Id);

                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Initial details updated for notification rule ID {rule.Id} ('{rule.RuleName}').");
                            return true;
                        }
                        Log(LogLevel.WARN, $"No rule found with ID {rule.Id} to update initial details.", null, rule.Id);
                        return false;
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error updating initial details for rule ID {rule.Id} ('{rule.RuleName}').", ex, rule.Id);
                        throw;
                    }
                }
            }
        }
        public bool DeleteNotificationRule(int ruleId)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Consider related data: If NotificationRule is FK in other tables with CASCADE, they'll be deleted.
                // Otherwise, you might need to delete related data manually or handle FK constraints.
                string query = "DELETE FROM NotificationRules WHERE Id = @RuleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Deleted notification rule ID {ruleId}.");
                            return true;
                        }
                        Log(LogLevel.WARN, $"No rule found with ID {ruleId} to delete.", null, ruleId);
                        return false;
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL error deleting rule ID {ruleId}.", ex, ruleId);
                        throw;
                    }
                }
            }
        }


        public List<NotificationRule> GetActiveNotificationRules()
        {
            var rules = new List<NotificationRule>();
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT * FROM NotificationRules WHERE IsEnabled = 1";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rules.Add(MapToNotificationRule(reader));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(LogLevel.ERROR, "Error getting active notification rules.", ex);
                    }
                }
            }
            return rules;
        }

        private NotificationRule MapToNotificationRule(SqlDataReader reader)
        {
            return new NotificationRule
            {
                Id = Convert.ToInt32(reader["Id"]),
                RuleName = reader["RuleName"].ToString(),
                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                NotificationChannelsJson = reader["NotificationChannelsJson"] == DBNull.Value ? "[]" : reader["NotificationChannelsJson"].ToString(),
                SourceFeature = reader["SourceFeature"] == DBNull.Value ? null : reader["SourceFeature"].ToString(),
                SourceEntityId = reader["SourceEntityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SourceEntityId"]),
                TriggerDetailsJson = reader["TriggerDetailsJson"] == DBNull.Value ? "{}" : reader["TriggerDetailsJson"].ToString(),
                ContentDetailsJson = reader["ContentDetailsJson"] == DBNull.Value ? null : reader["ContentDetailsJson"].ToString(),
                RecipientDetailsJson = reader["RecipientDetailsJson"] == DBNull.Value ? null : reader["RecipientDetailsJson"].ToString(),
                ScheduleDetailsJson = reader["ScheduleDetailsJson"] == DBNull.Value ? null : reader["ScheduleDetailsJson"].ToString(),
                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                LastModified = reader["LastModified"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastModified"]),
                LastRunTime = reader["LastRunTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastRunTime"]),
                NextRunTime = reader["NextRunTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NextRunTime"]),
                RunCount = Convert.ToInt32(reader["RunCount"])
            };
        }
        public bool UpdateRuleAfterProcessing(int ruleId, DateTime lastRun, DateTime? nextRun)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Increment RunCount and update the time fields
                string query = @"
            UPDATE NotificationRules SET 
                LastRunTime = @LastRunTime, 
                NextRunTime = @NextRunTime,
                RunCount = RunCount + 1
            WHERE Id = @RuleId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LastRunTime", lastRun);
                    cmd.Parameters.AddWithValue("@NextRunTime", (object)nextRun ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        public bool ResetRuleSchedule(int ruleId)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // This resets the run history, forcing re-evaluation.
                string query = @"
            UPDATE NotificationRules SET 
                RunCount = 0,
                LastRunTime = NULL, 
                NextRunTime = NULL
            WHERE Id = @RuleId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
