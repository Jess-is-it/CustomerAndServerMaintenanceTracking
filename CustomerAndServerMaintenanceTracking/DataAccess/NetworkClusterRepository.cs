using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CustomerAndServerMaintenanceTracking.Models;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class NetworkClusterRepository
    {
        private DatabaseHelper dbHelper;

        public NetworkClusterRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        public void AddCluster(NetworkCluster cluster)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO NetworkClusters (ClusterName, ClusterDescription) VALUES (@Name, @Description)",
                    conn);
                cmd.Parameters.AddWithValue("@Name", cluster.ClusterName);
                cmd.Parameters.AddWithValue("@Description", cluster.ClusterDescription);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public List<NetworkCluster> GetClusters()
        {
            List<NetworkCluster> clusters = new List<NetworkCluster>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, ClusterName, ClusterDescription FROM NetworkClusters",
                    conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        clusters.Add(new NetworkCluster
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ClusterName = reader["ClusterName"].ToString(),
                            ClusterDescription = reader["ClusterDescription"].ToString()
                        });
                    }
                }
                conn.Close();
            }
            return clusters;
        }

        public int? GetClusterIdForTag(int tagId)
        {
            int? clusterId = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT TOP 1 ClusterId FROM ClusterTagMapping WHERE TagId = @TagId", conn);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    clusterId = Convert.ToInt32(result);
                conn.Close();
            }
            return clusterId;
        }
        public void UpdateCluster(NetworkCluster cluster)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE NetworkClusters SET ClusterName=@Name, ClusterDescription=@Description WHERE Id=@Id",
                    conn
                );
                cmd.Parameters.AddWithValue("@Name", cluster.ClusterName);
                cmd.Parameters.AddWithValue("@Description", cluster.ClusterDescription);
                cmd.Parameters.AddWithValue("@Id", cluster.Id);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void DeleteCluster(int clusterId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // Remove any references from tables that use ClusterId as a foreign key
                // e.g., ClusterTagMapping
                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM ClusterTagMapping WHERE ClusterId = @ClusterId",
                    conn
                );
                cmd.Parameters.AddWithValue("@ClusterId", clusterId);
                cmd.ExecuteNonQuery();

                // Now remove the cluster itself
                cmd = new SqlCommand(
                    "DELETE FROM NetworkClusters WHERE Id = @Id",
                    conn
                );
                cmd.Parameters.AddWithValue("@Id", clusterId);
                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }
        public NetworkCluster GetClusterById(int clusterId)
        {
            NetworkCluster cluster = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, ClusterName, ClusterDescription FROM NetworkClusters WHERE Id = @Id",
                    conn);
                cmd.Parameters.AddWithValue("@Id", clusterId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) // Expecting only one row
                    {
                        cluster = new NetworkCluster
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ClusterName = reader["ClusterName"].ToString(),
                            ClusterDescription = reader["ClusterDescription"].ToString()
                        };
                    }
                }
                conn.Close();
            }
            return cluster; // Will return null if not found
        }
    }
}
