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

namespace CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms
{
    public partial class AddRouter : Form
    {
        // Event to notify that a router has been saved.
        public event EventHandler RouterSaved;

        public AddRouter()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddRouter_Click(object sender, EventArgs e)
        {

            // Validate fields (including API Port)
            if (string.IsNullOrWhiteSpace(txtRouterName.Text) ||
                string.IsNullOrWhiteSpace(txtHostIPAddress.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                string.IsNullOrWhiteSpace(txtApiPort.Text))  // new validation for API Port
            {
                MessageBox.Show("Please fill in all fields, including API Port.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate that API Port is a valid integer
            if (!int.TryParse(txtApiPort.Text.Trim(), out int apiPort))
            {
                MessageBox.Show("Please enter a valid API Port number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create a new Router object from the form data.
            MikrotikRouter newRouter = new MikrotikRouter()
            {
                RouterName = txtRouterName.Text.Trim(),
                HostIPAddress = txtHostIPAddress.Text.Trim(),
                ApiPort = apiPort,  // new assignment for API Port
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text.Trim()
            };

            // Save the router to the database.
            MikrotikRouterRepository repo = new MikrotikRouterRepository();
            repo.AddRouter(newRouter);

            // Notify subscribers that a router has been saved.
            RouterSaved?.Invoke(this, EventArgs.Empty);

            this.Close();
        }
    }
}
