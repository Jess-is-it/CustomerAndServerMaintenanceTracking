using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPPoESyncService.Models;
using SharedLibrary.DataAccess;

namespace PPPoESyncService.DataAccess
{
    public class MikrotikRouterRepository
    {
        private DatabaseHelper dbHelper;

        public MikrotikRouterRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        // Adds a new router record to the Routers table.
        // Note: This service might not need to Add/Update/Delete routers,
        // as that's typically managed by the UI application.
        // However, including it for completeness or potential future use.
        public void AddRouter(MikrotikRouter router)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Routers (RouterName, HostIPAddress, ApiPort, Username, Password) " +
                    "VALUES (@RouterName, @HostIPAddress, @ApiPort, @Username, @Password)",
                    conn);

                cmd.Parameters.AddWithValue("@RouterName", router.RouterName);
                cmd.Parameters.AddWithValue("@HostIPAddress", router.HostIPAddress);
                cmd.Parameters.AddWithValue("@ApiPort", router.ApiPort);
                cmd.Parameters.AddWithValue("@Username", router.Username);
                cmd.Parameters.AddWithValue("@Password", router.Password);

                cmd.ExecuteNonQuery();
                // conn.Close(); // Using block handles closing
            }
        }

        // Retrieves all router records from the Routers table.
        // The service will use this to get connection details.
        public List<MikrotikRouter> GetRouters()
        {
            List<MikrotikRouter> routers = new List<MikrotikRouter>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, RouterName, HostIPAddress, ApiPort, Username, Password FROM Routers",
                    conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        MikrotikRouter router = new MikrotikRouter()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            RouterName = reader["RouterName"].ToString(),
                            HostIPAddress = reader["HostIPAddress"].ToString(),
                            ApiPort = Convert.ToInt32(reader["ApiPort"]),
                            Username = reader["Username"].ToString(),
                            Password = reader["Password"].ToString()
                        };
                        routers.Add(router);
                    }
                }
                // conn.Close(); // Using block handles closing
            }
            return routers;
        }

        // Updates an existing router record.
        // Likely not used by this service directly, but included for completeness.
        public void UpdateRouter(MikrotikRouter router)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Routers SET RouterName=@RouterName, HostIPAddress=@HostIPAddress, ApiPort=@ApiPort, Username=@Username, Password=@Password WHERE Id=@Id",
                    conn);
                cmd.Parameters.AddWithValue("@RouterName", router.RouterName);
                cmd.Parameters.AddWithValue("@HostIPAddress", router.HostIPAddress);
                cmd.Parameters.AddWithValue("@ApiPort", router.ApiPort);
                cmd.Parameters.AddWithValue("@Username", router.Username);
                cmd.Parameters.AddWithValue("@Password", router.Password);
                cmd.Parameters.AddWithValue("@Id", router.Id);

                cmd.ExecuteNonQuery();
                // conn.Close(); // Using block handles closing
            }
        }

        // Deletes a router record.
        // Likely not used by this service directly.
        public void DeleteRouter(int routerId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Routers WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", routerId);
                cmd.ExecuteNonQuery();
                // conn.Close(); // Using block handles closing
            }
        }
    }
}
