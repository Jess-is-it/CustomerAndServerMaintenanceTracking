using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using System;
using System.Windows.Forms;
using tik4net; // IMPORTANT: You may need to add a reference to the tik4net.dll in your main application project.

namespace CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms
{
    /// <summary>
    /// A dedicated static helper class to test Mikrotik API connections.
    /// This is self-contained and uses the same tik4net library as your services.
    /// </summary>
    public static class MikrotikConnectionTester
    {
        /// <summary>
        /// Attempts to open and close a connection to a Mikrotik router's API port.
        /// </summary>
        /// <returns>True if the connection was successful, otherwise false.</returns>
        public static bool TestApiConnection(string host, int apiPort, string username, string password)
        {
            try
            {
                // The 'using' statement ensures the connection is properly closed and disposed,
                // even if an error occurs. This is the correct way to test a connection.
                using (ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api))
                {
                    // Set a reasonable timeout for the UI so it doesn't hang indefinitely
                    connection.SendTimeout = 5000;    // 5 seconds
                    connection.ReceiveTimeout = 5000; // 5 seconds

                    connection.Open(host, apiPort, username, password);
                    // If connection.Open() completes without throwing an exception, the credentials are valid.
                    return connection.IsOpened;
                }
            }
            catch (Exception ex)
            {
                // Log the technical exception for debugging purposes.
                Console.WriteLine($"Mikrotik API connection test failed: {ex.Message}");
                // For the user, we simply return false to indicate failure.
                return false;
            }
        }
    }


    public partial class AddRouter : Form
    {
        public event EventHandler RouterSaved;
        private readonly MikrotikRouter _routerToEdit;
        private readonly MikrotikRouterRepository _repo;

        // Constructor for Adding
        public AddRouter()
        {
            InitializeComponent();
            _repo = new MikrotikRouterRepository();

            this.Text = "Add Router";
            this.label1.Text = "Add Router";
            this.btnAddRouter.Text = "Add Router";

            // Initialize event handlers
            this.btnAddRouter.Click += new System.EventHandler(this.btnSave_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        }

        // Constructor for Editing
        public AddRouter(MikrotikRouter routerToEdit) : this()
        {
            _routerToEdit = routerToEdit;

            this.Text = "Edit Router";
            this.label1.Text = "Edit Router";
            this.btnAddRouter.Text = "Save Changes";

            PopulateFields();
        }

        private void PopulateFields()
        {
            if (_routerToEdit == null) return;

            txtRouterName.Text = _routerToEdit.RouterName;
            txtHostIPAddress.Text = _routerToEdit.HostIPAddress;
            txtApiPort.Text = _routerToEdit.ApiPort.ToString();
            txtUsername.Text = _routerToEdit.Username;
            txtPassword.Text = _routerToEdit.Password;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // 1. Validate the input fields first
            if (!ValidateFields(out string host, out int apiPort))
            {
                return; // Stop if validation fails
            }

            // 2. Perform the connection test before saving
            try
            {
                this.Cursor = Cursors.WaitCursor; // Provide user feedback

                bool isConnectionSuccessful = MikrotikConnectionTester.TestApiConnection(host, apiPort, txtUsername.Text.Trim(), txtPassword.Text.Trim());

                this.Cursor = Cursors.Default;

                if (!isConnectionSuccessful)
                {
                    MessageBox.Show("Connection failed. Please check the Host IP Address, API Port, and credentials.", "Connection Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // Stop the save process
                }

                MessageBox.Show("Connection successful! Credentials are valid.", "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"An error occurred during the connection test: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            // 3. If the connection test was successful, proceed with saving
            try
            {
                if (_routerToEdit == null) // Add Mode
                {
                    var newRouter = new MikrotikRouter
                    {
                        RouterName = txtRouterName.Text.Trim(),
                        HostIPAddress = host,
                        ApiPort = apiPort,
                        Username = txtUsername.Text.Trim(),
                        Password = txtPassword.Text.Trim()
                    };
                    _repo.AddRouter(newRouter);
                }
                else // Edit Mode
                {
                    _routerToEdit.RouterName = txtRouterName.Text.Trim();
                    _routerToEdit.HostIPAddress = host;
                    _routerToEdit.ApiPort = apiPort;
                    _routerToEdit.Username = txtUsername.Text.Trim();
                    _routerToEdit.Password = txtPassword.Text.Trim();
                    _repo.UpdateRouter(_routerToEdit);
                }

                RouterSaved?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the router: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateFields(out string host, out int apiPort)
        {
            host = null;
            apiPort = 0;

            if (string.IsNullOrWhiteSpace(txtRouterName.Text) ||
                string.IsNullOrWhiteSpace(txtHostIPAddress.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                string.IsNullOrWhiteSpace(txtApiPort.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(txtApiPort.Text.Trim(), out apiPort))
            {
                MessageBox.Show("Please enter a valid number for the API Port.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            host = txtHostIPAddress.Text.Trim();

            return true;
        }
    }
}
