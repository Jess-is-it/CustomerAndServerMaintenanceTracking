using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class AssignDeviceIP : Form
    {
        private TagClass currentTag;
        public event EventHandler DeviceIPAssigned; // Event to notify parent form

        // Helper class for DataGridView binding
        private class SelectableDeviceIP
        {
            public DeviceIP Device { get; set; }
            public bool IsSelected { get; set; }

            // Expose DeviceIP properties for DataGridView binding
            public int Id => Device.Id;
            public string DeviceName => Device.DeviceName;
            public string IPAddress => Device.IPAddress;
            public string Location => Device.Location;
        }


        public AssignDeviceIP(TagClass tag)
        {
            InitializeComponent();
            currentTag = tag;

            // Update the form title to be specific
            this.label1.Text = $"Assign Tag: {currentTag.TagName}"; // Use the Label control from your designer

            // Setup the DataGridView columns specifically for DeviceIP
            InitializeDeviceIPGrid();

            // Load Device IPs, not customers
            LoadDeviceIPs();

            // Wire up search event (make sure txtSearchCustomers is renamed or use the correct name)
            txtSearchCustomers.TextChanged += TxtSearch_TextChanged; // Use the correct TextBox name
        }

        // Method to set up DataGridView columns for DeviceIP data
        private void InitializeDeviceIPGrid()
        {
            // Ensure the correct DataGridView is referenced (likely named dataGridViewDeviceIPs in your designer)
            dataGridViewDeviceIPs.Columns.Clear();
            dataGridViewDeviceIPs.AutoGenerateColumns = false;

            // 1) Select CheckBox Column
            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "SelectDeviceIP", // Unique name for this column
                HeaderText = "Select",
                DataPropertyName = "IsSelected", // Binds to the IsSelected property of SelectableDeviceIP
                Width = 50
            };
            dataGridViewDeviceIPs.Columns.Add(colSelect);

            // 2) ID Column (optional, can be hidden)
            var colId = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                DataPropertyName = "Id", // Binds to SelectableDeviceIP.Id
                Visible = false // Usually hidden
            };
            dataGridViewDeviceIPs.Columns.Add(colId);

            // 3) Device Name Column
            var colDeviceName = new DataGridViewTextBoxColumn
            {
                Name = "DeviceName",
                HeaderText = "Device Name",
                DataPropertyName = "DeviceName", // Binds to SelectableDeviceIP.DeviceName
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewDeviceIPs.Columns.Add(colDeviceName);

            // 4) IP Address Column
            var colIPAddress = new DataGridViewTextBoxColumn
            {
                Name = "IPAddress",
                HeaderText = "IP Address",
                DataPropertyName = "IPAddress", // Binds to SelectableDeviceIP.IPAddress
                Width = 150
            };
            dataGridViewDeviceIPs.Columns.Add(colIPAddress);

            // 5) Location Column
            var colLocation = new DataGridViewTextBoxColumn
            {
                Name = "Location",
                HeaderText = "Location",
                DataPropertyName = "Location", // Binds to SelectableDeviceIP.Location
                Width = 200
            };
            dataGridViewDeviceIPs.Columns.Add(colLocation);
        }


        // Renamed and modified method to load Device IPs
        private void LoadDeviceIPs()
        {
            try
            {
                DeviceIPRepository deviceRepo = new DeviceIPRepository();
                TagRepository tagRepo = new TagRepository(); // Need this to check existing assignments

                List<DeviceIP> allDevices = deviceRepo.GetAllDevices();
                List<int> currentlyAssignedDeviceIds = tagRepo.GetDeviceIPsForTag(currentTag.Id); // Get IDs already assigned to THIS tag

                // Filter based on search text (ensure txtSearchCustomers is renamed or use the correct name)
                string search = txtSearchCustomers.Text.Trim(); // Use the correct TextBox name
                if (!string.IsNullOrEmpty(search))
                {
                    allDevices = allDevices
                        .Where(d => (d.DeviceName != null && d.DeviceName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (d.IPAddress != null && d.IPAddress.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (d.Location != null && d.Location.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToList();
                }

                // Create SelectableDeviceIP objects and pre-check assigned ones
                var selectableDevices = allDevices.Select(dev => new SelectableDeviceIP
                {
                    Device = dev,
                    IsSelected = currentlyAssignedDeviceIds.Contains(dev.Id) // Check if this device is already assigned
                }).ToList();


                // Bind to the correct DataGridView
                dataGridViewDeviceIPs.DataSource = null;
                dataGridViewDeviceIPs.DataSource = selectableDevices;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Device IPs: " + ex.Message);
            }
        }

        // Event handler for search text changes
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadDeviceIPs(); // Reload Device IPs based on the new search term
        }

        // Click event for the "Assign Tag" button
        private void btnAssign_Click(object sender, EventArgs e)
        {
            try
            {
                TagRepository tagRepo = new TagRepository();
                List<int> idsToAssign = new List<int>();
                List<int> idsToRemove = new List<int>();

                // Loop through rows to determine which assignments to add or remove
                foreach (DataGridViewRow row in dataGridViewDeviceIPs.Rows)
                {
                    if (row.DataBoundItem is SelectableDeviceIP item)
                    {
                        bool currentlySelectedInGrid = Convert.ToBoolean(row.Cells["SelectDeviceIP"].Value ?? false);
                        bool wasOriginallySelected = tagRepo.GetDeviceIPsForTag(currentTag.Id).Contains(item.Id); // Re-check original state

                        if (currentlySelectedInGrid && !wasOriginallySelected)
                        {
                            // User selected it, and it wasn't originally assigned -> Add assignment
                            idsToAssign.Add(item.Id);
                        }
                        else if (!currentlySelectedInGrid && wasOriginallySelected)
                        {
                            // User deselected it, and it *was* originally assigned -> Remove assignment
                            idsToRemove.Add(item.Id);
                        }
                    }
                }

                // Perform database operations
                foreach (int deviceId in idsToAssign)
                {
                    tagRepo.AssignTagToDeviceIP(deviceId, currentTag.Id);
                }
                foreach (int deviceId in idsToRemove)
                {
                    tagRepo.RemoveTagFromDeviceIP(deviceId, currentTag.Id);
                }


                // Notify parent form and close
                DeviceIPAssigned?.Invoke(this, EventArgs.Empty);
                //MessageBox.Show("Device IP assignments updated successfully."); // Optional confirmation
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating Device IP assignments: " + ex.Message);
            }
        }

        // Click event for the Cancel button
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
