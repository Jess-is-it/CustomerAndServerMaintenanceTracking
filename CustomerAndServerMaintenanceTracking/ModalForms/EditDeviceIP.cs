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
    public partial class EditDeviceIP : Form
    {
        private DeviceIP currentDevice;
        public event EventHandler DeviceUpdated;

        public EditDeviceIP(DeviceIP device)
        {
            InitializeComponent();
            currentDevice = device;
            txtDeviceName.Text = device.DeviceName;
            txtIPAddress.Text = device.IPAddress;
            txtLocation.Text = device.Location;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnUpdateDevice_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                MessageBox.Show("Please enter Device Name.");
                return;
            }

            currentDevice.DeviceName = txtDeviceName.Text.Trim();
            currentDevice.IPAddress = txtIPAddress.Text.Trim();
            currentDevice.Location = txtLocation.Text.Trim();

            DeviceIPRepository repo = new DeviceIPRepository();
            repo.UpdateDevice(currentDevice);

            DeviceUpdated?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
    }
}
