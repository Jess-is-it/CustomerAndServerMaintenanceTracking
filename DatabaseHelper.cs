using System;
using System.Data.SqlClient;
using System.Configuration; // Make sure your project references System.Configuration

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class DatabaseHelper
    {
        private string connectionString;

        public DatabaseHelper()
        {
            // Retrieve the connection string from App.config
            connectionString = ConfigurationManager.ConnectionStrings["CustomerAndServerMaintenanceTracking"].ConnectionString;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        // Example method to test the connection to the database
        public bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    conn.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                // Here you can log the exception if needed
                return false;
            }
        }
    }
}
