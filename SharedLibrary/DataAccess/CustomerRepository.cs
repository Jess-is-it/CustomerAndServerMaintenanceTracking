using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
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
            if (customer == null)
            {
                Console.WriteLine($"ERROR in InsertOrUpdateCustomer: Customer object is null. Skipping operation.");
                return;
            }

            // MODIFICATION HERE: Trim AccountName and check if it's null or empty
            string trimmedAccountName = customer.AccountName?.Trim();

            if (string.IsNullOrEmpty(trimmedAccountName))
            {
                Console.WriteLine($"ERROR in InsertOrUpdateCustomer: Customer AccountName is null or empty after trimming. Skipping operation for MkID: '{customer.MikrotikSecretId}', RouterID: {customer.RouterId}.");
                return;
            }

            // Enhanced logging for incoming data
            Console.WriteLine($"InsertOrUpdateCustomer - Incoming Data (trimmed AccountName): Account='{trimmedAccountName}', RouterId='{customer.RouterId?.ToString() ?? "NULL"}', MkID='{customer.MikrotikSecretId ?? "NULL"}', IP='{customer.IPAddress ?? "NULL"}', MAC='{customer.MacAddress ?? "NULL"}', Archived='{customer.IsArchived}'");

            try
            {
                using (SqlConnection conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd;
                    int? existingDbCustomerId = null;
                    bool isAdoptingRecord = false;

                    // --- REVISED MATCHING LOGIC ---

                    // Priority 1: Try to find by MikrotikSecretId AND RouterId (most reliable for already-synced entries)
                    if (!string.IsNullOrEmpty(customer.MikrotikSecretId) && customer.RouterId.HasValue)
                    {
                        SqlCommand checkCmd1 = new SqlCommand(
                            // MODIFICATION HERE: Added TRIM to AccountName in WHERE clause for safety, though not the primary match key here.
                            // This query primarily matches on MikrotikSecretId and RouterId.
                            "SELECT Id FROM Customers WHERE MikrotikSecretId = @MikrotikSecretId AND RouterId = @RouterId", conn);
                        checkCmd1.Parameters.AddWithValue("@MikrotikSecretId", customer.MikrotikSecretId);
                        checkCmd1.Parameters.AddWithValue("@RouterId", customer.RouterId.Value);
                        // For safety, if AccountName were also part of this specific check, we'd use:
                        // checkCmd1.Parameters.AddWithValue("@AccountName", trimmedAccountName);
                        // AND UPPER(TRIM(AccountName)) = UPPER(@AccountName)
                        object result1 = checkCmd1.ExecuteScalar();
                        if (result1 != null && result1 != DBNull.Value)
                        {
                            existingDbCustomerId = Convert.ToInt32(result1);
                            Console.WriteLine($"Found existing customer by MikrotikSecretId '{customer.MikrotikSecretId}' and RouterId {customer.RouterId.Value}. DB ID: {existingDbCustomerId}");
                        }
                    }

                    Console.WriteLine($"DEBUG: Before Priority 2 Check - existingDbCustomerId.HasValue = {existingDbCustomerId.HasValue}");
                    Console.WriteLine($"DEBUG: Before Priority 2 Check - customer.RouterId = {customer.RouterId?.ToString() ?? "NULL"}");
                    Console.WriteLine($"DEBUG: Before Priority 2 Check - customer.RouterId.HasValue = {customer.RouterId.HasValue}");
                    Console.WriteLine($"DEBUG: Before Priority 2 Check - trimmedAccountName for query = '{trimmedAccountName}'");


                    // Priority 2: If not found by (1), AND incoming customer has a RouterId (i.e., it's from a sync),
                    // try to "adopt" an existing DB record with the same AccountName that currently has RouterId IS NULL.
                    // This is crucial for linking your old records to the router.
                    if (!existingDbCustomerId.HasValue && customer.RouterId.HasValue)
                    {
                        // MODIFICATION HERE: SQL query now uses UPPER(TRIM(AccountName)) for robust matching.
                        // Parameter @AccountName will use the trimmedAccountName from C#.
                        SqlCommand checkCmd2 = new SqlCommand(
                            "SELECT Id FROM Customers WHERE UPPER(TRIM(AccountName)) = UPPER(@AccountName) AND RouterId IS NULL", conn);
                        checkCmd2.Parameters.AddWithValue("@AccountName", trimmedAccountName); // Use trimmed name
                        object result2 = checkCmd2.ExecuteScalar();
                        if (result2 != null && result2 != DBNull.Value)
                        {
                            existingDbCustomerId = Convert.ToInt32(result2);
                            isAdoptingRecord = true;
                            // MODIFICATION HERE: Log using trimmedAccountName
                            Console.WriteLine($"ADOPTING existing DB customer ID {existingDbCustomerId} (Account: '{trimmedAccountName}', old RouterId: NULL) for new RouterId: {customer.RouterId.Value}, MkID: '{customer.MikrotikSecretId}'");
                        }
                        else
                        {
                            // MODIFICATION HERE: Log if Priority 2 found no record
                            Console.WriteLine($"DEBUG: Priority 2 found NO existing record for AccountName: '{trimmedAccountName}' with RouterId IS NULL.");
                        }
                    }
                    else
                    {
                        // MODIFICATION HERE: Log why Priority 2 was skipped
                        if (existingDbCustomerId.HasValue)
                        {
                            Console.WriteLine($"DEBUG: Skipped Priority 2 because existingDbCustomerId already has a value: {existingDbCustomerId.Value}");
                        }
                        if (!customer.RouterId.HasValue)
                        {
                            Console.WriteLine($"DEBUG: Skipped Priority 2 because customer.RouterId.HasValue is FALSE. AccountName: '{trimmedAccountName}'");
                        }
                    }

                    // Priority 3: If still not found by (1) or (2), AND incoming customer has a RouterId,
                    // try to find by AccountName AND the same RouterId. (Handles cases where MkID might have been missing on an earlier sync).
                    if (!existingDbCustomerId.HasValue && customer.RouterId.HasValue)
                    {
                        // MODIFICATION HERE: SQL query now uses UPPER(TRIM(AccountName)) for robust matching.
                        // Parameter @AccountName will use the trimmedAccountName from C#.
                        SqlCommand checkCmd3 = new SqlCommand(
                           "SELECT Id FROM Customers WHERE UPPER(TRIM(AccountName)) = UPPER(@AccountName) AND RouterId = @RouterId", conn);
                        checkCmd3.Parameters.AddWithValue("@AccountName", trimmedAccountName); // Use trimmed name
                        checkCmd3.Parameters.AddWithValue("@RouterId", customer.RouterId.Value);
                        object result3 = checkCmd3.ExecuteScalar();
                        if (result3 != null && result3 != DBNull.Value)
                        {
                            existingDbCustomerId = Convert.ToInt32(result3);
                            // MODIFICATION HERE: Log using trimmedAccountName
                            Console.WriteLine($"Found existing customer by AccountName '{trimmedAccountName}' and RouterId {customer.RouterId.Value} (MkID may have been missing). DB ID: {existingDbCustomerId}");
                        }
                    }

                    if (existingDbCustomerId.HasValue)
                    {
                        // MODIFICATION HERE: Log using trimmedAccountName
                        Console.WriteLine($"UPDATING customer ID: {existingDbCustomerId.Value} (Account: '{trimmedAccountName}') with RouterID: {customer.RouterId}, MkID: {customer.MikrotikSecretId}, IP: {customer.IPAddress ?? "NULL"}, MAC: {customer.MacAddress ?? "NULL"}, Archived: {customer.IsArchived}");
                        cmd = new SqlCommand(@"
                    UPDATE Customers 
                    SET 
                        AccountName = @AccountName, 
                        AdditionalName = @AdditionalName, 
                        ContactNumber = @ContactNumber, 
                        Email = @Email, 
                        Location = @Location, 
                        IPAddress = @IPAddress,
                        IsArchived = @IsArchived,
                        RouterId = @RouterId,
                        MacAddress = @MacAddress,
                        MikrotikSecretId = @MikrotikSecretId
                    WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", existingDbCustomerId.Value);
                    }
                    else
                    {
                        // MODIFICATION HERE: Log using trimmedAccountName
                        Console.WriteLine($"INSERTING new customer: Account='{trimmedAccountName}', RouterId='{customer.RouterId?.ToString() ?? "NULL"}', MkID='{customer.MikrotikSecretId ?? "NULL"}', IP='{customer.IPAddress ?? "NULL"}', MAC='{customer.MacAddress ?? "NULL"}', Archived='{customer.IsArchived}'");
                        cmd = new SqlCommand(@"
                    INSERT INTO Customers 
                        (AccountName, AdditionalName, ContactNumber, Email, Location, IPAddress, IsArchived,
                         RouterId, MacAddress, MikrotikSecretId) 
                    VALUES 
                        (@AccountName, @AdditionalName, @ContactNumber, @Email, @Location, @IPAddress, @IsArchived,
                         @RouterId, @MacAddress, @MikrotikSecretId)", conn);
                    }

                    // MODIFICATION HERE: Use trimmedAccountName for the @AccountName parameter
                    cmd.Parameters.AddWithValue("@AccountName", trimmedAccountName);
                    cmd.Parameters.AddWithValue("@AdditionalName", (object)customer.AdditionalName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContactNumber", (object)customer.ContactNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)customer.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Location", (object)customer.Location ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@IPAddress", string.IsNullOrEmpty(customer.IPAddress) ? (object)DBNull.Value : customer.IPAddress);
                    cmd.Parameters.AddWithValue("@IsArchived", customer.IsArchived);

                    cmd.Parameters.AddWithValue("@RouterId", customer.RouterId.HasValue ? (object)customer.RouterId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@MacAddress", string.IsNullOrEmpty(customer.MacAddress) ? (object)DBNull.Value : customer.MacAddress);
                    cmd.Parameters.AddWithValue("@MikrotikSecretId", string.IsNullOrEmpty(customer.MikrotikSecretId) ? (object)DBNull.Value : customer.MikrotikSecretId);

                    cmd.ExecuteNonQuery();
                    conn.Close();
                    CustomerUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                // MODIFICATION HERE: Log using trimmedAccountName in exception message (if available)
                string accountNameToLog = customer?.AccountName ?? "[Unknown AccountName]";
                if (!string.IsNullOrEmpty(trimmedAccountName))
                {
                    accountNameToLog = trimmedAccountName;
                }
                Console.WriteLine($"CRITICAL Error in InsertOrUpdateCustomer for {accountNameToLog} (RouterId: {customer?.RouterId}, MkID: {customer?.MikrotikSecretId}). Details: {ex.ToString()}");
                throw new Exception($"Error in InsertOrUpdateCustomer for {accountNameToLog}: {ex.Message}", ex);
            }
        }
        public void ArchiveCustomerById(int customerId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Customers SET IsArchived = 1 WHERE Id = @CustomerId", conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.ExecuteNonQuery();
                conn.Close();
                CustomerUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        public void ArchiveCustomer(string accountName)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Customers SET IsArchived = 1 WHERE AccountName = @AccountName", conn);
                cmd.Parameters.AddWithValue("@AccountName", accountName);
                cmd.ExecuteNonQuery(); // This could affect multiple customers if AccountName is not unique across routers
                conn.Close();
                CustomerUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        public void MarkActiveCustomerById(int customerId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Customers SET IsArchived = 0 WHERE Id = @CustomerId", conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.ExecuteNonQuery();
                conn.Close();
                CustomerUpdated?.Invoke(this, EventArgs.Empty);
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
        public List<Customer> GetCustomers(bool includeArchived = true)
        {
            List<Customer> customers = new List<Customer>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT 
                        c.Id, c.AccountName, c.AdditionalName, c.ContactNumber, c.Email, 
                        c.Location, c.IsArchived, c.IPAddress,
                        c.RouterId, c.MacAddress, c.MikrotikSecretId,
                        r.RouterName 
                    FROM Customers c
                    LEFT JOIN Routers r ON c.RouterId = r.Id";

                if (!includeArchived)
                {
                    query += " WHERE c.IsArchived = 0";
                }
                query += " ORDER BY r.RouterName, c.AccountName;"; // Order by Router, then AccountName

                SqlCommand cmd = new SqlCommand(query, conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(MapToCustomer(reader));
                    }
                }
                conn.Close();
            }
            return customers;
        }
        public List<Customer> GetCustomersByTagIds(List<int> tagIds)
        {
            if (tagIds == null || tagIds.Count == 0)
                return new List<Customer>();

            List<Customer> customers = new List<Customer>();

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                var paramNames = new List<string>();
                for (int i = 0; i < tagIds.Count; i++)
                {
                    paramNames.Add("@TagId" + i);
                }
                string inClause = string.Join(", ", paramNames);

                string query = $@"
                    SELECT 
                        c.Id, c.AccountName, c.AdditionalName, c.ContactNumber, c.Email, 
                        c.Location, c.IsArchived, c.IPAddress,
                        c.RouterId, c.MacAddress, c.MikrotikSecretId,
                        r.RouterName 
                    FROM Customers c
                    LEFT JOIN Routers r ON c.RouterId = r.Id
                    JOIN CustomerTags ct ON c.Id = ct.CustomerId
                    WHERE ct.TagId IN ({inClause})
                    ORDER BY r.RouterName, c.AccountName;"; // Order by Router, then AccountName

                SqlCommand cmd = new SqlCommand(query, conn);

                for (int i = 0; i < tagIds.Count; i++)
                {
                    cmd.Parameters.AddWithValue(paramNames[i], tagIds[i]);
                }

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(MapToCustomer(reader));
                    }
                }
                conn.Close();
            }
            return customers;
        }
        private Customer MapToCustomer(SqlDataReader reader)
        {
            return new Customer
            {
                Id = Convert.ToInt32(reader["Id"]),
                AccountName = reader["AccountName"]?.ToString(),
                AdditionalName = reader["AdditionalName"]?.ToString(),
                ContactNumber = reader["ContactNumber"]?.ToString(),
                Email = reader["Email"]?.ToString(),
                Location = reader["Location"]?.ToString(),
                IsArchived = Convert.ToBoolean(reader["IsArchived"]),
                IPAddress = reader["IPAddress"]?.ToString(),

                // New fields
                RouterId = reader["RouterId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["RouterId"]),
                MacAddress = reader["MacAddress"]?.ToString(),
                MikrotikSecretId = reader["MikrotikSecretId"]?.ToString(),
                RouterName = reader["RouterName"]?.ToString() // Populated by JOIN
            };
        }

        public int ArchiveCustomersByRouterId(int routerId)
        {
            int rowsAffected = 0;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // This query archives customers linked to the router IF they are not already archived.
                // The FK constraint ON DELETE SET NULL will handle setting RouterId to NULL if the router row is deleted.
                string query = @"
                    UPDATE Customers 
                    SET IsArchived = 1
                    WHERE RouterId = @RouterId AND IsArchived = 0;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RouterId", routerId);
                    rowsAffected = cmd.ExecuteNonQuery();
                }
                conn.Close();
                if (rowsAffected > 0)
                {
                    CustomerUpdated?.Invoke(this, EventArgs.Empty); // Notify if changes were made
                }
            }
            Console.WriteLine($"Archived {rowsAffected} customers for RouterId: {routerId}"); // For logging/debugging
            return rowsAffected;
        }

        // --- NEW METHOD (Optional but Recommended): GetCustomerById ---
        // Useful for editing or viewing a specific customer.
        public Customer GetCustomerById(int customerId)
        {
            Customer customer = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT 
                        c.Id, c.AccountName, c.AdditionalName, c.ContactNumber, c.Email, 
                        c.Location, c.IsArchived, c.IPAddress,
                        c.RouterId, c.MacAddress, c.MikrotikSecretId,
                        r.RouterName 
                    FROM Customers c
                    LEFT JOIN Routers r ON c.RouterId = r.Id
                    WHERE c.Id = @CustomerId;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        customer = MapToCustomer(reader);
                    }
                }
                conn.Close();
            }
            return customer;
        }
    }

}

