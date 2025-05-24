using System;
using System.Data.SqlClient;
using System.Configuration; // Make sure your project references System.Configuration


namespace SharedLibrary.DataAccess
{
    public class DatabaseHelper
    {
        private string connectionString;

        public DatabaseHelper()
        {
            // Retrieve the connection string from the calling application's App.config/Web.config
            connectionString = ConfigurationManager.ConnectionStrings["CustomerAndServerMaintenanceTracking"].ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                // This error will be more relevant for the service applications if their App.config is missing the string.
                // For the UI app, it should find its own App.config.
                string errorMessage = "FATAL ERROR: Database connection string 'CustomerAndServerMaintenanceTracking' not found in application configuration.";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {errorMessage}"); // Good for service logs
                                                                                    // Consider a more specific exception or logging for production
                throw new ConfigurationErrorsException(errorMessage);
            }
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
