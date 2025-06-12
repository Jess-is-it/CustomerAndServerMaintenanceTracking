using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class NotificationHistoryRepository
    {
        private readonly DatabaseHelper _dbHelper;

        public NotificationHistoryRepository()
        {
            _dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Writes a notification history log entry to the database.
        /// </summary>
        public void WriteLog(NotificationHistoryLog logEntry)
        {
            // --- Start of Diagnostic Code ---
            Console.WriteLine($"[HistoryRepo] Attempting to log message for Rule ID {logEntry.RuleId}: '{logEntry.Message}'");
            // --- End of Diagnostic Code ---

            if (logEntry == null || string.IsNullOrWhiteSpace(logEntry.Message)) return;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                string query = @"
            INSERT INTO NotificationHistoryLogs (RuleId, LogTimestamp, LogLevel, Message, ExceptionDetails)
            VALUES (@RuleId, @LogTimestamp, @LogLevel, @Message, @ExceptionDetails)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleId", logEntry.RuleId);
                    cmd.Parameters.AddWithValue("@LogTimestamp", logEntry.LogTimestamp);
                    cmd.Parameters.AddWithValue("@LogLevel", logEntry.LogLevel);
                    cmd.Parameters.AddWithValue("@Message", logEntry.Message);
                    cmd.Parameters.AddWithValue("@ExceptionDetails", (object)logEntry.ExceptionDetails ?? DBNull.Value);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        // --- Start of Diagnostic Code ---
                        Console.WriteLine($"[HistoryRepo] >>> Log entry saved successfully.");
                        // --- End of Diagnostic Code ---
                    }
                    catch (Exception ex)
                    {
                        // --- Start of Diagnostic Code ---
                        // Make the error impossible to miss by throwing it.
                        // Our main Program.cs will catch this and write it to the crash log file.
                        Console.WriteLine($"[HistoryRepo] FATAL DATABASE ERROR: {ex.Message}");
                        throw;
                        // --- End of Diagnostic Code ---
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all history logs for a specific notification rule.
        /// </summary>
        public List<NotificationHistoryLog> GetLogsForRule(int ruleId)
        {
            var entries = new List<NotificationHistoryLog>();
            using (var conn = _dbHelper.GetConnection())
            {
                // --- MODIFIED QUERY: Added "TOP 50" to limit results ---
                string query = "SELECT TOP 50 * FROM NotificationHistoryLogs WHERE RuleId = @RuleId ORDER BY LogTimestamp DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RuleId", ruleId);
                    try
                    {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                entries.Add(new NotificationHistoryLog
                                {
                                    HistoryLogId = Convert.ToInt32(reader["HistoryLogId"]),
                                    RuleId = Convert.ToInt32(reader["RuleId"]),
                                    LogTimestamp = Convert.ToDateTime(reader["LogTimestamp"]),
                                    LogLevel = reader["LogLevel"].ToString(),
                                    Message = reader["Message"].ToString(),
                                    ExceptionDetails = reader["ExceptionDetails"] == DBNull.Value ? null : reader["ExceptionDetails"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DB ERROR GetLogsForRule (History): {ex.Message}");
                    }
                }
            }
            return entries;
        }
    }
}
