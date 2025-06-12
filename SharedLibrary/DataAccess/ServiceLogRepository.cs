using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class ServiceLogRepository
    {
        private readonly DatabaseHelper _dbHelper;

        public ServiceLogRepository()
        {
            _dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Writes a log entry to the ApplicationLogs table.
        /// </summary>
        public void WriteLog(ServiceLogEntry logEntry)
        {
            // Basic validation
            if (logEntry == null || string.IsNullOrWhiteSpace(logEntry.ServiceName) || string.IsNullOrWhiteSpace(logEntry.Message))
            {
                // Optionally log this failure to a local file or debug output if DB logging itself fails
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] WriteLog Error: Log entry, ServiceName, or Message is null/empty.");
                return;
            }

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                string query = @"
                    INSERT INTO ApplicationLogs (LogTimestamp, ServiceName, LogLevel, Message, ExceptionDetails)
                    VALUES (@LogTimestamp, @ServiceName, @LogLevel, @Message, @ExceptionDetails)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LogTimestamp", logEntry.LogTimestamp);
                    cmd.Parameters.AddWithValue("@ServiceName", logEntry.ServiceName);
                    cmd.Parameters.AddWithValue("@LogLevel", logEntry.LogLevel?.ToString() ?? LogLevel.INFO.ToString()); // Default to INFO if null
                    cmd.Parameters.AddWithValue("@Message", logEntry.Message);
                    cmd.Parameters.AddWithValue("@ExceptionDetails",
                        string.IsNullOrWhiteSpace(logEntry.ExceptionDetails) ? (object)DBNull.Value : logEntry.ExceptionDetails);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        // If logging to DB fails, write to console/debug as a fallback for services
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FAILSAFE LOG (DB Log Error: {ex.Message}): Service: {logEntry.ServiceName}, Level: {logEntry.LogLevel}, Msg: {logEntry.Message}");
                        if (!string.IsNullOrWhiteSpace(logEntry.ExceptionDetails))
                        {
                            Console.WriteLine($"    Exception: {logEntry.ExceptionDetails}");
                        }
                        // Depending on severity, you might re-throw or handle
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves log entries, optionally filtered by service name and within a date range.
        /// Defaults to retrieving logs for the last 7 days if no dates are specified.
        /// </summary>
        public List<ServiceLogEntry> GetLogs(string serviceName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            List<ServiceLogEntry> logs = new List<ServiceLogEntry>();

            // Default to last 7 days if no start date is provided
            DateTime effectiveStartDate = startDate ?? DateTime.Now.AddDays(-7).Date; // Start of day, 7 days ago
            DateTime effectiveEndDate = endDate ?? DateTime.Now.AddDays(1).Date.AddTicks(-1); // End of current day

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                // Build the query dynamically based on filters
                string query = "SELECT LogId, LogTimestamp, ServiceName, LogLevel, Message, ExceptionDetails FROM ApplicationLogs WHERE LogTimestamp >= @StartDate AND LogTimestamp <= @EndDate";

                if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    query += " AND ServiceName = @ServiceName";
                }
                query += " ORDER BY LogTimestamp DESC"; // Show newest logs first

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", effectiveStartDate);
                    cmd.Parameters.AddWithValue("@EndDate", effectiveEndDate);

                    if (!string.IsNullOrWhiteSpace(serviceName))
                    {
                        cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                    }

                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                logs.Add(new ServiceLogEntry
                                {
                                    LogId = Convert.ToInt32(reader["LogId"]),
                                    LogTimestamp = Convert.ToDateTime(reader["LogTimestamp"]),
                                    ServiceName = reader["ServiceName"].ToString(),
                                    LogLevel = reader["LogLevel"].ToString(),
                                    Message = reader["Message"].ToString(),
                                    ExceptionDetails = reader["ExceptionDetails"] == DBNull.Value ? null : reader["ExceptionDetails"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DB ERROR GetLogs: {ex.Message}");
                        // Handle or re-throw
                    }
                }
            }
            return logs;
        }

        /// <summary>
        /// Deletes log entries older than a specified number of days.
        /// </summary>
        public int DeleteOldLogs(int daysToKeep = 7)
        {
            int rowsAffected = 0;
            DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep).Date; // Delete logs older than the start of X days ago

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                string query = "DELETE FROM ApplicationLogs WHERE LogTimestamp < @CutoffDate";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CutoffDate", cutoffDate);
                    try
                    {
                        conn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Deleted {rowsAffected} old log entries older than {cutoffDate:yyyy-MM-dd}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DB ERROR DeleteOldLogs: {ex.Message}");
                        // Handle or re-throw
                    }
                }
            }
            return rowsAffected;
        }
        public List<ServiceLogEntry> GetLogsForRule(int ruleId)
        {
            var entries = new List<ServiceLogEntry>();
            // The filter looks for the specific pattern "[ID=<ruleId>]" in the message.
            string ruleIdFilter = $"[ID={ruleId}]";

            using (var conn = _dbHelper.GetConnection())
            {
                // Corrected to use 'ApplicationLogs' table
                string query = "SELECT * FROM ApplicationLogs WHERE Message LIKE '%' + @Filter + '%' ORDER BY LogTimestamp DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Filter", ruleIdFilter);
                    try
                    {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                entries.Add(new ServiceLogEntry
                                {
                                    // Corrected to use LogId and LogTimestamp
                                    LogId = Convert.ToInt32(reader["LogId"]),
                                    LogTimestamp = Convert.ToDateTime(reader["LogTimestamp"]),
                                    ServiceName = reader["ServiceName"].ToString(),
                                    LogLevel = reader["LogLevel"].ToString(),
                                    Message = reader["Message"].ToString(),
                                    ExceptionDetails = reader["ExceptionDetails"] == DBNull.Value ? null : reader["ExceptionDetails"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DB ERROR GetLogsForRule: {ex.Message}");
                    }
                }
            }
            return entries;
        }
    }
}
