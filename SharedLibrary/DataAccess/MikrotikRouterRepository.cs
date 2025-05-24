using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace SharedLibrary.DataAccess
{
    public class MikrotikRouterRepository
    {
        private DatabaseHelper dbHelper;

        public MikrotikRouterRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        // Adds a new router record to the Routers table.
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
                conn.Close();
            }
        }

        // Retrieves all router records from the Routers table.
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
                conn.Close(); // Explicitly close connection
            }

            return routers;
        }

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
                conn.Close(); // Explicitly close connection
            }
        }

        public void DeleteRouter(int routerId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Routers WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", routerId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }


    }
}
