using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class UserRoleRepository
    {
        private readonly DatabaseHelper dbHelper;

        public UserRoleRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Adds a new role and its associated permissions to the database.
        /// </summary>
        /// <param name="roleName">The name of the new role.</param>
        /// <param name="description">The description of the role.</param>
        /// <param name="permissionKeys">A list of permission keys to assign to this role.</param>
        /// <returns>The ID of the newly created role, or -1 if an error occurred.</returns>
        public int AddRoleWithPermissions(string roleName, string description, List<string> permissionKeys)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                // Or throw new ArgumentException("Role name cannot be empty.", nameof(roleName));
                // For consistency with how AddOrGetMunicipality returns -1 for invalid input:
                Console.WriteLine("AddRoleWithPermissions Error: Role name cannot be empty.");
                return -1; // Indicate invalid input
            }

            int newRoleId = -1;

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // *** NEW: Check for existing role name first (case-insensitive) ***
                string checkQuery = "SELECT Id FROM UserRoles WHERE UPPER(RoleName) = UPPER(@RoleName)";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@RoleName", roleName.Trim());
                    object existingId = checkCmd.ExecuteScalar();
                    if (existingId != null && existingId != DBNull.Value)
                    {
                        Console.WriteLine($"AddRoleWithPermissions Info: Role name '{roleName}' already exists with ID {existingId}.");
                        return 0; // Indicate that the role name already exists
                    }
                }
                // *** END OF NEW CHECK ***

                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // Insert the role
                    string insertRoleQuery = "INSERT INTO UserRoles (RoleName, Description, DateCreated) OUTPUT INSERTED.Id VALUES (@RoleName, @Description, @DateCreated)";
                    using (SqlCommand cmdRole = new SqlCommand(insertRoleQuery, conn, transaction))
                    {
                        cmdRole.Parameters.AddWithValue("@RoleName", roleName.Trim()); // Use trimmed name
                        cmdRole.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
                        cmdRole.Parameters.AddWithValue("@DateCreated", DateTime.Now);
                        newRoleId = (int)cmdRole.ExecuteScalar();
                    }

                    if (newRoleId > 0 && permissionKeys != null && permissionKeys.Any())
                    {
                        string insertPermissionQuery = "INSERT INTO RolePermissions (RoleId, PermissionKey) VALUES (@RoleId, @PermissionKey)";
                        foreach (string pKey in permissionKeys)
                        {
                            if (string.IsNullOrWhiteSpace(pKey)) continue;

                            using (SqlCommand cmdPerm = new SqlCommand(insertPermissionQuery, conn, transaction))
                            {
                                cmdPerm.Parameters.AddWithValue("@RoleId", newRoleId);
                                cmdPerm.Parameters.AddWithValue("@PermissionKey", pKey);
                                cmdPerm.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"SQL Error adding role '{roleName}': {ex.Message}");
                    newRoleId = -1; // Indicate general error
                                    // Optionally, re-throw or handle more specifically if needed
                                    // throw; 
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"General Error adding role '{roleName}': {ex.Message}");
                    newRoleId = -1; // Indicate general error
                                    // throw;
                }
            }
            return newRoleId;
        }

        // Add other methods later: GetRoles, GetRoleByIdWithPermissions, UpdateRoleWithPermissions, DeleteRole etc.
        public List<UserRole> GetRoles()
        {
            List<UserRole> roles = new List<UserRole>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, RoleName, Description, DateCreated FROM UserRoles ORDER BY RoleName", conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new UserRole
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            RoleName = reader["RoleName"].ToString(),
                            Description = reader["Description"] == DBNull.Value ? string.Empty : reader["Description"].ToString(),
                            DateCreated = Convert.ToDateTime(reader["DateCreated"])
                        });
                    }
                }
            }
            return roles;
        }

        // Add these methods inside the UserRoleRepository class

        /// <summary>
        /// Retrieves a single role by its ID, including its assigned permission keys.
        /// </summary>
        public UserRole GetRoleByIdWithPermissions(int roleId)
        {
            UserRole role = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Get Role Details
                SqlCommand cmdRole = new SqlCommand("SELECT Id, RoleName, Description, DateCreated FROM UserRoles WHERE Id = @RoleId", conn);
                cmdRole.Parameters.AddWithValue("@RoleId", roleId);
                using (SqlDataReader reader = cmdRole.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        role = new UserRole
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            RoleName = reader["RoleName"].ToString(),
                            Description = reader["Description"] == DBNull.Value ? string.Empty : reader["Description"].ToString(),
                            DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                            PermissionKeys = new List<string>() // Initialize the list
                        };
                    }
                }

                // If role found, get its permissions
                if (role != null)
                {
                    SqlCommand cmdPerms = new SqlCommand("SELECT PermissionKey FROM RolePermissions WHERE RoleId = @RoleId", conn);
                    cmdPerms.Parameters.AddWithValue("@RoleId", roleId);
                    using (SqlDataReader reader = cmdPerms.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            role.PermissionKeys.Add(reader["PermissionKey"].ToString());
                        }
                    }
                }
            }
            return role;
        }

        /// <summary>
        /// Updates an existing role and its associated permissions.
        /// </summary>
        public bool UpdateRoleWithPermissions(UserRole roleToUpdate, List<string> newPermissionKeys)
        {
            if (roleToUpdate == null || string.IsNullOrWhiteSpace(roleToUpdate.RoleName))
            {
                throw new ArgumentException("Role object or RoleName cannot be null/empty for update.");
            }

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // Update Role Name and Description
                    string updateRoleQuery = "UPDATE UserRoles SET RoleName = @RoleName, Description = @Description WHERE Id = @RoleId";
                    using (SqlCommand cmdUpdateRole = new SqlCommand(updateRoleQuery, conn, transaction))
                    {
                        cmdUpdateRole.Parameters.AddWithValue("@RoleName", roleToUpdate.RoleName);
                        cmdUpdateRole.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(roleToUpdate.Description) ? (object)DBNull.Value : roleToUpdate.Description);
                        cmdUpdateRole.Parameters.AddWithValue("@RoleId", roleToUpdate.Id);
                        cmdUpdateRole.ExecuteNonQuery();
                    }

                    // Delete existing permissions for this role
                    string deletePermsQuery = "DELETE FROM RolePermissions WHERE RoleId = @RoleId";
                    using (SqlCommand cmdDeletePerms = new SqlCommand(deletePermsQuery, conn, transaction))
                    {
                        cmdDeletePerms.Parameters.AddWithValue("@RoleId", roleToUpdate.Id);
                        cmdDeletePerms.ExecuteNonQuery();
                    }

                    // Add new permissions
                    if (newPermissionKeys != null && newPermissionKeys.Any())
                    {
                        string insertPermissionQuery = "INSERT INTO RolePermissions (RoleId, PermissionKey) VALUES (@RoleId, @PermissionKey)";
                        foreach (string pKey in newPermissionKeys)
                        {
                            if (string.IsNullOrWhiteSpace(pKey)) continue;
                            using (SqlCommand cmdInsertPerm = new SqlCommand(insertPermissionQuery, conn, transaction))
                            {
                                cmdInsertPerm.Parameters.AddWithValue("@RoleId", roleToUpdate.Id);
                                cmdInsertPerm.Parameters.AddWithValue("@PermissionKey", pKey);
                                cmdInsertPerm.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                    return true;
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"SQL Error updating role '{roleToUpdate.RoleName}': {ex.Message}");
                    // Consider logging this with your ServiceLogRepository if this lib has access or a logging mechanism
                    return false;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"General Error updating role '{roleToUpdate.RoleName}': {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Deletes a role from the database.
        /// Associated permissions are deleted by ON DELETE CASCADE.
        /// Throws an exception if the role is still assigned to users (due to FK constraint).
        /// </summary>
        public bool DeleteRole(int roleId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // The ON DELETE CASCADE on RolePermissions table will handle deleting associated permissions.
                // The FK constraint on UserAccounts table (without ON DELETE CASCADE/SET NULL)
                // will prevent deletion if users are assigned to this role.
                string query = "DELETE FROM UserRoles WHERE Id = @RoleId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                    catch (SqlException ex)
                    {
                        // Catch FK violation if role is in use
                        if (ex.Number == 547) // Foreign key constraint violation
                        {
                            Console.WriteLine($"Cannot delete role ID {roleId} as it is currently assigned to one or more users.");
                            throw new InvalidOperationException($"Cannot delete role as it is currently assigned to one or more users. Please reassign users before deleting this role.", ex);
                        }
                        Console.WriteLine($"SQL Error deleting role ID {roleId}: {ex.Message}");
                        throw; // Re-throw other SQL exceptions
                    }
                }
            }
        }
    }
}
