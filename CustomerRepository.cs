using System;
using System.Data.SqlClient;
using CustomerAndServerMaintenanceTracking.Models;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class CustomerRepository
    {
        private DatabaseHelper dbHelper;

        public CustomerRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        public void InsertOrUpdateCustomer(Customer customer)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Check if the customer already exists based on AccountName
                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE AccountName=@AccountName", conn);
                checkCmd.Parameters.AddWithValue("@AccountName", customer.AccountName);
                int count = (int)checkCmd.ExecuteScalar();

                SqlCommand cmd;
                if (count > 0)
                {
                    // Update existing customer record
                    cmd = new SqlCommand("UPDATE Customers SET AdditionalName=@AdditionalName, ContactNumber=@ContactNumber, Email=@Email, Location=@Location WHERE AccountName=@AccountName", conn);
                }
                else
                {
                    // Insert new customer record
                    cmd = new SqlCommand("INSERT INTO Customers (AccountName, AdditionalName, ContactNumber, Email, Location) VALUES (@AccountName, @AdditionalName, @ContactNumber, @Email, @Location)", conn);
                }

                cmd.Parameters.AddWithValue("@AccountName", customer.AccountName);
                cmd.Parameters.AddWithValue("@AdditionalName", string.IsNullOrEmpty(customer.AdditionalName) ? (object)DBNull.Value : customer.AdditionalName);
                cmd.Parameters.AddWithValue("@ContactNumber", string.IsNullOrEmpty(customer.ContactNumber) ? (object)DBNull.Value : customer.ContactNumber);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(customer.Email) ? (object)DBNull.Value : customer.Email);
                cmd.Parameters.AddWithValue("@Location", string.IsNullOrEmpty(customer.Location) ? (object)DBNull.Value : customer.Location);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
