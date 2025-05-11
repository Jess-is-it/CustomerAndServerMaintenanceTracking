using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CustomerAndServerMaintenanceTracking.Models;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class TagRepository
    {
        private DatabaseHelper dbHelper;

        public TagRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        // Adds a new tag to the Tags table
        public void AddTag(TagClass tag)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Tags (TagName, TagDescription) VALUES (@TagName, @TagDescription)", conn);
                cmd.Parameters.AddWithValue("@TagName", tag.TagName);
                cmd.Parameters.AddWithValue("@TagDescription", tag.TagDescription);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public bool IsChildTag(int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM TagAssignments WHERE ChildTagId = @ChildId",
                    conn);
                cmd.Parameters.AddWithValue("@ChildId", tagId);

                int count = (int)cmd.ExecuteScalar();
                return (count > 0);
            }
        }
        public int GetChildTagCount(int parentTagId)
        {
            int count = 0;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Query the TagAssignments table where ParentTagId matches
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM TagAssignments WHERE ParentTagId = @ParentTagId", conn);
                cmd.Parameters.AddWithValue("@ParentTagId", parentTagId);
                count = (int)cmd.ExecuteScalar();
            }
            return count;
        }
        public List<int> GetAllDescendantTagIds(int parentTagId)
        {
            List<int> result = new List<int>();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(parentTagId);

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    if (!result.Contains(current))
                        result.Add(current);

                    SqlCommand cmd = new SqlCommand(
                        "SELECT ChildTagId FROM TagAssignments WHERE ParentTagId = @pid", conn);
                    cmd.Parameters.AddWithValue("@pid", current);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int childId = Convert.ToInt32(reader["ChildTagId"]);
                            // If not in result yet, enqueue it
                            if (!result.Contains(childId))
                                queue.Enqueue(childId);
                        }
                    }
                }
            }

            return result;
        }
        public void UpdateTag(TagClass tag)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // If this tag is a parent, override TagType
                if (tag.IsParent)
                {
                    tag.TagType = "Parent";
                }

                SqlCommand cmd = new SqlCommand(@"
            UPDATE Tags
            SET TagName=@TagName,
                TagDescription=@TagDescription,
                IsParent=@IsParent,
                TagType=@TagType
            WHERE Id=@Id", conn);

                cmd.Parameters.AddWithValue("@TagName", tag.TagName);
                cmd.Parameters.AddWithValue("@TagDescription",
                    string.IsNullOrEmpty(tag.TagDescription) ? (object)DBNull.Value : tag.TagDescription);
                cmd.Parameters.AddWithValue("@IsParent", tag.IsParent);

                cmd.Parameters.AddWithValue("@TagType",
                    string.IsNullOrEmpty(tag.TagType) ? (object)DBNull.Value : tag.TagType);

                cmd.Parameters.AddWithValue("@Id", tag.Id);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        // Deletes a tag and its associations from the database
        public void DeleteTag(int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // 1) Remove references from ClusterTagMapping
                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM ClusterTagMapping WHERE TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();

                // 2) Remove references from TagAssignments
                cmd = new SqlCommand(
                    "DELETE FROM TagAssignments WHERE ParentTagId=@TagId OR ChildTagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();

                // 3) Remove references from CustomerTags (already in your code)
                cmd = new SqlCommand(
                    "DELETE FROM CustomerTags WHERE TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();

                // 4) Finally, delete the tag itself
                cmd = new SqlCommand("DELETE FROM Tags WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", tagId);
                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        // Retrieves all tags from the database
        public List<TagClass> GetAllTags()
        {
            List<TagClass> tags = new List<TagClass>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, TagName, TagDescription FROM Tags", conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TagClass tag = new TagClass()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TagName = reader["TagName"].ToString(),
                            TagDescription = reader["TagDescription"].ToString()
                        };
                        tags.Add(tag);
                    }
                }
                conn.Close();
            }
            return tags;
        }
        public TagClass GetTagById(int id)
        {
            TagClass tag = null;
            using (SqlConnection conn = dbHelper.GetConnection()) // Ensure dbHelper is initialized
            {
                conn.Open();
                // MODIFIED QUERY: Added TagType and IsParent to the SELECT list
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, TagName, TagDescription, IsParent, TagType FROM Tags WHERE Id = @Id",
                     conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tag = new TagClass
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TagName = reader["TagName"].ToString(),
                            TagDescription = reader["TagDescription"] == DBNull.Value ? string.Empty : reader["TagDescription"].ToString(),
                            // ADDED MAPPING: Read IsParent and TagType
                            IsParent = reader["IsParent"] != DBNull.Value && Convert.ToBoolean(reader["IsParent"]),
                            TagType = reader["TagType"] == DBNull.Value ? string.Empty : reader["TagType"].ToString()
                            // Note: AssignedCustomerCount is not loaded here, as it requires a separate query or JOIN.
                        };
                    }
                }
                conn.Close(); // Consider adding finally block for closing if not using 'using' for SqlDataReader
            }
            return tag;
        }

        #region Method for Customers Tag
        public List<int> GetAssignedCustomerIds(int tagId)
        {
            List<int> customerIds = new List<int>();
            using (SqlConnection conn = dbHelper.GetConnection()) // Ensure dbHelper is initialized
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerId FROM CustomerTags WHERE TagId = @TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customerIds.Add(Convert.ToInt32(reader["CustomerId"]));
                    }
                }
                conn.Close();
            }
            return customerIds;
        }
        public List<TagDisplayModel> GetAllTagsWithCluster()
        {
            List<TagDisplayModel> list = new List<TagDisplayModel>();
            using (SqlConnection conn = dbHelper.GetConnection()) // Assuming dbHelper is initialized
            {
                conn.Open();
                // MODIFIED QUERY: Added t.TagType to the SELECT list
                string query = @"
            SELECT t.Id, t.TagName, t.TagDescription, t.IsParent, t.TagType, nc.ClusterName
            FROM Tags t
            LEFT JOIN ClusterTagMapping ctm ON t.Id = ctm.TagId
            LEFT JOIN NetworkClusters nc ON ctm.ClusterId = nc.Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TagDisplayModel model = new TagDisplayModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TagName = reader["TagName"].ToString(),
                            TagDescription = reader["TagDescription"] == DBNull.Value ? string.Empty : reader["TagDescription"].ToString(),
                            NetworkCluster = reader["ClusterName"] == DBNull.Value ? "N/A" : reader["ClusterName"].ToString(),
                            IsParent = reader["IsParent"] != DBNull.Value && Convert.ToBoolean(reader["IsParent"]),
                            // ADDED MAPPING: Read TagType from the reader
                            TagType = reader["TagType"] == DBNull.Value ? string.Empty : reader["TagType"].ToString()
                        };
                        list.Add(model);
                    }
                }
                conn.Close();
            }
            return list;
        }
        // Associates a tag with a customer in the join table
        public void AssignTagToCustomer(int customerId, int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Check if there's already a row for this (customerId, tagId).
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM CustomerTags WHERE CustomerId=@CustomerId AND TagId=@TagId",
                    conn);
                checkCmd.Parameters.AddWithValue("@CustomerId", customerId);
                checkCmd.Parameters.AddWithValue("@TagId", tagId);
                int count = (int)checkCmd.ExecuteScalar();

                if (count == 0)
                {
                    // Insert only if it doesn't exist yet.
                    SqlCommand insertCmd = new SqlCommand(
                        "INSERT INTO CustomerTags (CustomerId, TagId) VALUES (@CustomerId, @TagId)",
                        conn);
                    insertCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    insertCmd.Parameters.AddWithValue("@TagId", tagId);
                    insertCmd.ExecuteNonQuery();
                }

                conn.Close();
            }
        }
        // Removes a tag association from a customer
        public void RemoveTagFromCustomer(int customerId, int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM CustomerTags WHERE CustomerId=@CustomerId AND TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void AssignTagToTag(int parentTagId, int childTagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO TagAssignments (ParentTagId, ChildTagId) VALUES (@ParentTagId, @ChildTagId)", conn);
                cmd.Parameters.AddWithValue("@ParentTagId", parentTagId);
                cmd.Parameters.AddWithValue("@ChildTagId", childTagId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        // Removes anassociation between a child tag and a parent tag.
        public void RemoveTagFromTag(int parentTagId, int childTagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM TagAssignments WHERE ParentTagId=@ParentTagId AND ChildTagId=@ChildTagId", conn);
                cmd.Parameters.AddWithValue("@ParentTagId", parentTagId);
                cmd.Parameters.AddWithValue("@ChildTagId", childTagId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public List<string> GetAssignedEntities(int tagId)
        {
            List<string> entities = new List<string>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // Retrieve assigned customers.
                string queryCustomers = @"
            SELECT c.AccountName 
            FROM CustomerTags ct 
            INNER JOIN Customers c ON ct.CustomerId = c.Id 
            WHERE ct.TagId = @TagId";
                SqlCommand cmdCustomers = new SqlCommand(queryCustomers, conn);
                cmdCustomers.Parameters.AddWithValue("@TagId", tagId);
                using (SqlDataReader reader = cmdCustomers.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(reader["AccountName"].ToString());
                    }
                }

                // Retrieve assigned child tags (if applicable).
                string queryTags = @"
            SELECT t.TagName 
            FROM TagAssignments ta 
            INNER JOIN Tags t ON ta.ChildTagId = t.Id 
            WHERE ta.ParentTagId = @TagId";
                SqlCommand cmdTags = new SqlCommand(queryTags, conn);
                cmdTags.Parameters.AddWithValue("@TagId", tagId);
                using (SqlDataReader reader = cmdTags.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(reader["TagName"].ToString());
                    }
                }

                conn.Close();
            }

            return entities;
        }
        public List<TagAssignment> GetAllTagAssignments()
        {
            List<TagAssignment> assignments = new List<TagAssignment>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT ParentTagId, ChildTagId FROM TagAssignments", conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        assignments.Add(new TagAssignment
                        {
                            ParentTagId = Convert.ToInt32(reader["ParentTagId"]),
                            ChildTagId = Convert.ToInt32(reader["ChildTagId"])
                        });
                    }
                }
                conn.Close();
            }
            return assignments;
        }
        public List<TagClass> GetTagsForCluster(int clusterId)
        {
           
            List<TagClass> tags = new List<TagClass>();
            using (SqlConnection conn = dbHelper.GetConnection()) // Ensure dbHelper is initialized
            {
                conn.Open();
                // MODIFIED QUERY: Added t.TagType to the SELECT list
                string query = @"
            SELECT t.Id, t.TagName, t.TagDescription, t.IsParent, t.TagType
            FROM ClusterTagMapping ctm
            JOIN Tags t ON ctm.TagId = t.Id
            WHERE ctm.ClusterId = @ClusterId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ClusterId", clusterId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TagClass tag = new TagClass
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TagName = reader["TagName"].ToString(),
                            TagDescription = reader["TagDescription"] == DBNull.Value
                                             ? string.Empty
                                             : reader["TagDescription"].ToString(),
                            IsParent = Convert.ToBoolean(reader["IsParent"]),
                            // ADDED MAPPING: Read the TagType from the database
                            TagType = reader["TagType"] == DBNull.Value
                                      ? string.Empty // Or null, depending on your preference
                                      : reader["TagType"].ToString()
                        };
                        tags.Add(tag);
                    }
                }
                conn.Close(); // Consider using block for conn to ensure disposal
            }
            return tags;
        }
        public int AddTagAndReturnId(TagClass tag)
        {
            int newId = 0;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // If the tag is a parent, forcibly set TagType to "Parent" (or null).
                if (tag.IsParent)
                {
                    tag.TagType = "Parent"; // or (object)DBNull.Value if you prefer to store nothing
                }

                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO Tags (TagName, TagDescription, IsParent, TagType)
            OUTPUT INSERTED.Id
            VALUES (@TagName, @TagDescription, @IsParent, @TagType)", conn);

                cmd.Parameters.AddWithValue("@TagName", tag.TagName);
                cmd.Parameters.AddWithValue("@TagDescription",
                    string.IsNullOrEmpty(tag.TagDescription) ? (object)DBNull.Value : tag.TagDescription);
                cmd.Parameters.AddWithValue("@IsParent", tag.IsParent);

                // If TagType is null or empty, store as DB null:
                cmd.Parameters.AddWithValue("@TagType",
                    string.IsNullOrEmpty(tag.TagType) ? (object)DBNull.Value : tag.TagType);

                object result = cmd.ExecuteScalar();
                if (result != null)
                    newId = Convert.ToInt32(result);

                conn.Close();
            }
            return newId;
        }
        public List<TagClass> GetAllTagsWithAssignedCount()
        {
            List<TagClass> tags = new List<TagClass>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
            SELECT 
                t.Id, 
                t.TagName, 
                t.TagDescription,
                t.IsParent,
                (
                    SELECT COUNT(*) 
                    FROM CustomerTags ct 
                    WHERE ct.TagId = t.Id
                ) AS AssignedCount
            FROM Tags t";
                SqlCommand cmd = new SqlCommand(sql, conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TagClass tag = new TagClass()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TagName = reader["TagName"].ToString(),
                            TagDescription = reader["TagDescription"].ToString(),
                            IsParent = reader["IsParent"] != DBNull.Value && Convert.ToBoolean(reader["IsParent"]),
                            AssignedCustomerCount = Convert.ToInt32(reader["AssignedCount"])
                        };
                        tags.Add(tag);
                    }
                }
                conn.Close();
            }
            return tags;
        }
        #endregion

        #region Method ManageTagTree

        #endregion

        #region Method for Device IP
        public void AssignTagToDeviceIP(int deviceIPId, int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Check if it already exists
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM DeviceIPTags WHERE DeviceIPId=@DeviceIPId AND TagId=@TagId",
                    conn);
                checkCmd.Parameters.AddWithValue("@DeviceIPId", deviceIPId);
                checkCmd.Parameters.AddWithValue("@TagId", tagId);
                int count = (int)checkCmd.ExecuteScalar();

                if (count == 0)
                {
                    SqlCommand insertCmd = new SqlCommand(
                        "INSERT INTO DeviceIPTags (DeviceIPId, TagId) VALUES (@DeviceIPId, @TagId)",
                        conn);
                    insertCmd.Parameters.AddWithValue("@DeviceIPId", deviceIPId);
                    insertCmd.Parameters.AddWithValue("@TagId", tagId);
                    insertCmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
        public void RemoveTagFromDeviceIP(int deviceIPId, int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM DeviceIPTags WHERE DeviceIPId=@DeviceIPId AND TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@DeviceIPId", deviceIPId);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public List<int> GetDeviceIPsForTag(int tagId)
        {
            List<int> deviceIds = new List<int>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT DeviceIPId FROM DeviceIPTags WHERE TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        deviceIds.Add(Convert.ToInt32(reader["DeviceIPId"]));
                    }
                }
                conn.Close();
            }
            return deviceIds;
        }
        public List<DeviceIP> GetAssignedDeviceIPs(int tagId)
        {
            List<DeviceIP> devices = new List<DeviceIP>();
            using (SqlConnection conn = dbHelper.GetConnection()) // Ensure dbHelper is initialized
            {
                conn.Open();
                // Query to join DeviceIPTags and DeviceIPs tables
                string query = @"
            SELECT d.Id, d.DeviceName, d.IPAddress, d.Location
            FROM DeviceIPTags dt
            JOIN DeviceIPs d ON dt.DeviceIPId = d.Id
            WHERE dt.TagId = @TagId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DeviceIP device = new DeviceIP
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            DeviceName = reader["DeviceName"].ToString(),
                            IPAddress = reader["IPAddress"] == DBNull.Value ? string.Empty : reader["IPAddress"].ToString(),
                            Location = reader["Location"] == DBNull.Value ? string.Empty : reader["Location"].ToString()
                            // TagType is not stored in DeviceIPs table, so we don't retrieve it here
                        };
                        devices.Add(device);
                    }
                }
                conn.Close();
            }
            return devices;
        }
        public int GetAssignedDeviceIPCount(int tagId)
        {
            int count = 0;
            using (SqlConnection conn = dbHelper.GetConnection()) // Ensure dbHelper is initialized
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM DeviceIPTags WHERE TagId = @TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    count = Convert.ToInt32(result);
                }
                conn.Close();
            }
            return count;
        }
        #endregion


    }
}
