using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Services;
using CustomerAndServerMaintenanceTracking.DataAccess;



namespace CustomerAndServerMaintenanceTracking
{
    public partial class Customers: Form
    {
        public Customers()
        {
            InitializeComponent();

            // Subscribe to the event. Use Invoke if needed to ensure the update happens on the UI thread.
            CustomerRepository.CustomerUpdated += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    LoadActiveCustomers();
                });
            };
        }

        private void SyncMikrotikData()
        {
            // Replace these with your actual Mikrotik credentials or retrieve them from config.
            string host = "192.168.170.1";
            string username = "CSMT";
            string password = "CSMT";

            try
            {
                SyncManager syncManager = new SyncManager();
                syncManager.SyncCustomers(host, username, password);

                // Refresh the DataGridView with updated data from the database.
                LoadActiveCustomers();
                LoadArchivedCustomers();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during sync: " + ex.Message);
            }
        }

        private void LoadActiveCustomers()
        {
            try
            {
                CustomerRepository repo = new CustomerRepository();
                // Filter active customers (IsArchived == false)
                var customers = repo.GetCustomers().Where(c => !c.IsArchived).ToList();
                dataGridViewActiveCustomers.DataSource = null; // Clear current binding
                dataGridViewActiveCustomers.DataSource = customers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading active customers: " + ex.Message);
            }

        }

        private void LoadArchivedCustomers()
        {
            try
            {
                CustomerRepository repo = new CustomerRepository();
                // Filter archived customers (IsArchived == true)
                var customers = repo.GetCustomers().Where(c => c.IsArchived).ToList();
                dataGridViewArchivedCustomers.DataSource = null; // Clear current binding
                dataGridViewArchivedCustomers.DataSource = customers;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading archived customers: " + ex.Message);
            }
        }

        private void timerSync_Tick(object sender, EventArgs e)
        {
            SyncMikrotikData();
            
            // Refresh the DataGridView with the latest data from the database.
            LoadActiveCustomers();
            LoadArchivedCustomers();

            // Update the label with the current time in the format "Synced: 7:01:10 PM"
            lblsyncActive.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
            lblsyncArchived.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SyncMikrotikData();
            LoadActiveCustomers();
            LoadArchivedCustomers();
            lblsyncActive.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
            lblsyncArchived.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
        }

        private void btnRefreshArchived_Click(object sender, EventArgs e)
        {
            SyncMikrotikData();
            LoadArchivedCustomers();
            lblsyncArchived.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
        }

        private void btnRefreshActive_Click(object sender, EventArgs e)
        {
            SyncMikrotikData();
            LoadActiveCustomers();
            lblsyncActive.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
        }
    }
    
}
