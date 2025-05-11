using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CustomerAndServerMaintenanceTracking.Models;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class TagHierarchyMappingRepository
    {
        private DatabaseHelper dbHelper;

        public TagHierarchyMappingRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        // Adds a mapping between a tag and a hierarchy group.
        public void AddMapping(int tagId, int groupId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO TagHierarchyMapping (TagId, TagHierarchyGroupId) VALUES (@TagId, @GroupId)", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        // Removes a mapping between a tag and a hierarchy group.
        public void RemoveMapping(int tagId, int groupId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM TagHierarchyMapping WHERE TagId=@TagId AND TagHierarchyGroupId=@GroupId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        // Retrieves all hierarchy group IDs for a given tag.
        public List<int> GetGroupsForTag(int tagId)
        {
            List<int> groupIds = new List<int>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT TagHierarchyGroupId FROM TagHierarchyMapping WHERE TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        groupIds.Add(Convert.ToInt32(reader["TagHierarchyGroupId"]));
                    }
                }
                conn.Close();
            }
            return groupIds;
        }

        // Retrieves all tag IDs for a given hierarchy group.
        public List<int> GetTagsForGroup(int groupId)
        {
            List<int> tagIds = new List<int>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT TagId FROM TagHierarchyMapping WHERE TagHierarchyGroupId=@GroupId", conn);
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tagIds.Add(Convert.ToInt32(reader["TagId"]));
                    }
                }
                conn.Close();
            }
            return tagIds;
        }
    }
}
