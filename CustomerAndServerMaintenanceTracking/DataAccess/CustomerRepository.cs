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
                        // Update existing customer, ensure it's active (IsArchived=0)
                        cmd = new SqlCommand(@"
                    UPDATE Customers 
                    SET 
                        AdditionalName = @AdditionalName, 
                        ContactNumber = @ContactNumber, 
                        Email = @Email, 
                        Location = @Location, 
                        IPAddress = @IPAddress,
                        IsArchived = 0
                    WHERE AccountName = @AccountName", conn);
                    }
                    else
                    {
                        // Insert new customer as active (IsArchived = 0)
                        cmd = new SqlCommand(@"
                    INSERT INTO Customers 
                        (AccountName, AdditionalName, ContactNumber, Email, Location, IPAddress, IsArchived) 
                    VALUES 
                        (@AccountName, @AdditionalName, @ContactNumber, @Email, @Location, @IPAddress, 0)", conn);
                    }

                    cmd.Parameters.AddWithValue("@AccountName", customer.AccountName);
                    cmd.Parameters.AddWithValue("@AdditionalName", string.IsNullOrEmpty(customer.AdditionalName) ? (object)DBNull.Value : customer.AdditionalName);
                    cmd.Parameters.AddWithValue("@ContactNumber", string.IsNullOrEmpty(customer.ContactNumber) ? (object)DBNull.Value : customer.ContactNumber);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(customer.Email) ? (object)DBNull.Value : customer.Email);
                    cmd.Parameters.AddWithValue("@Location", string.IsNullOrEmpty(customer.Location) ? (object)DBNull.Value : customer.Location);

                    // New parameter for the IP address
                    cmd.Parameters.AddWithValue("@IPAddress", string.IsNullOrEmpty(customer.IPAddress) ? (object)DBNull.Value : customer.IPAddress);

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
                // Include IPAddress column in the SELECT statement.
                SqlCommand cmd = new SqlCommand("SELECT Id, AccountName, AdditionalName, ContactNumber, Email, Location, IsArchived, IPAddress FROM Customers", conn);
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
                            // Map the IsArchived value.
                            IsArchived = reader["IsArchived"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsArchived"]),
                            // Map the IPAddress value.
                            IPAddress = reader["IPAddress"] == DBNull.Value ? string.Empty : reader["IPAddress"].ToString()
                        };
                        customers.Add(customer);
                    }
                }
                conn.Close();
            }

            return customers;
        }

        public List<Customer> GetCustomersByTagIds(List<int> tagIds)
        {
            // If no tagIds provided, return empty list
            if (tagIds == null || tagIds.Count == 0)
                return new List<Customer>();

            List<Customer> customers = new List<Customer>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // Build a parameterized IN clause, e.g.:  WHERE ct.TagId IN (@TagId0, @TagId1, ...)
                var paramNames = new List<string>();
                for (int i = 0; i < tagIds.Count; i++)
                {
                    paramNames.Add("@TagId" + i);
                }
                string inClause = string.Join(", ", paramNames);

                // Create the query
                string query = $@"
            SELECT c.Id, c.AccountName, c.AdditionalName, c.ContactNumber, c.Email, c.Location, c.IsArchived, c.IPAddress
            FROM Customers c
            JOIN CustomerTags ct ON c.Id = ct.CustomerId
            WHERE ct.TagId IN ({inClause})
        ";

                SqlCommand cmd = new SqlCommand(query, conn);

                // Add parameters for each tag ID
                for (int i = 0; i < tagIds.Count; i++)
                {
                    cmd.Parameters.AddWithValue(paramNames[i], tagIds[i]);
                }

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
                            IsArchived = reader["IsArchived"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsArchived"]),
                            IPAddress = reader["IPAddress"] == DBNull.Value ? string.Empty : reader["IPAddress"].ToString()
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
