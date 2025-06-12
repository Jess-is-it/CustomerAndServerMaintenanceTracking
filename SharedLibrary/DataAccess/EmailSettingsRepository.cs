using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.DataAccess
{
    public class EmailSettingsRepository
    {
        private readonly DatabaseHelper _dbHelper;

        public EmailSettingsRepository()
        {
            _dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Retrieves all email settings from the database.
        /// </summary>
        public List<EmailSettings> GetAllEmailSettings()
        {
            var settingsList = new List<EmailSettings>();
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM EmailSettings ORDER BY SettingName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            settingsList.Add(MapToEmailSettings(reader));
                        }
                    }
                }
            }
            return settingsList;
        }

        /// <summary>
        /// Retrieves a single email setting by its ID.
        /// </summary>
        public EmailSettings GetEmailSettingById(int id)
        {
            EmailSettings setting = null;
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM EmailSettings WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            setting = MapToEmailSettings(reader);
                        }
                    }
                }
            }
            return setting;
        }


        /// <summary>
        /// Adds a new email settings record to the database.
        /// </summary>
        /// <param name="settings">The EmailSettings object to add.</param>
        /// <returns>The ID of the newly created record, or 0 if it fails.</returns>
        public int AddEmailSetting(EmailSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO EmailSettings 
                        (SettingName, SmtpServer, SmtpPort, EnableSsl, SenderEmail, SenderDisplayName, SmtpUsername, SmtpPassword, IsDefault)
                    OUTPUT INSERTED.Id
                    VALUES 
                        (@SettingName, @SmtpServer, @SmtpPort, @EnableSsl, @SenderEmail, @SenderDisplayName, @SmtpUsername, @SmtpPassword, @IsDefault)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SettingName", settings.SettingName);
                    cmd.Parameters.AddWithValue("@SmtpServer", settings.SmtpServer);
                    cmd.Parameters.AddWithValue("@SmtpPort", settings.SmtpPort);
                    cmd.Parameters.AddWithValue("@EnableSsl", settings.EnableSsl);
                    cmd.Parameters.AddWithValue("@SenderEmail", settings.SenderEmail);
                    cmd.Parameters.AddWithValue("@SenderDisplayName", (object)settings.SenderDisplayName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmtpUsername", (object)settings.SmtpUsername ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmtpPassword", (object)settings.SmtpPassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsDefault", settings.IsDefault);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Updates an existing email settings record in the database.
        /// </summary>
        public bool UpdateEmailSetting(EmailSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE EmailSettings SET
                        SettingName = @SettingName,
                        SmtpServer = @SmtpServer,
                        SmtpPort = @SmtpPort,
                        EnableSsl = @EnableSsl,
                        SenderEmail = @SenderEmail,
                        SenderDisplayName = @SenderDisplayName,
                        SmtpUsername = @SmtpUsername,
                        SmtpPassword = @SmtpPassword,
                        IsDefault = @IsDefault
                    WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", settings.Id);
                    cmd.Parameters.AddWithValue("@SettingName", settings.SettingName);
                    cmd.Parameters.AddWithValue("@SmtpServer", settings.SmtpServer);
                    cmd.Parameters.AddWithValue("@SmtpPort", settings.SmtpPort);
                    cmd.Parameters.AddWithValue("@EnableSsl", settings.EnableSsl);
                    cmd.Parameters.AddWithValue("@SenderEmail", settings.SenderEmail);
                    cmd.Parameters.AddWithValue("@SenderDisplayName", (object)settings.SenderDisplayName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmtpUsername", (object)settings.SmtpUsername ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmtpPassword", (object)settings.SmtpPassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsDefault", settings.IsDefault);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// Deletes an email setting from the database.
        /// </summary>
        public bool DeleteEmailSetting(int id)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM EmailSettings WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// Sets a specific email setting as the default, and unsets all others.
        /// </summary>
        public void SetDefaultEmailSetting(int id)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // First, unset all other records from being the default
                    string unsetQuery = "UPDATE EmailSettings SET IsDefault = 0 WHERE IsDefault = 1";
                    using (SqlCommand unsetCmd = new SqlCommand(unsetQuery, conn, transaction))
                    {
                        unsetCmd.ExecuteNonQuery();
                    }

                    // Second, set the new record as the default
                    string setQuery = "UPDATE EmailSettings SET IsDefault = 1 WHERE Id = @Id";
                    using (SqlCommand setCmd = new SqlCommand(setQuery, conn, transaction))
                    {
                        setCmd.Parameters.AddWithValue("@Id", id);
                        setCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw; // Re-throw the exception to be handled by the caller
                }
            }
        }

        /// <summary>
        /// Helper method to map a SqlDataReader record to an EmailSettings object.
        /// </summary>
        private EmailSettings MapToEmailSettings(SqlDataReader reader)
        {
            return new EmailSettings
            {
                Id = Convert.ToInt32(reader["Id"]),
                SettingName = reader["SettingName"].ToString(),
                SmtpServer = reader["SmtpServer"].ToString(),
                SmtpPort = Convert.ToInt32(reader["SmtpPort"]),
                EnableSsl = Convert.ToBoolean(reader["EnableSsl"]),
                SenderEmail = reader["SenderEmail"].ToString(),
                SenderDisplayName = reader["SenderDisplayName"] == DBNull.Value ? null : reader["SenderDisplayName"].ToString(),
                SmtpUsername = reader["SmtpUsername"] == DBNull.Value ? null : reader["SmtpUsername"].ToString(),
                SmtpPassword = reader["SmtpPassword"] == DBNull.Value ? null : reader["SmtpPassword"].ToString(),
                IsDefault = Convert.ToBoolean(reader["IsDefault"])
            };
        }
    }
}
