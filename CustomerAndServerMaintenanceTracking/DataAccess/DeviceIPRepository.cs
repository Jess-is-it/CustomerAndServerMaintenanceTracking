using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.DataAccess
{
    public class DeviceIPRepository
    {
        private DatabaseHelper dbHelper;

        public DeviceIPRepository()
        {
            dbHelper = new DatabaseHelper();
        }

        public List<DeviceIP> GetAllDevices()
        {
            List<DeviceIP> devices = new List<DeviceIP>();
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT Id, DeviceName, IPAddress, Location FROM DeviceIPs";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DeviceIP device = new DeviceIP
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                DeviceName = reader["DeviceName"].ToString(),
                                IPAddress = reader["IPAddress"] == DBNull.Value
                                    ? string.Empty
                                    : reader["IPAddress"].ToString(),
                                Location = reader["Location"] == DBNull.Value
                                    ? string.Empty
                                    : reader["Location"].ToString()
                            };
                            devices.Add(device);
                        }
                    }
                }
            }
            return devices;
        }

        public void AddDevice(DeviceIP device)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                // Insert TagType as well
                string sql = "INSERT INTO DeviceIPs (DeviceName, IPAddress, Location) VALUES (@DeviceName, @IPAddress, @Location)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DeviceName", device.DeviceName);
                    cmd.Parameters.AddWithValue("@IPAddress", string.IsNullOrEmpty(device.IPAddress) ? (object)DBNull.Value : device.IPAddress);
                    cmd.Parameters.AddWithValue("@Location", string.IsNullOrEmpty(device.Location) ? (object)DBNull.Value : device.Location);
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }


        public void UpdateDevice(DeviceIP device)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE DeviceIPs SET DeviceName=@DeviceName, IPAddress=@IPAddress, Location=@Location WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DeviceName", device.DeviceName);
                    cmd.Parameters.AddWithValue("@IPAddress", string.IsNullOrEmpty(device.IPAddress) ? (object)DBNull.Value : device.IPAddress);
                    cmd.Parameters.AddWithValue("@Location", string.IsNullOrEmpty(device.Location) ? (object)DBNull.Value : device.Location);
                    cmd.Parameters.AddWithValue("@Id", device.Id);
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
        public void DeleteDevice(int deviceId)
        {
            using (SqlConnection conn = dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM DeviceIPs WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deviceId);
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
    }
}
