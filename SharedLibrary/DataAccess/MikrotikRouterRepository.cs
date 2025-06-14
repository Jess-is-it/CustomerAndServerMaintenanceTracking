using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class MikrotikRouterRepository
    {
        private readonly DatabaseHelper dbHelper;

        public MikrotikRouterRepository()
        {
            dbHelper = new DatabaseHelper();
        }


        public void AddRouter(MikrotikRouter router)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Removed IPPort from the INSERT statement
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
            }
        }

        public List<MikrotikRouter> GetRouters()
        {
            List<MikrotikRouter> routers = new List<MikrotikRouter>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Removed IPPort from the SELECT statement
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, RouterName, HostIPAddress, ApiPort, Username, Password FROM Routers",
                    conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        MikrotikRouter router = new MikrotikRouter
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            RouterName = reader["RouterName"].ToString(),
                            HostIPAddress = reader["HostIPAddress"].ToString(),
                            ApiPort = Convert.ToInt32(reader["ApiPort"]),
                            Username = reader["Username"].ToString(),
                            Password = reader["Password"].ToString()
                            // Removed IPPort assignment
                        };
                        routers.Add(router);
                    }
                }
            }
            return routers;
        }

        public void UpdateRouter(MikrotikRouter router)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Removed IPPort from the UPDATE statement
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Routers SET RouterName=@RouterName, HostIPAddress=@HostIPAddress, ApiPort=@ApiPort, Username=@Username, Password=@Password WHERE Id=@Id",
                    conn);
                cmd.Parameters.AddWithValue("@Id", router.Id);
                cmd.Parameters.AddWithValue("@RouterName", router.RouterName);
                cmd.Parameters.AddWithValue("@HostIPAddress", router.HostIPAddress);
                cmd.Parameters.AddWithValue("@ApiPort", router.ApiPort);
                cmd.Parameters.AddWithValue("@Username", router.Username);
                cmd.Parameters.AddWithValue("@Password", router.Password);

                cmd.ExecuteNonQuery();
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
            }
        }
    }
}
