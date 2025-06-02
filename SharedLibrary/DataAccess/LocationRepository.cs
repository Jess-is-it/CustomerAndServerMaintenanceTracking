using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class LocationRepository
    {
        private readonly DatabaseHelper dbHelper;

        public LocationRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Adds a new Municipality to the database and returns its ID.
        /// Checks if a municipality with the same name already exists.
        /// </summary>
        /// <param name="municipalityName">The name of the municipality to add.</param>
        /// <returns>The ID of the newly added or existing municipality. Returns -1 if input is invalid or an error occurs.</returns>
        public int AddOrGetMunicipality(string municipalityName)
        {
            if (string.IsNullOrWhiteSpace(municipalityName))
            {
                // Optionally log this error
                Console.WriteLine("AddOrGetMunicipality Error: Municipality name cannot be null or empty.");
                return -1; // Indicate error or invalid input
            }

            int municipalityId = -1;

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();

                // Check if municipality already exists
                string checkQuery = "SELECT Id FROM Municipalities WHERE Name = @Name";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Name", municipalityName.Trim());
                    object result = checkCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        municipalityId = Convert.ToInt32(result);
                    }
                }

                // If not exists, add it
                if (municipalityId == -1)
                {
                    string insertQuery = "INSERT INTO Municipalities (Name) OUTPUT INSERTED.Id VALUES (@Name)";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Name", municipalityName.Trim());
                        object result = insertCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            municipalityId = Convert.ToInt32(result);
                        }
                        else
                        {
                            // Optionally log this error
                            Console.WriteLine($"AddOrGetMunicipality Error: Failed to retrieve ID for new municipality '{municipalityName}'.");
                            return -1; // Indicate insertion failure
                        }
                    }
                }
            }
            return municipalityId;
        }

        /// <summary>
        /// Adds a list of barangays for a given municipality ID.
        /// Only adds barangays that do not already exist for that municipality.
        /// </summary>
        /// <param name="municipalityId">The ID of the municipality.</param>
        /// <param name="barangayNames">A list of barangay names to add.</param>
        /// <returns>The number of new barangays successfully added.</returns>
        public int AddBarangays(int municipalityId, List<string> barangayNames)
        {
            if (municipalityId <= 0 || barangayNames == null || !barangayNames.Any())
            {
                return 0;
            }

            int barangaysAddedCount = 0;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                foreach (string rawBarangayName in barangayNames)
                {
                    string barangayName = rawBarangayName.Trim();
                    if (string.IsNullOrWhiteSpace(barangayName))
                    {
                        continue; // Skip empty names
                    }

                    // Check if this barangay already exists for this municipality
                    string checkQuery = "SELECT COUNT(*) FROM Barangays WHERE MunicipalityId = @MunicipalityId AND Name = @Name";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@MunicipalityId", municipalityId);
                        checkCmd.Parameters.AddWithValue("@Name", barangayName);
                        int existingCount = (int)checkCmd.ExecuteScalar();

                        if (existingCount == 0)
                        {
                            // Barangay does not exist, so add it
                            string insertQuery = "INSERT INTO Barangays (MunicipalityId, Name) VALUES (@MunicipalityId, @Name)";
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@MunicipalityId", municipalityId);
                                insertCmd.Parameters.AddWithValue("@Name", barangayName);
                                insertCmd.ExecuteNonQuery();
                                barangaysAddedCount++;
                            }
                        }
                    }
                }
            }
            return barangaysAddedCount;
        }

        /// <summary>
        /// Retrieves all municipalities, optionally including their barangays.
        /// </summary>
        /// <param name="includeBarangays">Whether to load associated barangays.</param>
        /// <returns>A list of municipalities.</returns>
        public List<Municipality> GetMunicipalities(bool includeBarangays = false)
        {
            List<Municipality> municipalities = new List<Municipality>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT Id, Name, DateAdded FROM Municipalities ORDER BY Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            municipalities.Add(new Municipality
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                DateAdded = Convert.ToDateTime(reader["DateAdded"])
                            });
                        }
                    }
                }

                if (includeBarangays)
                {
                    foreach (var municipality in municipalities)
                    {
                        municipality.Barangays = GetBarangaysByMunicipalityId(municipality.Id, conn); // Pass open connection
                    }
                }
            }
            return municipalities;
        }

        /// <summary>
        /// Retrieves all barangays for a specific municipality ID.
        /// Can use an existing open connection or create a new one.
        /// </summary>
        /// <param name="municipalityId">The ID of the municipality.</param>
        /// <param name="existingConnection">Optional existing open SQL connection.</param>
        /// <returns>A list of barangays.</returns>
        public List<Barangay> GetBarangaysByMunicipalityId(int municipalityId, SqlConnection existingConnection = null)
        {
            List<Barangay> barangays = new List<Barangay>();
            SqlConnection conn = existingConnection ?? dbHelper.GetConnection();
            bool closeConnectionAfter = existingConnection == null; // Only close if we opened it

            try
            {
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }
                string query = "SELECT Id, Name, DateAdded FROM Barangays WHERE MunicipalityId = @MunicipalityId ORDER BY Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MunicipalityId", municipalityId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            barangays.Add(new Barangay
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                MunicipalityId = municipalityId,
                                Name = reader["Name"].ToString(),
                                DateAdded = Convert.ToDateTime(reader["DateAdded"])
                            });
                        }
                    }
                }
            }
            finally
            {
                if (closeConnectionAfter && conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            return barangays;
        }

        /// <summary>
        /// Updates an existing Municipality's name in the database.
        /// </summary>
        public void UpdateMunicipality(Municipality municipality)
        {
            if (municipality == null || string.IsNullOrWhiteSpace(municipality.Name))
            {
                Console.WriteLine("UpdateMunicipality Error: Municipality object or name is null/empty.");
                return; // Or throw ArgumentNullException
            }

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Consider checking if another municipality (not this one) already has the new name to prevent duplicates
                // For simplicity, this direct update assumes unique name constraint on DB will handle or you manage it at UI level
                string query = "UPDATE Municipalities SET Name = @Name WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", municipality.Name.Trim());
                    cmd.Parameters.AddWithValue("@Id", municipality.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes all barangays associated with a specific municipality ID.
        /// </summary>
        public void DeleteBarangaysByMunicipalityId(int municipalityId)
        {
            if (municipalityId <= 0) return;

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM Barangays WHERE MunicipalityId = @MunicipalityId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MunicipalityId", municipalityId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a municipality and its associated barangays (due to ON DELETE CASCADE).
        /// </summary>
        public void DeleteMunicipality(int municipalityId)
        {
            if (municipalityId <= 0) return;

            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // The ON DELETE CASCADE constraint on FK_Barangays_Municipalities
                // in your database schema will handle deleting associated barangays.
                string query = "DELETE FROM Municipalities WHERE Id = @MunicipalityId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MunicipalityId", municipalityId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Retrieves a single municipality by its ID, including its barangays.
        /// </summary>
        public Municipality GetMunicipalityById(int municipalityId)
        {
            Municipality municipality = null;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT Id, Name, DateAdded FROM Municipalities WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", municipalityId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            municipality = new Municipality
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                DateAdded = Convert.ToDateTime(reader["DateAdded"])
                            };
                        }
                    }
                }

                if (municipality != null)
                {
                    municipality.Barangays = GetBarangaysByMunicipalityId(municipality.Id, conn); // Pass open connection
                }
            }
            return municipality;
        }
    }
}
