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
        public void AddTag(Tag tag)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Tags (TagName) VALUES (@TagName)", conn);
                cmd.Parameters.AddWithValue("@TagName", tag.TagName);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        // Updates an existing tag's name
        public void UpdateTag(Tag tag)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Tags SET TagName=@TagName WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@TagName", tag.TagName);
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
                // Delete related records in CustomerTags first
                SqlCommand cmd = new SqlCommand("DELETE FROM CustomerTags WHERE TagId=@TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();

                // Then delete the tag itself
                cmd = new SqlCommand("DELETE FROM Tags WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", tagId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        // Retrieves all tags from the database
        public List<Tag> GetAllTags()
        {
            List<Tag> tags = new List<Tag>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, TagName FROM Tags", conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Tag tag = new Tag()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TagName = reader["TagName"].ToString()
                        };
                        tags.Add(tag);
                    }
                }
                conn.Close();
            }
            return tags;
        }

        // Associates a tag with a customer in the join table
        public void AssignTagToCustomer(int customerId, int tagId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO CustomerTags (CustomerId, TagId) VALUES (@CustomerId, @TagId)", conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();
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
    }
}
