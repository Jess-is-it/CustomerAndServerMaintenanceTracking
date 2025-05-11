using CustomerAndServerMaintenanceTracking.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class PingTaskRepository
    {
        private DatabaseHelper dbHelper;

        public PingTaskRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        // Insert a new ping task
        public void AddPingTask(PingTask task)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO PingTasks (PingName, TargetIP, PingIntervalMs, Status, CreatedDate, TagId) " +
                    "VALUES (@PingName, @TargetIP, @PingIntervalMs, @Status, @CreatedDate, @TagId)",
                    conn);

                cmd.Parameters.AddWithValue("@PingName", task.PingName);
                cmd.Parameters.AddWithValue("@TargetIP", task.TargetIP);
                cmd.Parameters.AddWithValue("@PingIntervalMs", task.PingIntervalMs);
                cmd.Parameters.AddWithValue("@Status", (object)task.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedDate", task.CreatedDate);
                cmd.Parameters.AddWithValue("@TagId", task.TagId);

                cmd.ExecuteNonQuery();
            }
        }


        // Retrieve all ping tasks
        public List<PingTask> GetAllPingTasks()
        {
            List<PingTask> tasks = new List<PingTask>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, PingName, TargetIP, PingIntervalMs, Status, CreatedDate FROM PingTasks", conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PingTask task = new PingTask()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            PingName = reader["PingName"].ToString(),
                            TargetIP = reader["TargetIP"].ToString(),
                            PingIntervalMs = Convert.ToInt32(reader["PingIntervalMs"]),
                            Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                            CreatedDate = (DateTime)reader["CreatedDate"]
                        };
                        tasks.Add(task);
                    }
                }
            }
            return tasks;
        }

        // Update an existing ping task
        public void UpdatePingTask(PingTask task)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE PingTasks SET PingName=@PingName, TargetIP=@TargetIP, PingIntervalMs=@PingIntervalMs, Status=@Status " +
                    "WHERE Id=@Id",
                    conn);

                cmd.Parameters.AddWithValue("@PingName", task.PingName);
                cmd.Parameters.AddWithValue("@TargetIP", task.TargetIP);
                cmd.Parameters.AddWithValue("@PingIntervalMs", task.PingIntervalMs);
                cmd.Parameters.AddWithValue("@Status", (object)task.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", task.Id);

                cmd.ExecuteNonQuery();
            }
        }

        // Delete a ping task
        public void DeletePingTask(int id)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM PingTasks WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public List<PingTask> GetPingTasksByIP(string ipAddress)
        {
            List<PingTask> tasks = new List<PingTask>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, PingName, TargetIP, PingIntervalMs, Status, CreatedDate " +
                    "FROM PingTasks WHERE TargetIP = @TargetIP",
                    conn);
                cmd.Parameters.AddWithValue("@TargetIP", ipAddress);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PingTask task = new PingTask()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            PingName = reader["PingName"].ToString(),
                            TargetIP = reader["TargetIP"].ToString(),
                            PingIntervalMs = Convert.ToInt32(reader["PingIntervalMs"]),
                            Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                            CreatedDate = (DateTime)reader["CreatedDate"]
                        };
                        tasks.Add(task);
                    }
                }
            }
            return tasks;
        }

        public List<PingTask> GetPingTasksByIPAndTag(string ipAddress, int tagId)
        {
            List<PingTask> tasks = new List<PingTask>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, PingName, TargetIP, PingIntervalMs, Status, CreatedDate, TagId " +
                    "FROM PingTasks WHERE TargetIP = @TargetIP AND TagId = @TagId",
                    conn);
                cmd.Parameters.AddWithValue("@TargetIP", ipAddress);
                cmd.Parameters.AddWithValue("@TagId", tagId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PingTask task = new PingTask()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            PingName = reader["PingName"].ToString(),
                            TargetIP = reader["TargetIP"].ToString(),
                            PingIntervalMs = Convert.ToInt32(reader["PingIntervalMs"]),
                            Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                            CreatedDate = (DateTime)reader["CreatedDate"],
                            TagId = Convert.ToInt32(reader["TagId"])
                        };
                        tasks.Add(task);
                    }
                }
            }
            return tasks;
        }

    }
}
