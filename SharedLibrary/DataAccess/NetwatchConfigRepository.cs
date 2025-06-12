using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class NetwatchConfigRepository
    {
        private readonly DatabaseHelper dbHelper;
        private readonly TagRepository _tagRepository; // Assuming this is used by other methods in the class
        private readonly ServiceLogRepository _logRepository; // Added for logging
        private const string REPO_LOG_SOURCE = "NetwatchConfigRepo"; // For log messages

        // Updated constructor to accept ServiceLogRepository
        public NetwatchConfigRepository(ServiceLogRepository logRepository, TagRepository tagRepository)
        {
            dbHelper = new DatabaseHelper();
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _tagRepository = tagRepository; // Keep if other methods use it
        }

        // Overload constructor for cases where only dbHelper and logRepository are needed (like for heartbeats)
        // Or, if TagRepository is always needed, ensure it's always passed.
        // For simplicity, let's assume some parts of this repo might not need TagRepo,
        // but UpdateNetwatchLastStatus definitely doesn't.
        // A cleaner DI approach would be better long-term.
        public NetwatchConfigRepository(ServiceLogRepository logRepository)
        {
            dbHelper = new DatabaseHelper();
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _tagRepository = null; // Explicitly set to null if not provided/needed for all methods
                                   // Or, make TagRepository mandatory if all methods use it.
                                   // For now, to minimize changes, let's allow it to be null if only used by some methods.
                                   // If GetMonitoredIpDetailsForTags (or similar) is in THIS repo, _tagRepository is needed.
                                   // Based on previous files, GetMonitoredIpDetailsForTags is in TagRepository itself.
                                   // The NetwatchConfigRepository constructor in user files had `_tagRepository = new TagRepository();`
                                   // Let's keep that pattern for now, but inject the logger.
            _tagRepository = new TagRepository(); // Re-instating this as per original structure
        }


        private void Log(LogLevel level, string message, Exception ex = null, int? configId = null)
        {
            string context = configId.HasValue ? $"ConfigId {configId}: " : "";
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = REPO_LOG_SOURCE, // Or a more general service name if this library is used by multiple
                LogLevel = level.ToString(),
                Message = context + message,
                ExceptionDetails = ex?.ToString()
            });
            // Also to console for immediate debugging if service is run manually
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{REPO_LOG_SOURCE}{(configId.HasValue ? $" CfgID:{configId}" : "")}] [{level}] {message}{(ex != null ? " - Exc: " + ex.Message : "")}");
        }

        public void AddNetwatchConfig(NetwatchConfig config)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string queryConfig = @"
                INSERT INTO NetwatchConfigs 
                            (NetwatchName, Type, IntervalSeconds, TimeoutMilliseconds, 
                             SourceType, TargetId, IsEnabled, RunUponSave, CreatedDate, LastStatus)
                VALUES      (@NetwatchName, @Type, @IntervalSeconds, @TimeoutMilliseconds, 
                             @SourceType, @TargetId, @IsEnabled, @RunUponSave, @CreatedDate, @LastStatus);
                SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmdConfig = new SqlCommand(queryConfig, conn, transaction))
                    {
                        cmdConfig.Parameters.AddWithValue("@NetwatchName", config.NetwatchName);
                        cmdConfig.Parameters.AddWithValue("@Type", config.Type);
                        cmdConfig.Parameters.AddWithValue("@IntervalSeconds", config.IntervalSeconds);
                        cmdConfig.Parameters.AddWithValue("@TimeoutMilliseconds", config.TimeoutMilliseconds);
                        cmdConfig.Parameters.AddWithValue("@SourceType", config.SourceType.ToString());
                        cmdConfig.Parameters.AddWithValue("@TargetId", config.TargetId);
                        cmdConfig.Parameters.AddWithValue("@IsEnabled", config.IsEnabled);
                        cmdConfig.Parameters.AddWithValue("@RunUponSave", config.RunUponSave);
                        cmdConfig.Parameters.AddWithValue("@CreatedDate", config.CreatedDate);
                        cmdConfig.Parameters.AddWithValue("@LastStatus",
                            string.IsNullOrEmpty(config.LastStatus) ? (object)DBNull.Value : config.LastStatus);

                        object newId = cmdConfig.ExecuteScalar();
                        if (newId != null && newId != DBNull.Value)
                        {
                            config.Id = Convert.ToInt32(newId);
                        }
                        else
                        {
                            throw new Exception("Failed to retrieve new NetwatchConfig ID after insert.");
                        }
                    }

                    if (config.MonitoredTagIds != null && config.MonitoredTagIds.Any())
                    {
                        string queryTags = "INSERT INTO NetwatchConfigTags (NetwatchConfigId, TagId) VALUES (@NetwatchConfigId, @TagId)";
                        foreach (int tagIdInList in config.MonitoredTagIds)
                        {
                            using (SqlCommand cmdTags = new SqlCommand(queryTags, conn, transaction))
                            {
                                cmdTags.Parameters.AddWithValue("@NetwatchConfigId", config.Id);
                                cmdTags.Parameters.AddWithValue("@TagId", tagIdInList);
                                cmdTags.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log(LogLevel.ERROR, "Error saving Netwatch configuration with associated tags.", ex, config?.Id);
                    throw; // Re-throw to notify the calling layer
                }
            }
        }

        public List<NetwatchConfigDisplay> GetNetwatchConfigsForDisplay()
        {
            var configsList = new List<NetwatchConfigDisplay>();
            var configTagsDict = new Dictionary<int, List<string>>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string tagsQuery = @"
            SELECT 
                nct.NetwatchConfigId, 
                t.TagName
            FROM NetwatchConfigTags nct
            JOIN Tags t ON nct.TagId = t.Id
            ORDER BY nct.NetwatchConfigId, t.TagName;";

                using (SqlCommand cmdTags = new SqlCommand(tagsQuery, conn))
                {
                    using (SqlDataReader readerTags = cmdTags.ExecuteReader())
                    {
                        while (readerTags.Read())
                        {
                            int configId = Convert.ToInt32(readerTags["NetwatchConfigId"]);
                            string tagName = readerTags["TagName"].ToString();
                            if (!configTagsDict.ContainsKey(configId))
                            {
                                configTagsDict[configId] = new List<string>();
                            }
                            configTagsDict[configId].Add(tagName);
                        }
                    }
                }
                string query = @"
            SELECT 
                nc.Id, nc.NetwatchName, nc.Type, nc.IntervalSeconds, nc.TimeoutMilliseconds,
                nc.SourceType, nc.TargetId, nc.IsEnabled, nc.RunUponSave, 
                nc.CreatedDate, nc.LastChecked, nc.LastStatus,
                ncl.ClusterName 
            FROM NetwatchConfigs nc
            LEFT JOIN NetworkClusters ncl ON nc.TargetId = ncl.Id AND nc.SourceType = @NetworkClusterSourceType
            ORDER BY nc.NetwatchName;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetworkClusterSourceType", NetwatchSourceType.NetworkCluster.ToString());

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var displayModel = new NetwatchConfigDisplay
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NetwatchName = reader["NetwatchName"].ToString(),
                                Type = reader["Type"].ToString(),
                                SourceType = reader["SourceType"].ToString(),
                                IntervalSeconds = Convert.ToInt32(reader["IntervalSeconds"]),
                                TimeoutMilliseconds = Convert.ToInt32(reader["TimeoutMilliseconds"]),
                                TargetId = Convert.ToInt32(reader["TargetId"]),
                                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                                RunUponSave = Convert.ToBoolean(reader["RunUponSave"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                LastChecked = reader["LastChecked"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastChecked"]),
                                LastStatus = reader["LastStatus"] == DBNull.Value ? string.Empty : reader["LastStatus"].ToString()
                            };

                            if (displayModel.SourceType == NetwatchSourceType.NetworkCluster.ToString())
                            {
                                displayModel.TargetSourceName = reader["ClusterName"] == DBNull.Value ? "N/A" : reader["ClusterName"].ToString();
                            }
                            else
                            {
                                displayModel.TargetSourceName = "N/A"; // Placeholder for other types
                            }

                            if (configTagsDict.ContainsKey(displayModel.Id))
                            {
                                displayModel.MonitoredTagNames = configTagsDict[displayModel.Id];
                            }
                            configsList.Add(displayModel);
                        }
                    }
                }
            }
            return configsList;
        }

        public void UpdateNetwatchConfigEnabledStatus(int configId, bool isEnabled)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            UPDATE NetwatchConfigs 
            SET IsEnabled = @IsEnabled 
            WHERE Id = @ConfigId;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IsEnabled", isEnabled);
                    cmd.Parameters.AddWithValue("@ConfigId", configId);
                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            Log(LogLevel.WARN, $"UpdateNetwatchConfigEnabledStatus: No row found for ConfigId {configId}. Status set to {isEnabled}.", null, configId);
                        }
                        else
                        {
                            Log(LogLevel.INFO, $"UpdateNetwatchConfigEnabledStatus: ConfigId {configId} IsEnabled set to {isEnabled}. Rows affected: {rowsAffected}.", null, configId);
                        }
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"Database error updating NetwatchConfig IsEnabled status for ID {configId}.", ex, configId);
                        throw;
                    }
                }
            }
        }

        public bool DeleteNetwatchConfigById(int configId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // NetwatchConfigTags are deleted by CASCADE DELETE constraint in DB
                    // string queryDeleteTags = "DELETE FROM NetwatchConfigTags WHERE NetwatchConfigId = @ConfigId";
                    // using (SqlCommand cmdDeleteTags = new SqlCommand(queryDeleteTags, conn, transaction))
                    // {
                    //    cmdDeleteTags.Parameters.AddWithValue("@ConfigId", configId);
                    //    cmdDeleteTags.ExecuteNonQuery(); 
                    // }

                    string queryDeleteConfig = "DELETE FROM NetwatchConfigs WHERE Id = @ConfigId";
                    using (SqlCommand cmdDeleteConfig = new SqlCommand(queryDeleteConfig, conn, transaction))
                    {
                        cmdDeleteConfig.Parameters.AddWithValue("@ConfigId", configId);
                        int rowsAffected = cmdDeleteConfig.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            transaction.Commit();
                            Log(LogLevel.INFO, $"NetwatchConfig with ID {configId} deleted successfully.", null, configId);
                            return true;
                        }
                        else
                        {
                            transaction.Rollback();
                            Log(LogLevel.WARN, $"NetwatchConfig with ID {configId} not found for deletion.", null, configId);
                            return false;
                        }
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Log(LogLevel.ERROR, $"Database error deleting NetwatchConfig with ID {configId}.", ex, configId);
                    throw;
                }
            }
        }

        public void UpdateNetwatchLastStatus(int configId, string newStatus, DateTime lastCheckedTime)
        {
            string originalNewStatus = newStatus; // For logging original
            if (newStatus != null && newStatus.Length > 255)
            {
                newStatus = newStatus.Substring(0, 255);
                Log(LogLevel.WARN, $"Status string for ConfigId {configId} was truncated from '{originalNewStatus}' to '{newStatus}'.", null, configId);
            }

            Log(LogLevel.DEBUG, $"Attempting to update DB: ConfigId={configId}, NewStatus='{newStatus}', LastCheckedTime='{lastCheckedTime:O}'", null, configId);

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            UPDATE NetwatchConfigs 
            SET 
                LastStatus = @LastStatus, 
                LastChecked = @LastChecked
            WHERE Id = @ConfigId;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LastStatus", string.IsNullOrEmpty(newStatus) ? (object)DBNull.Value : newStatus);
                    cmd.Parameters.AddWithValue("@LastChecked", lastCheckedTime);
                    cmd.Parameters.AddWithValue("@ConfigId", configId);

                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Successfully updated LastStatus='{newStatus}', LastChecked='{lastCheckedTime:O}'. Rows affected: {rowsAffected}.", null, configId);
                        }
                        else
                        {
                            Log(LogLevel.WARN, $"UpdateNetwatchLastStatus: No row found for ConfigId {configId}. Status not updated in DB.", null, configId);
                        }
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"Database error updating Netwatch LastStatus/LastChecked. Status='{newStatus}', LastChecked='{lastCheckedTime:O}'.", ex, configId);
                        throw; // Re-throw to allow calling code to handle it
                    }
                }
            }
        }

        public NetwatchConfig GetNetwatchConfigWithDetails(int configId)
        {
            NetwatchConfig config = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            SELECT Id, NetwatchName, Type, IntervalSeconds, TimeoutMilliseconds, 
                   SourceType, TargetId, IsEnabled, RunUponSave, CreatedDate, 
                   LastChecked, LastStatus
            FROM NetwatchConfigs
            WHERE Id = @ConfigId;";

                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ConfigId", configId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            config = new NetwatchConfig
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NetwatchName = reader["NetwatchName"].ToString(),
                                Type = reader["Type"].ToString(),
                                IntervalSeconds = Convert.ToInt32(reader["IntervalSeconds"]),
                                TimeoutMilliseconds = Convert.ToInt32(reader["TimeoutMilliseconds"]),
                                SourceType = (NetwatchSourceType)Enum.Parse(typeof(NetwatchSourceType), reader["SourceType"].ToString(), true),
                                TargetId = Convert.ToInt32(reader["TargetId"]),
                                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                                RunUponSave = Convert.ToBoolean(reader["RunUponSave"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                LastChecked = reader["LastChecked"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastChecked"]),
                                LastStatus = reader["LastStatus"] == DBNull.Value ? string.Empty : reader["LastStatus"].ToString(),
                                MonitoredTagIds = new List<int>()
                            };
                        }
                    }
                }

                if (config != null)
                {
                    string tagsQuery = "SELECT TagId FROM NetwatchConfigTags WHERE NetwatchConfigId = @ConfigId";
                    using (SqlCommand cmdTags = new SqlCommand(tagsQuery, conn))
                    {
                        cmdTags.Parameters.AddWithValue("@ConfigId", config.Id);
                        using (SqlDataReader tagsReader = cmdTags.ExecuteReader())
                        {
                            while (tagsReader.Read())
                            {
                                config.MonitoredTagIds.Add(Convert.ToInt32(tagsReader["TagId"]));
                            }
                        }
                    }
                }
            }
            return config;
        }

        public List<NetwatchConfig> GetAllEnabledNetwatchConfigsWithDetails()
        {
            var configs = new List<NetwatchConfig>();
            var configIdToTagsMap = new Dictionary<int, List<int>>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string allTagsQuery = "SELECT NetwatchConfigId, TagId FROM NetwatchConfigTags";
                using (SqlCommand cmdAllTags = new SqlCommand(allTagsQuery, conn))
                {
                    using (SqlDataReader tagsReader = cmdAllTags.ExecuteReader())
                    {
                        while (tagsReader.Read())
                        {
                            int cfgId = Convert.ToInt32(tagsReader["NetwatchConfigId"]);
                            int tagId = Convert.ToInt32(tagsReader["TagId"]);
                            if (!configIdToTagsMap.ContainsKey(cfgId))
                            {
                                configIdToTagsMap[cfgId] = new List<int>();
                            }
                            configIdToTagsMap[cfgId].Add(tagId);
                        }
                    }
                }

                string query = @"
            SELECT Id, NetwatchName, Type, IntervalSeconds, TimeoutMilliseconds, 
                   SourceType, TargetId, IsEnabled, RunUponSave, CreatedDate, 
                   LastChecked, LastStatus
            FROM NetwatchConfigs
            WHERE IsEnabled = 1;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var config = new NetwatchConfig
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NetwatchName = reader["NetwatchName"].ToString(),
                                Type = reader["Type"].ToString(),
                                IntervalSeconds = Convert.ToInt32(reader["IntervalSeconds"]),
                                TimeoutMilliseconds = Convert.ToInt32(reader["TimeoutMilliseconds"]),
                                SourceType = (NetwatchSourceType)Enum.Parse(typeof(NetwatchSourceType), reader["SourceType"].ToString(), true),
                                TargetId = Convert.ToInt32(reader["TargetId"]),
                                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                                RunUponSave = Convert.ToBoolean(reader["RunUponSave"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                LastChecked = reader["LastChecked"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastChecked"]),
                                LastStatus = reader["LastStatus"] == DBNull.Value ? string.Empty : reader["LastStatus"].ToString(),
                                MonitoredTagIds = configIdToTagsMap.ContainsKey(Convert.ToInt32(reader["Id"]))
                                                  ? configIdToTagsMap[Convert.ToInt32(reader["Id"])]
                                                  : new List<int>()
                            };
                            configs.Add(config);
                        }
                    }
                }
            }
            return configs;
        }

        public void UpdateServiceHeartbeat(string serviceName, DateTime heartbeatTime)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            MERGE ServiceHeartbeats AS target
            USING (SELECT @ServiceName AS ServiceName, @HeartbeatTime AS LastHeartbeatDateTime) AS source
            ON (target.ServiceName = source.ServiceName)
            WHEN MATCHED THEN
                UPDATE SET LastHeartbeatDateTime = source.LastHeartbeatDateTime
            WHEN NOT MATCHED THEN
                INSERT (ServiceName, LastHeartbeatDateTime)
                VALUES (source.ServiceName, source.LastHeartbeatDateTime);";

                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                    cmd.Parameters.AddWithValue("@HeartbeatTime", heartbeatTime);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public DateTime? GetLastServiceHeartbeat(string serviceName)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = "SELECT LastHeartbeatDateTime FROM ServiceHeartbeats WHERE ServiceName = @ServiceName";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToDateTime(result);
                    }
                }
            }
            return null;
        }
        public void SaveIndividualIpPingResult(int netwatchConfigId, MonitoredIpDetail ipDetail, PingReply reply, DateTime pingAttemptTime)
        {
            string pingStatusText;
            long? roundtripTime = null;

            if (reply == null)
            {
                pingStatusText = "Error (No Reply)";
            }
            else
            {
                pingStatusText = reply.Status.ToString();
                if (reply.Status == IPStatus.Success)
                {
                    roundtripTime = reply.RoundtripTime;
                }
            }

            if (pingStatusText.Length > 50) pingStatusText = pingStatusText.Substring(0, 50);

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            MERGE NetwatchIpResults AS Target
            USING (SELECT @NetwatchConfigId AS NCID, @IpAddress AS IP) AS Source
            ON Target.NetwatchConfigId = Source.NCID AND Target.IpAddress = Source.IP
            WHEN MATCHED THEN
                UPDATE SET 
                    EntityName = @EntityName,
                    LastPingStatus = @LastPingStatus,
                    RoundtripTimeMs = @RoundtripTimeMs,
                    LastPingAttemptDateTime = @LastPingAttemptDateTime
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (NetwatchConfigId, IpAddress, EntityName, LastPingStatus, RoundtripTimeMs, LastPingAttemptDateTime)
                VALUES (@NetwatchConfigId, @IpAddress, @EntityName, @LastPingStatus, @RoundtripTimeMs, @LastPingAttemptDateTime);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    cmd.Parameters.AddWithValue("@IpAddress", ipDetail.IpAddress);
                    cmd.Parameters.AddWithValue("@EntityName", string.IsNullOrEmpty(ipDetail.EntityName) ? (object)DBNull.Value : ipDetail.EntityName);
                    cmd.Parameters.AddWithValue("@LastPingStatus", pingStatusText);
                    cmd.Parameters.AddWithValue("@RoundtripTimeMs", roundtripTime.HasValue ? (object)roundtripTime.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastPingAttemptDateTime", pingAttemptTime);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL Error saving individual IP ping result for ConfigId {netwatchConfigId}, IP {ipDetail.IpAddress}.", ex, netwatchConfigId);
                        throw;
                    }
                }
            }
        }
        public List<IndividualIpStatus> GetDetailedIpStatuses(int netwatchConfigId)
        {
            var resultStatusList = new List<IndividualIpStatus>();
            NetwatchConfig config = GetNetwatchConfigWithDetails(netwatchConfigId);
            if (config == null || config.MonitoredTagIds == null || !config.MonitoredTagIds.Any())
            {
                return resultStatusList;
            }

            TagRepository tagRepo = _tagRepository ?? new TagRepository(); // Use injected or new up
            List<MonitoredIpDetail> allExpectedEntities = tagRepo.GetMonitoredIpDetailsForTags(config.MonitoredTagIds);
            var currentPingResultsMap = new Dictionary<string, IndividualIpStatus>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string queryIpResults = @"
            SELECT EntityName, IpAddress, LastPingStatus, RoundtripTimeMs, LastPingAttemptDateTime
            FROM NetwatchIpResults
            WHERE NetwatchConfigId = @NetwatchConfigId;";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(queryIpResults, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ipAddress = reader["IpAddress"].ToString();
                            if (!string.IsNullOrWhiteSpace(ipAddress) && !currentPingResultsMap.ContainsKey(ipAddress))
                            {
                                currentPingResultsMap[ipAddress] = new IndividualIpStatus
                                {
                                    EntityName = reader["EntityName"] == DBNull.Value ? "N/A" : reader["EntityName"].ToString(),
                                    IpAddress = ipAddress,
                                    LastPingStatus = reader["LastPingStatus"].ToString(),
                                    RoundtripTimeMs = reader["RoundtripTimeMs"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["RoundtripTimeMs"]),
                                    LastPingAttemptDateTime = Convert.ToDateTime(reader["LastPingAttemptDateTime"])
                                };
                            }
                        }
                    }
                }
            }

            foreach (var expectedEntity in allExpectedEntities)
            {
                if (string.IsNullOrWhiteSpace(expectedEntity.IpAddress))
                {
                    resultStatusList.Add(new IndividualIpStatus
                    {
                        EntityName = expectedEntity.EntityName,
                        IpAddress = "N/A",
                        LastPingStatus = "No IP",
                        RoundtripTimeMs = null,
                        LastPingAttemptDateTime = DateTime.MinValue
                    });
                }
                else
                {
                    if (currentPingResultsMap.TryGetValue(expectedEntity.IpAddress, out IndividualIpStatus pingStatus))
                    {
                        pingStatus.EntityName = expectedEntity.EntityName;
                        resultStatusList.Add(pingStatus);
                    }
                    else
                    {
                        resultStatusList.Add(new IndividualIpStatus
                        {
                            EntityName = expectedEntity.EntityName,
                            IpAddress = expectedEntity.IpAddress,
                            LastPingStatus = "Pending",
                            RoundtripTimeMs = null,
                            LastPingAttemptDateTime = DateTime.MinValue
                        });
                    }
                }
            }
            return resultStatusList.OrderBy(s => s.EntityName).ThenBy(s => s.IpAddress).ToList();
        }
        public string GetLastKnownPingStatusForIp(int netwatchConfigId, string ipAddress)
        {
            string lastStatus = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            SELECT LastPingStatus 
            FROM NetwatchIpResults 
            WHERE NetwatchConfigId = @NetwatchConfigId AND IpAddress = @IpAddress;";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        lastStatus = result.ToString();
                    }
                }
            }
            return lastStatus;
        }
        public void StartOutageLog(int netwatchConfigId, string ipAddress, string entityName, DateTime outageStartTime, string initialPingStatus)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            INSERT INTO NetwatchOutageLog 
                (NetwatchConfigId, IpAddress, EntityName, OutageStartTime, LastPingStatusAtStart, OutageEndTime)
            VALUES 
                (@NetwatchConfigId, @IpAddress, @EntityName, @OutageStartTime, @LastPingStatusAtStart, NULL);";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@EntityName", string.IsNullOrEmpty(entityName) ? (object)DBNull.Value : entityName);
                    cmd.Parameters.AddWithValue("@OutageStartTime", outageStartTime);
                    cmd.Parameters.AddWithValue("@LastPingStatusAtStart", string.IsNullOrEmpty(initialPingStatus) ? (object)DBNull.Value : initialPingStatus);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        Log(LogLevel.INFO, $"Started outage log for IP: {ipAddress} at {outageStartTime}", null, netwatchConfigId);
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL ERROR Starting OutageLog for IP: {ipAddress}.", ex, netwatchConfigId);
                    }
                }
            }
        }
        public void EndOutageLog(int netwatchConfigId, string ipAddress, DateTime outageEndTime)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
                UPDATE NetwatchOutageLog
                SET OutageEndTime = @OutageEndTime
                WHERE OutageLogId = (
                    SELECT TOP 1 OutageLogId 
                    FROM NetwatchOutageLog
                    WHERE NetwatchConfigId = @NetwatchConfigId 
                      AND IpAddress = @IpAddress 
                      AND OutageEndTime IS NULL
                    ORDER BY OutageStartTime DESC
                );";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@OutageEndTime", outageEndTime);

                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Log(LogLevel.INFO, $"Ended outage log for IP: {ipAddress} at {outageEndTime}", null, netwatchConfigId);
                        }
                        else
                        {
                            Log(LogLevel.WARN, $"No open outage found to end for IP: {ipAddress}", null, netwatchConfigId);
                        }
                    }
                    catch (SqlException ex)
                    {
                        Log(LogLevel.ERROR, $"SQL ERROR Ending OutageLog for IP: {ipAddress}.", ex, netwatchConfigId);
                        throw;
                    }
                }
            }
        }
        public DateTime? GetCurrentOutageStartTime(int netwatchConfigId, string ipAddress)
        {
            DateTime? outageStartTime = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            SELECT TOP 1 OutageStartTime 
            FROM NetwatchOutageLog
            WHERE NetwatchConfigId = @NetwatchConfigId 
              AND IpAddress = @IpAddress 
              AND OutageEndTime IS NULL
            ORDER BY OutageStartTime DESC;";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        outageStartTime = Convert.ToDateTime(result);
                    }
                }
            }
            return outageStartTime;
        }
        public void PruneNetwatchDataForMissingEntities(int netwatchConfigId, List<MonitoredIpDetail> currentValidEntityDetails)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    var existingDbEntries = new HashSet<(string EntityName, string IpAddress)>();
                    string selectQuery = @"
                SELECT DISTINCT EntityName, IpAddress 
                FROM NetwatchIpResults 
                WHERE NetwatchConfigId = @NetwatchConfigId AND EntityName IS NOT NULL AND IpAddress IS NOT NULL";
                    using (SqlCommand cmdSelect = new SqlCommand(selectQuery, conn, transaction))
                    {
                        cmdSelect.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                        using (SqlDataReader reader = cmdSelect.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existingDbEntries.Add((reader.GetString(0), reader.GetString(1)));
                            }
                        }
                    }

                    var currentValidSet = new HashSet<(string EntityName, string IpAddress)>(
                        currentValidEntityDetails.Select(d => (d.EntityName, d.IpAddress))
                    );

                    var entriesToPrune = existingDbEntries.Except(currentValidSet).ToList();

                    if (entriesToPrune.Any())
                    {
                        Log(LogLevel.INFO, $"Pruning data for {entriesToPrune.Count} stale entity-IP pairs.", null, netwatchConfigId);
                        foreach (var staleEntry in entriesToPrune)
                        {
                            string staleEntityName = staleEntry.EntityName;
                            string staleIpAddress = staleEntry.IpAddress;
                            DateTime pruneTime = DateTime.Now;

                            string updateOutageSql = @"
                        UPDATE NetwatchOutageLog 
                        SET OutageEndTime = @PruneTime 
                        WHERE NetwatchConfigId = @NetwatchConfigId 
                          AND EntityName = @EntityName 
                          AND IpAddress = @IpAddress 
                          AND OutageEndTime IS NULL;";
                            using (SqlCommand cmdUpdateOutage = new SqlCommand(updateOutageSql, conn, transaction))
                            {
                                cmdUpdateOutage.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                                cmdUpdateOutage.Parameters.AddWithValue("@EntityName", staleEntityName);
                                cmdUpdateOutage.Parameters.AddWithValue("@IpAddress", staleIpAddress);
                                cmdUpdateOutage.Parameters.AddWithValue("@PruneTime", pruneTime);
                                cmdUpdateOutage.ExecuteNonQuery();
                                Log(LogLevel.DEBUG, $"Closed open outages for stale: {staleEntityName} - {staleIpAddress}", null, netwatchConfigId);
                            }

                            string deleteIpResultSql = @"
                        DELETE FROM NetwatchIpResults 
                        WHERE NetwatchConfigId = @NetwatchConfigId 
                          AND EntityName = @EntityName 
                          AND IpAddress = @IpAddress;";
                            using (SqlCommand cmdDeleteResult = new SqlCommand(deleteIpResultSql, conn, transaction))
                            {
                                cmdDeleteResult.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                                cmdDeleteResult.Parameters.AddWithValue("@EntityName", staleEntityName);
                                cmdDeleteResult.Parameters.AddWithValue("@IpAddress", staleIpAddress);
                                cmdDeleteResult.ExecuteNonQuery();
                                Log(LogLevel.DEBUG, $"Deleted IP results for stale: {staleEntityName} - {staleIpAddress}", null, netwatchConfigId);
                            }
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log(LogLevel.ERROR, $"ERROR pruning stale Netwatch data.", ex, netwatchConfigId);
                    throw;
                }
            }
        }
        public TimeSpan GetTotalHistoricalOutageDurationForEntity(int netwatchConfigId, string entityName, string excludeCurrentIpAddress)
        {
            TimeSpan totalHistoricalDuration = TimeSpan.Zero;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT OutageStartTime, OutageEndTime 
            FROM NetwatchOutageLog
            WHERE NetwatchConfigId = @NetwatchConfigId 
              AND EntityName = @EntityName
              AND IpAddress != @ExcludeCurrentIpAddress 
              AND OutageEndTime IS NOT NULL;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    cmd.Parameters.AddWithValue("@EntityName", string.IsNullOrEmpty(entityName) ? (object)DBNull.Value : entityName);
                    cmd.Parameters.AddWithValue("@ExcludeCurrentIpAddress", string.IsNullOrEmpty(excludeCurrentIpAddress) ? (object)DBNull.Value : excludeCurrentIpAddress);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime startTime = reader.GetDateTime(0);
                            DateTime endTime = reader.GetDateTime(1);
                            totalHistoricalDuration += (endTime - startTime);
                        }
                    }
                }
            }
            return totalHistoricalDuration;
        }
        public void UpdateLastDataSyncTimestamp(string serviceName, DateTime syncTimestamp)
        {
            try
            {
                using (SqlConnection conn = dbHelper.GetConnection()) // Assumes dbHelper is available like in other methods
                {
                    conn.Open();
                    // Check if the service entry exists, if not, create it (though UpdateServiceHeartbeat likely handles creation)
                    // For simplicity, this assumes UpdateServiceHeartbeat is called regularly and creates the entry.
                    // If not, you might need an INSERT INTO ... ON DUPLICATE KEY UPDATE or MERGE equivalent,
                    // or ensure the record exists before this call.
                    // Given the context, we'll assume an UPDATE is sufficient as the record should exist.

                    string query = @"
                UPDATE ServiceHeartbeats 
                SET LastDataSyncTimestamp = @SyncTimestamp
                WHERE ServiceName = @ServiceName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SyncTimestamp", syncTimestamp);
                        cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            // Optionally, if the record might not exist, you could try an INSERT here
                            // or log a warning. For now, we assume UpdateServiceHeartbeat ensures the record.
                            Console.WriteLine($"UpdateLastDataSyncTimestamp: No existing record found for ServiceName '{serviceName}' to update LastDataSyncTimestamp.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log this exception appropriately in a real application
                Console.WriteLine($"Error in UpdateLastDataSyncTimestamp for {serviceName}: {ex.Message}");
                // Consider re-throwing or logging to a more persistent store if this is critical
            }
        }
        public DateTime? GetLastDataSyncTimestamp(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                Log(LogLevel.WARN, "GetLastDataSyncTimestamp: ServiceName cannot be null or whitespace.");
                return null;
            }

            DateTime? lastSyncTimestamp = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = "SELECT LastDataSyncTimestamp FROM ServiceHeartbeats WHERE ServiceName = @ServiceName";
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            lastSyncTimestamp = Convert.ToDateTime(result);
                            Log(LogLevel.DEBUG, $"GetLastDataSyncTimestamp: Retrieved {lastSyncTimestamp} for service '{serviceName}'.");
                        }
                        else
                        {
                            Log(LogLevel.INFO, $"GetLastDataSyncTimestamp: No LastDataSyncTimestamp found for service '{serviceName}'.");
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Log(LogLevel.ERROR, $"SQL Error fetching LastDataSyncTimestamp for service '{serviceName}'.", ex);
                    // Depending on handling strategy, you might want to return null or rethrow
                    return null;
                }
                catch (Exception ex)
                {
                    Log(LogLevel.ERROR, $"General Error fetching LastDataSyncTimestamp for service '{serviceName}'.", ex);
                    return null;
                }
            }
            return lastSyncTimestamp;
        }

        public NetwatchConfigDisplay GetNetwatchConfigStatus(int configId)
        {
            // We can reuse the existing method and just find the specific one we need.
            // This is efficient enough for now.
            var allConfigs = GetNetwatchConfigsForDisplay();
            return allConfigs.FirstOrDefault(c => c.Id == configId);
        }
    }
}
