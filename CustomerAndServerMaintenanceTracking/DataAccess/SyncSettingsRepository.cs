using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CustomerAndServerMaintenanceTracking.DataAccess; // assuming you have DatabaseHelper

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class SyncSettingsRepository
    {
        private DatabaseHelper dbHelper;

        public SyncSettingsRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        // Retrieves a sync interval by name.
        public int GetInterval(string settingName)
        {
            int interval = 0;
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Interval FROM SyncSettings WHERE SettingName = @SettingName", conn);
                cmd.Parameters.AddWithValue("@SettingName", settingName);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    interval = Convert.ToInt32(result);
                }
                conn.Close();
            }
            return interval;
        }

        // Inserts or updates the sync interval.
        public void SaveInterval(string settingName, int interval)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Check if the setting exists
                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM SyncSettings WHERE SettingName = @SettingName", conn);
                checkCmd.Parameters.AddWithValue("@SettingName", settingName);
                int count = (int)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    // Update the existing row.
                    SqlCommand updateCmd = new SqlCommand("UPDATE SyncSettings SET Interval = @Interval WHERE SettingName = @SettingName", conn);
                    updateCmd.Parameters.AddWithValue("@Interval", interval);
                    updateCmd.Parameters.AddWithValue("@SettingName", settingName);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    // Insert a new row.
                    SqlCommand insertCmd = new SqlCommand("INSERT INTO SyncSettings (SettingName, Interval) VALUES (@SettingName, @Interval)", conn);
                    insertCmd.Parameters.AddWithValue("@SettingName", settingName);
                    insertCmd.Parameters.AddWithValue("@Interval", interval);
                    insertCmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
    }
}
