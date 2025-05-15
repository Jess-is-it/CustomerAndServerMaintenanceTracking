using CustomerAndServerMaintenanceTracking.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class NetwatchConfigRepository
    {
        private DatabaseHelper dbHelper;

        public NetwatchConfigRepository()
        {
            dbHelper = new DatabaseHelper();
        }


        // In NetwatchConfigRepository.cs
        public void AddNetwatchConfig(NetwatchConfig config)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction(); // Start a database transaction

                try
                {
                    // Step 1: Insert into NetwatchConfigs table
                    // TargetTagId has been removed from this INSERT as per your database change.
                    string queryConfig = @"
                INSERT INTO NetwatchConfigs 
                            (NetwatchName, Type, IntervalSeconds, TimeoutMilliseconds, 
                             SourceType, TargetId,  -- TargetId is the NetworkClusterId here
                             IsEnabled, RunUponSave, CreatedDate, LastStatus)
                VALUES      (@NetwatchName, @Type, @IntervalSeconds, @TimeoutMilliseconds, 
                             @SourceType, @TargetId, 
                             @IsEnabled, @RunUponSave, @CreatedDate, @LastStatus);
                SELECT SCOPE_IDENTITY();"; // Get the ID of the newly inserted row

                    using (SqlCommand cmdConfig = new SqlCommand(queryConfig, conn, transaction))
                    {
                        cmdConfig.Parameters.AddWithValue("@NetwatchName", config.NetwatchName);
                        cmdConfig.Parameters.AddWithValue("@Type", config.Type);
                        cmdConfig.Parameters.AddWithValue("@IntervalSeconds", config.IntervalSeconds);
                        cmdConfig.Parameters.AddWithValue("@TimeoutMilliseconds", config.TimeoutMilliseconds);
                        cmdConfig.Parameters.AddWithValue("@SourceType", config.SourceType.ToString());

                        // TargetId for NetwatchConfigs is the ID of the source entity (e.g., NetworkCluster.Id)
                        cmdConfig.Parameters.AddWithValue("@TargetId", config.TargetId);

                        cmdConfig.Parameters.AddWithValue("@IsEnabled", config.IsEnabled);
                        cmdConfig.Parameters.AddWithValue("@RunUponSave", config.RunUponSave);
                        cmdConfig.Parameters.AddWithValue("@CreatedDate", config.CreatedDate);
                        cmdConfig.Parameters.AddWithValue("@LastStatus",
                            string.IsNullOrEmpty(config.LastStatus) ? (object)DBNull.Value : config.LastStatus);

                        object newId = cmdConfig.ExecuteScalar();
                        if (newId != null && newId != DBNull.Value)
                        {
                            config.Id = Convert.ToInt32(newId); // Set the ID on the config object for use in the next step
                        }
                        else
                        {
                            // If SCOPE_IDENTITY() returns null or DBNull, something went wrong with the insert.
                            throw new Exception("Failed to retrieve new NetwatchConfig ID after insert. The insert may have failed.");
                        }
                    }

                    // Step 2: Insert into NetwatchConfigTags table for each monitored tag ID
                    if (config.MonitoredTagIds != null && config.MonitoredTagIds.Any()) // Check if there are any tags to monitor
                    {
                        string queryTags = "INSERT INTO NetwatchConfigTags (NetwatchConfigId, TagId) VALUES (@NetwatchConfigId, @TagId)";
                        foreach (int tagIdInList in config.MonitoredTagIds)
                        {
                            using (SqlCommand cmdTags = new SqlCommand(queryTags, conn, transaction))
                            {
                                cmdTags.Parameters.AddWithValue("@NetwatchConfigId", config.Id); // Use the new ID from Step 1
                                cmdTags.Parameters.AddWithValue("@TagId", tagIdInList);
                                cmdTags.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit(); // If all database operations were successful, commit the transaction
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // If any error occurred during the process, roll back all changes
                                            // Optionally log the exception (ex.ToString()) or provide more specific error handling
                    throw new Exception("Error saving Netwatch configuration with associated tags: " + ex.Message, ex); // Re-throw to notify the UI layer
                }
                // The 'using (SqlConnection conn ...)' block will ensure the connection is closed.
            }
        }

        public List<NetwatchConfigDisplay> GetNetwatchConfigsForDisplay()

        {
            var configsList = new List<NetwatchConfigDisplay>();
            // Temporary dictionary to hold tag names for each config ID
            var configTagsDict = new Dictionary<int, List<string>>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // First, get all tag mappings and their names
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
                } // readerTags and cmdTags disposed here

                // Now, get the main NetwatchConfigs and join with NetworkClusters if applicable
                string query = @"
            SELECT 
                nc.Id, nc.NetwatchName, nc.Type, nc.IntervalSeconds, nc.TimeoutMilliseconds,
                nc.SourceType, nc.TargetId, nc.IsEnabled, nc.RunUponSave, 
                nc.CreatedDate, nc.LastChecked, nc.LastStatus,
                ncl.ClusterName 
            FROM NetwatchConfigs nc
            LEFT JOIN NetworkClusters ncl ON nc.TargetId = ncl.Id AND nc.SourceType = @NetworkClusterSourceType
            ORDER BY nc.NetwatchName;";

                // Pass NetwatchSourceType.NetworkCluster.ToString() as a parameter
                // This ensures the LEFT JOIN only attempts to match ClusterName for the correct SourceType.

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

                            // Set TargetSourceName based on SourceType
                            if (displayModel.SourceType == NetwatchSourceType.NetworkCluster.ToString())
                            {
                                displayModel.TargetSourceName = reader["ClusterName"] == DBNull.Value ? "N/A" : reader["ClusterName"].ToString();
                            }
                            // else if (displayModel.SourceType == NetwatchSourceType.Customer.ToString()) { /* Fetch Customer Name */ }
                            // else if (displayModel.SourceType == NetwatchSourceType.DeviceIP.ToString()) { /* Fetch DeviceIP Name */ }
                            else
                            {
                                displayModel.TargetSourceName = "N/A";
                            }

                            // Assign collected tag names
                            if (configTagsDict.ContainsKey(displayModel.Id))
                            {
                                displayModel.MonitoredTagNames = configTagsDict[displayModel.Id];
                            }

                            configsList.Add(displayModel);
                        }
                    }
                } // reader and cmd disposed here
            } // conn disposed here
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
                            // This could mean the configId was not found, though not necessarily an exception
                            // You might want to log this or handle it if it's unexpected.
                            Console.WriteLine($"Warning: No NetwatchConfig found with ID {configId} to update IsEnabled status.");
                        }
                    }
                    catch (SqlException ex)
                    {
                        // Log the exception (ex.ToString()) or handle it more gracefully
                        // For now, re-throw to make the calling code aware of the failure
                        throw new Exception($"Database error updating NetwatchConfig IsEnabled status for ID {configId}: {ex.Message}", ex);
                    }
                    // The 'using (SqlConnection conn ...)' block will ensure the connection is closed.
                }
            }
        }

        // Add this method to your NetwatchConfigRepository.cs file

        public bool DeleteNetwatchConfigById(int configId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction(); // Start a transaction

                try
                {
                    // Step 1: Delete from NetwatchConfigTags (associated tags)
                    // ON DELETE CASCADE is set for NetwatchConfigId in NetwatchConfigTags table based on your SQL script,
                    // so this step might be redundant if the cascade delete is working as expected at the database level.
                    // However, explicit deletion can be safer or used if cascade isn't set for TagId.
                    // If ON DELETE CASCADE is reliable for NetwatchConfigId, you might only need to delete from NetwatchConfigs.
                    // For now, let's assume explicit deletion of tags first for clarity.

                    string queryDeleteTags = "DELETE FROM NetwatchConfigTags WHERE NetwatchConfigId = @ConfigId";
                    using (SqlCommand cmdDeleteTags = new SqlCommand(queryDeleteTags, conn, transaction))
                    {
                        cmdDeleteTags.Parameters.AddWithValue("@ConfigId", configId);
                        cmdDeleteTags.ExecuteNonQuery(); // This will remove all tag associations for this config
                    }

                    // Step 2: Delete from NetwatchConfigs
                    string queryDeleteConfig = "DELETE FROM NetwatchConfigs WHERE Id = @ConfigId";
                    using (SqlCommand cmdDeleteConfig = new SqlCommand(queryDeleteConfig, conn, transaction))
                    {
                        cmdDeleteConfig.Parameters.AddWithValue("@ConfigId", configId);
                        int rowsAffected = cmdDeleteConfig.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            transaction.Commit(); // Commit if both deletions were successful (or config deletion if tags are cascaded)
                            return true;
                        }
                        else
                        {
                            transaction.Rollback(); // Rollback if the main config wasn't found or deleted
                            return false; // Config not found or not deleted
                        }
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback(); // Rollback on any SQL error
                                            // Log the exception (ex.ToString()) or handle it more gracefully
                    throw new Exception($"Database error deleting NetwatchConfig with ID {configId}: {ex.Message}", ex);
                }
                // The 'using (SqlConnection conn ...)' block will ensure the connection is closed.
            }
        }

        #region ICMP Ping Service
        public void UpdateNetwatchLastStatus(int configId, string newStatus, DateTime lastCheckedTime)
        {
            using (SqlConnection conn = dbHelper.GetConnection()) // Assuming dbHelper is your DatabaseHelper instance
            {
                // Ensure newStatus is not excessively long for the database column (nvarchar(255))
                if (newStatus != null && newStatus.Length > 255)
                {
                    newStatus = newStatus.Substring(0, 255);
                }

                string query = @"
            UPDATE NetwatchConfigs 
            SET 
                LastStatus = @LastStatus, 
                LastChecked = @LastChecked
            WHERE Id = @ConfigId;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Handle potential null or empty status string
                    cmd.Parameters.AddWithValue("@LastStatus", string.IsNullOrEmpty(newStatus) ? (object)DBNull.Value : newStatus);
                    cmd.Parameters.AddWithValue("@LastChecked", lastCheckedTime);
                    cmd.Parameters.AddWithValue("@ConfigId", configId);

                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            // This could mean the configId was not found.
                            // You might want to log this as a warning.
                            Console.WriteLine($"Warning: No NetwatchConfig found with ID {configId} to update LastStatus/LastChecked.");
                        }
                    }
                    catch (SqlException ex)
                    {
                        // Log the exception or handle it more gracefully
                        // For now, re-throw to make the calling code aware of the failure
                        throw new Exception($"Database error updating Netwatch LastStatus/LastChecked for ID {configId}: {ex.Message}", ex);
                    }
                    // Connection will be closed by the 'using' block.
                }
            }
        }

        // Helper method to fetch a single NetwatchConfig with its associated MonitoredTagIds
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
                                MonitoredTagIds = new List<int>() // Initialize
                            };
                        }
                    } // Reader closes here
                } // Command disposed here

                if (config != null)
                {
                    // Now fetch the associated TagIds
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
                        } // tagsReader closes here
                    } // cmdTags disposed here
                }
            } // Connection closes here
            return config;
        }

        // Method to get all ENABLED NetwatchConfigs with their MonitoredTagIds
        // This will be used by the NetwatchServiceManager on startup.
        public List<NetwatchConfig> GetAllEnabledNetwatchConfigsWithDetails()
        {
            var configs = new List<NetwatchConfig>();
            var configIdToTagsMap = new Dictionary<int, List<int>>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // Step 1: Get all MonitoredTagIds for all NetwatchConfigs
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


                // Step 2: Get all enabled NetwatchConfig main details
                string query = @"
            SELECT Id, NetwatchName, Type, IntervalSeconds, TimeoutMilliseconds, 
                   SourceType, TargetId, IsEnabled, RunUponSave, CreatedDate, 
                   LastChecked, LastStatus
            FROM NetwatchConfigs
            WHERE IsEnabled = 1;"; // Only fetch enabled ones

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
                                                  : new List<int>() // Empty list if no tags
                            };
                            configs.Add(config);
                        }
                    }
                }
            }
            return configs;
        }

        #endregion

        #region Netwatch HeartBeats
        public void UpdateServiceHeartbeat(string serviceName, DateTime heartbeatTime)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                // UPSERT logic: Update if exists, Insert if not.
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
        #endregion

        #region METHOD for Detailed IP Status Popup/Form
        public void SaveIndividualIpPingResult(int netwatchConfigId, MonitoredIpDetail ipDetail, PingReply reply, DateTime pingAttemptTime)
        {
            string pingStatusText;
            long? roundtripTime = null;

            if (reply == null)
            {
                pingStatusText = "Error (No Reply)"; // Or "Failed (Exception)"
            }
            else
            {
                pingStatusText = reply.Status.ToString(); // e.g., "Success", "TimedOut", "DestinationHostUnreachable"
                if (reply.Status == IPStatus.Success)
                {
                    roundtripTime = reply.RoundtripTime;
                }
            }

            // Truncate status text if too long for DB column
            if (pingStatusText.Length > 50) pingStatusText = pingStatusText.Substring(0, 50);
            if (ipDetail.EntityName != null && ipDetail.EntityName.Length > 100) ipDetail.EntityName = ipDetail.EntityName.Substring(0, 100);


            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // UPSERT logic: If a record for this NetwatchConfigId and IpAddress exists, update it. Otherwise, insert.
                // This simple UPSERT assumes you want to overwrite the previous result for this IP under this config.
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
                    cmd.Parameters.AddWithValue("@EntityName", (object)ipDetail.EntityName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastPingStatus", pingStatusText);
                    cmd.Parameters.AddWithValue("@RoundtripTimeMs", roundtripTime.HasValue ? (object)roundtripTime.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastPingAttemptDateTime", pingAttemptTime);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<IndividualIpStatus> GetDetailedIpStatuses(int netwatchConfigId)
        {
            var detailedStatuses = new List<IndividualIpStatus>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                string query = @"
            SELECT EntityName, IpAddress, LastPingStatus, RoundtripTimeMs, LastPingAttemptDateTime
            FROM NetwatchIpResults
            WHERE NetwatchConfigId = @NetwatchConfigId
            ORDER BY EntityName, IpAddress;"; // Or any order you prefer

                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NetwatchConfigId", netwatchConfigId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            detailedStatuses.Add(new IndividualIpStatus
                            {
                                EntityName = reader["EntityName"] == DBNull.Value ? "N/A" : reader["EntityName"].ToString(),
                                IpAddress = reader["IpAddress"].ToString(),
                                LastPingStatus = reader["LastPingStatus"].ToString(),
                                RoundtripTimeMs = reader["RoundtripTimeMs"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["RoundtripTimeMs"]),
                                LastPingAttemptDateTime = Convert.ToDateTime(reader["LastPingAttemptDateTime"])
                            });
                        }
                    }
                }
            }
            return detailedStatuses;
        }
        #endregion
    }
}
