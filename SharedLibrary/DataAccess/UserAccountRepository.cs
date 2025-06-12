using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class UserAccountRepository
    {
        private readonly DatabaseHelper dbHelper;

        public UserAccountRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }
            string hashOfEnteredPassword = HashPassword(enteredPassword);
            return StringComparer.OrdinalIgnoreCase.Compare(hashOfEnteredPassword, storedHash) == 0;
        }

        /// <summary>
        /// Adds a new user account to the database.
        /// </summary>
        /// <param name="user">The UserAccount object (PasswordHash property will be ignored here).</param>
        /// <param name="plainPassword">The plain text password to be hashed and stored.</param>
        /// <returns>The ID of the newly created user, or -1 if username exists, or throws exception on other errors.</returns>
        public int AddUserAccount(UserAccount user, string plainPassword)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(plainPassword))
            {
                throw new ArgumentException("User object, username, or plain password cannot be null or empty.");
            }

            if (UsernameExists(user.Username))
            {
                return -1; // Indicate username exists
            }

            string hashedPassword = HashPassword(plainPassword);
            int newUserId = 0;

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO UserAccounts (FullName, Username, PasswordHash, Email, PhoneNumber, RoleId, IsActive, DateCreated, LastLoginDate)
                    OUTPUT INSERTED.Id
                    VALUES (@FullName, @Username, @PasswordHash, @Email, @PhoneNumber, @RoleId, @IsActive, @DateCreated, @LastLoginDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(user.FullName) ? (object)DBNull.Value : user.FullName);
                    cmd.Parameters.AddWithValue("@Username", user.Username.Trim());
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword); // Use the generated hash
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(user.Email) ? (object)DBNull.Value : user.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrWhiteSpace(user.PhoneNumber) ? (object)DBNull.Value : user.PhoneNumber);
                    cmd.Parameters.AddWithValue("@RoleId", user.RoleId);
                    cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                    cmd.Parameters.AddWithValue("@DateCreated", user.DateCreated);
                    cmd.Parameters.AddWithValue("@LastLoginDate", user.LastLoginDate.HasValue ? (object)user.LastLoginDate.Value : DBNull.Value);

                    newUserId = (int)cmd.ExecuteScalar();
                }
            }
            return newUserId;
        }

        public bool UsernameExists(string username, int currentUserIdToExclude = 0)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM UserAccounts WHERE UPPER(Username) = UPPER(@Username)";
                if (currentUserIdToExclude > 0)
                {
                    query += " AND Id != @CurrentUserId";
                }
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username.Trim());
                    if (currentUserIdToExclude > 0)
                    {
                        cmd.Parameters.AddWithValue("@CurrentUserId", currentUserIdToExclude);
                    }
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public List<UserAccount> GetUserAccountsWithRoles()
        {
            List<UserAccount> users = new List<UserAccount>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT ua.Id, ua.FullName, ua.Username, ua.Email, ua.PhoneNumber, 
                   ua.RoleId, ua.IsActive, ua.DateCreated, ua.LastLoginDate,
                   ua.DeactivationReason, -- *** ADDED THIS ***
                   ur.RoleName
            FROM UserAccounts ua
            LEFT JOIN UserRoles ur ON ua.RoleId = ur.Id 
            ORDER BY ua.IsActive DESC, ua.FullName, ua.Username";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UserAccount user = new UserAccount
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                FullName = reader["FullName"] == DBNull.Value ? string.Empty : reader["FullName"].ToString(),
                                Username = reader["Username"].ToString(),
                                Email = reader["Email"] == DBNull.Value ? string.Empty : reader["Email"].ToString(),
                                PhoneNumber = reader["PhoneNumber"] == DBNull.Value ? string.Empty : reader["PhoneNumber"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                LastLoginDate = reader["LastLoginDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastLoginDate"]),
                                DeactivationReason = reader["DeactivationReason"] == DBNull.Value ? null : reader["DeactivationReason"].ToString(), // *** ADDED THIS ***
                                Role = reader["RoleName"] == DBNull.Value ?
                                       new UserRole { Id = Convert.ToInt32(reader["RoleId"]), RoleName = "N/A (Role Missing)" } :
                                       new UserRole { Id = Convert.ToInt32(reader["RoleId"]), RoleName = reader["RoleName"].ToString() }
                            };
                            users.Add(user);
                        }
                    }
                }
            }
            return users;
        }

        public UserAccount GetUserAccountById(int userId)
        {
            UserAccount user = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT ua.Id, ua.FullName, ua.Username, ua.PasswordHash, ua.Email, ua.PhoneNumber, 
                   ua.RoleId, ua.IsActive, ua.DateCreated, ua.LastLoginDate,
                   ua.DeactivationReason, -- *** ADDED THIS ***
                   ur.RoleName
            FROM UserAccounts ua
            LEFT JOIN UserRoles ur ON ua.RoleId = ur.Id
            WHERE ua.Id = @UserId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new UserAccount
                            {
                                // ... (other properties as before) ...
                                Id = Convert.ToInt32(reader["Id"]),
                                FullName = reader["FullName"] == DBNull.Value ? string.Empty : reader["FullName"].ToString(),
                                Username = reader["Username"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                Email = reader["Email"] == DBNull.Value ? string.Empty : reader["Email"].ToString(),
                                PhoneNumber = reader["PhoneNumber"] == DBNull.Value ? string.Empty : reader["PhoneNumber"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                LastLoginDate = reader["LastLoginDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastLoginDate"]),
                                DeactivationReason = reader["DeactivationReason"] == DBNull.Value ? null : reader["DeactivationReason"].ToString(), // *** ADDED THIS ***
                                Role = reader["RoleName"] == DBNull.Value ? null : new UserRole { Id = Convert.ToInt32(reader["RoleId"]), RoleName = reader["RoleName"].ToString() }
                            };
                        }
                    }
                }
            }
            return user;
        }

        public bool UpdateUserAccount(UserAccount user, string newPlainPassword = null)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username))
            {
                throw new ArgumentException("User object or username cannot be null or empty.");
            }

            if (UsernameExists(user.Username, user.Id))
            {
                return false;
            }

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // *** MODIFIED QUERY TO INCLUDE DeactivationReason ***
                string query = @"
            UPDATE UserAccounts SET 
                FullName = @FullName, 
                Username = @Username, 
                Email = @Email, 
                PhoneNumber = @PhoneNumber, 
                RoleId = @RoleId, 
                IsActive = @IsActive,
                DeactivationReason = @DeactivationReason"; // *** ADDED THIS ***

                if (!string.IsNullOrWhiteSpace(newPlainPassword))
                {
                    query += ", PasswordHash = @PasswordHash";
                }
                query += " WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(user.FullName) ? (object)DBNull.Value : user.FullName);
                    cmd.Parameters.AddWithValue("@Username", user.Username.Trim());
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(user.Email) ? (object)DBNull.Value : user.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrWhiteSpace(user.PhoneNumber) ? (object)DBNull.Value : user.PhoneNumber);
                    cmd.Parameters.AddWithValue("@RoleId", user.RoleId);
                    cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                    cmd.Parameters.AddWithValue("@Id", user.Id);

                    // *** SET DeactivationReason PARAMETER ***
                    // If user is being activated, clear the reason. Otherwise, save the provided reason.
                    if (user.IsActive)
                    {
                        cmd.Parameters.AddWithValue("@DeactivationReason", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@DeactivationReason",
                            string.IsNullOrWhiteSpace(user.DeactivationReason) ? (object)DBNull.Value : user.DeactivationReason);
                    }

                    if (!string.IsNullOrWhiteSpace(newPlainPassword))
                    {
                        cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(newPlainPassword));
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool DeleteUserAccount(int userId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM UserAccounts WHERE Id = @UserId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public IEnumerable<UserAccount> GetUsersByRoleId(int roleId)
        {
            // We can reuse the existing method for simplicity
            return GetUserAccountsWithRoles().Where(u => u.RoleId == roleId && u.IsActive);
        }
    }
}
