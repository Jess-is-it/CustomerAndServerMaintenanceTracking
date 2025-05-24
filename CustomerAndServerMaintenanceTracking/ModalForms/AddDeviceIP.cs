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

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddDeviceIP : Form
    {
        public event EventHandler DeviceAdded;

        public AddDeviceIP()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddDevice_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                MessageBox.Show("Please enter a Device Name.");
                return;
            }

            // Create new DeviceIP model and set TagType to "DeviceIP"
            DeviceIP newDevice = new DeviceIP()
            {
                DeviceName = txtDeviceName.Text.Trim(),
                IPAddress = txtIPAddress.Text.Trim(),
                Location = txtLocation.Text.Trim()
            };

            DeviceIPRepository repo = new DeviceIPRepository();
            repo.AddDevice(newDevice);

            // Raise the event so the parent form can refresh its grid
            DeviceAdded?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
    }
}
