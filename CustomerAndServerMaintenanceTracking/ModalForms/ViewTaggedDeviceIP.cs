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
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class ViewTaggedDeviceIP : Form
    {
        // Event to notify TagForm if assignments change (e.g., via the Edit button)
        public event EventHandler DeviceIPUpdated;

        private TagClass currentTag;
        private TagRepository tagRepo; // Use a class-level repository instance
        private DeviceIPRepository deviceRepo; // Also useful

        public ViewTaggedDeviceIP(TagClass tag)
        {
            InitializeComponent();
            currentTag = tag ?? throw new ArgumentNullException(nameof(tag));
            tagRepo = new TagRepository();
            deviceRepo = new DeviceIPRepository(); // Initialize if needed elsewhere

            this.Text = $"Device IPs Tagged With: {currentTag.TagName}"; // Set form title
            label1.Text = $"Device IPs Tagged With: {currentTag.TagName}"; // Set label text

            InitializeGridColumns(); // Setup the grid columns first
            LoadDeviceIP();         // Load the assigned devices
        }

        // Method to set up DataGridView columns correctly for DeviceIP
        private void InitializeGridColumns()
        {
            dataGridViewEntities.Columns.Clear(); // Assuming grid is named dataGridViewEntities
            dataGridViewEntities.AutoGenerateColumns = false;

            // Add columns suitable for DeviceIP data
            var colId = new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Name = "Id", Visible = false };
            var colName = new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName", HeaderText = "Device Name", Name = "DeviceName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            var colIP = new DataGridViewTextBoxColumn { DataPropertyName = "IPAddress", HeaderText = "IP Address", Name = "IPAddress", Width = 120 };
            var colLocation = new DataGridViewTextBoxColumn { DataPropertyName = "Location", HeaderText = "Location", Name = "Location", Width = 150 };

            dataGridViewEntities.Columns.AddRange(new DataGridViewColumn[] { colId, colName, colIP, colLocation });
        }

        // Modified method to load actual DeviceIP objects
        private void LoadDeviceIP()
        {
            try
            {
                // Use the new repository method
                List<DeviceIP> assignedDevices = tagRepo.GetAssignedDeviceIPs(currentTag.Id);

                // Apply search filter based on DeviceIP properties
                string search = txtSearch.Text.Trim(); // Assuming TextBox name is txtSearch
                if (!string.IsNullOrEmpty(search))
                {
                    assignedDevices = assignedDevices
                        .Where(d => (d.DeviceName != null && d.DeviceName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (d.IPAddress != null && d.IPAddress.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (d.Location != null && d.Location.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToList();
                }

                // Bind the List<DeviceIP> directly to the DataGridView
                dataGridViewEntities.DataSource = null;
                dataGridViewEntities.DataSource = assignedDevices;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading assigned Device IPs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for the search TextBox
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadDeviceIP(); // Reload data when search text changes
        }

        // Event handler for the Edit button
        private void btnEdit_Click(object sender, EventArgs e)
        {
            // Open the AssignDeviceIP form to allow modification of assignments for the currentTag
            AssignDeviceIP assignForm = new AssignDeviceIP(currentTag);

            // Subscribe to the event that indicates assignments were changed in the Assign form
            assignForm.DeviceIPAssigned += (s, ea) =>
            {
                LoadDeviceIP(); // Reload the data in *this* form's grid
                DeviceIPUpdated?.Invoke(this, EventArgs.Empty); // Notify the parent (TagForm) to refresh its display
            };

            assignForm.StartPosition = FormStartPosition.CenterScreen; // Center the assignment form
            assignForm.ShowDialog(); // Show it modally
            // No need for overlay here as this form itself is likely modal already
        }


    }
}
