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

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class EditRouter : Form
    {
        private MikrotikRouter currentRouter;
        // Event to notify that a router was edited.
        public event EventHandler RouterSaved;

        public EditRouter(MikrotikRouter router)
        {
            InitializeComponent();
            currentRouter = router;
            // Pre-populate the form fields with the router's data.
            txtRouterName.Text = router.RouterName;
            txtHostIPAddress.Text = router.HostIPAddress;
            txtUsername.Text = router.Username;
            txtPassword.Text = router.Password;
            txtApiPort.Text = router.ApiPort.ToString(); // <-- Ensure this line is added.
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnUpdateRouter_Click(object sender, EventArgs e)
        {
            // Validate all fields including the API Port field.
            if (string.IsNullOrWhiteSpace(txtRouterName.Text) ||
                string.IsNullOrWhiteSpace(txtHostIPAddress.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                string.IsNullOrWhiteSpace(txtApiPort.Text))
            {
                MessageBox.Show("Please fill in all fields, including API Port.",
                                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate that API Port is a valid integer.
            if (!int.TryParse(txtApiPort.Text.Trim(), out int apiPort))
            {
                MessageBox.Show("Please enter a valid API Port number.",
                                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update the current router object with new values.
            currentRouter.RouterName = txtRouterName.Text.Trim();
            currentRouter.HostIPAddress = txtHostIPAddress.Text.Trim();
            currentRouter.ApiPort = apiPort; // New: update the API Port.
            currentRouter.Username = txtUsername.Text.Trim();
            currentRouter.Password = txtPassword.Text.Trim();

            // Update the router in the database.
            MikrotikRouterRepository repo = new MikrotikRouterRepository();
            repo.UpdateRouter(currentRouter);

            // Notify subscribers that a router has been updated.
            RouterSaved?.Invoke(this, EventArgs.Empty);

            this.Close();
        }
    }
}
