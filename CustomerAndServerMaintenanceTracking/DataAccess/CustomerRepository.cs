using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Models;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class CustomerRepository
    {
        private DatabaseHelper dbHelper;

        // Event to notify that customer data has been updated.
        public static event EventHandler CustomerUpdated;

        public CustomerRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        public void InsertOrUpdateCustomer(Customer customer)
        {
            try
            {
                using (SqlConnection conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    // Check if the customer exists
                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE AccountName=@AccountName", conn);
                    checkCmd.Parameters.AddWithValue("@AccountName", customer.AccountName);
                    int count = (int)checkCmd.ExecuteScalar();

                    SqlCommand cmd;
                    if (count > 0)
                    {
                        // Update existing customer, ensure it's active
                        cmd = new SqlCommand("UPDATE Customers SET AdditionalName=@AdditionalName, ContactNumber=@ContactNumber, Email=@Email, Location=@Location, IsArchived=0 WHERE AccountName=@AccountName", conn);
                    }
                    else
                    {
                        // Insert new customer as active (IsArchived = 0)
                        cmd = new SqlCommand("INSERT INTO Customers (AccountName, AdditionalName, ContactNumber, Email, Location, IsArchived) VALUES (@AccountName, @AdditionalName, @ContactNumber, @Email, @Location, 0)", conn);
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
            catch (Exception ex)
            {
                throw new Exception("Error in InsertOrUpdateCustomer: " + ex.Message);
            }
        }

        public void ArchiveCustomer(string accountName)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Customers SET IsArchived = 1 WHERE AccountName = @AccountName", conn);
                cmd.Parameters.AddWithValue("@AccountName", accountName);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void MarkActiveCustomer(string accountName)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Customers SET IsArchived = 0 WHERE AccountName = @AccountName", conn);
                cmd.Parameters.AddWithValue("@AccountName", accountName);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }


        public List<Customer> GetCustomers()
        {
            List<Customer> customers = new List<Customer>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Include the IsArchived column in your SELECT statement.
                SqlCommand cmd = new SqlCommand("SELECT Id, AccountName, AdditionalName, ContactNumber, Email, Location, IsArchived FROM Customers", conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Customer customer = new Customer()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            AccountName = reader["AccountName"].ToString(),
                            AdditionalName = reader["AdditionalName"] == DBNull.Value ? string.Empty : reader["AdditionalName"].ToString(),
                            ContactNumber = reader["ContactNumber"] == DBNull.Value ? string.Empty : reader["ContactNumber"].ToString(),
                            Email = reader["Email"] == DBNull.Value ? string.Empty : reader["Email"].ToString(),
                            Location = reader["Location"] == DBNull.Value ? string.Empty : reader["Location"].ToString(),
                            // Map the IsArchived value
                            IsArchived = reader["IsArchived"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsArchived"])
                        };
                        customers.Add(customer);
                    }
                }
                conn.Close();
            }

            return customers;
        }
    }
}
