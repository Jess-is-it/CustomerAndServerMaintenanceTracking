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
        private SyncManager syncManager;

        public Customers()
        {
            InitializeComponent();
            InitializeCustomerDataGridView();
            InitializeArchivedCustomersDataGridView();

            // Subscribe to the DataSynced event
            SyncManager.DataSynced += SyncManager_DataSynced;

            // Subscribe to the event. Use Invoke if needed to ensure the update happens on the UI thread.
            CustomerRepository.CustomerUpdated += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    LoadActiveCustomers();
                });
            };
        }

        //TABLE COLUMNS
        private void InitializeCustomerDataGridView()
        {
            // Clear any existing columns.
            dataGridViewActiveCustomers.Columns.Clear();

            // Set AutoGenerateColumns to false since we're adding columns manually.
            dataGridViewActiveCustomers.AutoGenerateColumns = false;

            // Create and add the ID column.
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.DataPropertyName = "Id"; // Must match the property in Customer
            idColumn.HeaderText = "ID";
            idColumn.Width = 50;
            dataGridViewActiveCustomers.Columns.Add(idColumn);

            // Create and add the Account Name column.
            DataGridViewTextBoxColumn accountNameColumn = new DataGridViewTextBoxColumn();
            accountNameColumn.DataPropertyName = "AccountName";
            accountNameColumn.HeaderText = "Account Name";
            accountNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewActiveCustomers.Columns.Add(accountNameColumn);

            // Create and add the Additional Name column.
            DataGridViewTextBoxColumn additionalNameColumn = new DataGridViewTextBoxColumn();
            additionalNameColumn.DataPropertyName = "AdditionalName";
            additionalNameColumn.HeaderText = "Additional Name";
            additionalNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewActiveCustomers.Columns.Add(additionalNameColumn);

            // Create and add the Contact Number column.
            DataGridViewTextBoxColumn contactNumberColumn = new DataGridViewTextBoxColumn();
            contactNumberColumn.DataPropertyName = "ContactNumber";
            contactNumberColumn.HeaderText = "Contact Number";
            contactNumberColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewActiveCustomers.Columns.Add(contactNumberColumn);

            // Create and add the Email column.
            DataGridViewTextBoxColumn emailColumn = new DataGridViewTextBoxColumn();
            emailColumn.DataPropertyName = "Email";
            emailColumn.HeaderText = "Email";
            emailColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewActiveCustomers.Columns.Add(emailColumn);

            // Create and add the Location column.
            DataGridViewTextBoxColumn locationColumn = new DataGridViewTextBoxColumn();
            locationColumn.DataPropertyName = "Location";
            locationColumn.HeaderText = "Location";
            locationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewActiveCustomers.Columns.Add(locationColumn);

            //// Create and add the IsArchived column.
            //DataGridViewCheckBoxColumn archivedColumn = new DataGridViewCheckBoxColumn();
            //archivedColumn.DataPropertyName = "IsArchived";
            //archivedColumn.HeaderText = "Archived";
            //archivedColumn.Width = 70;
            //dataGridViewActiveCustomers.Columns.Add(archivedColumn);

            // Create and add the IP Address column.
            DataGridViewTextBoxColumn ipColumn = new DataGridViewTextBoxColumn();
            ipColumn.DataPropertyName = "IPAddress"; // Must match the property in Customer
            ipColumn.HeaderText = "IP Address";
            ipColumn.Width = 120; // Adjust width as needed
            dataGridViewActiveCustomers.Columns.Add(ipColumn);
        }
        private void InitializeArchivedCustomersDataGridView()
        {
            dataGridViewArchivedCustomers.Columns.Clear();
            dataGridViewArchivedCustomers.AutoGenerateColumns = false;

            // (1) ID column.
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.DataPropertyName = "Id";
            idColumn.HeaderText = "ID";
            idColumn.Width = 50;
            dataGridViewArchivedCustomers.Columns.Add(idColumn);

            // (2) Account Name column.
            DataGridViewTextBoxColumn accountNameColumn = new DataGridViewTextBoxColumn();
            accountNameColumn.DataPropertyName = "AccountName";
            accountNameColumn.HeaderText = "Account Name";
            accountNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewArchivedCustomers.Columns.Add(accountNameColumn);

            // (3) Additional Name column.
            DataGridViewTextBoxColumn additionalNameColumn = new DataGridViewTextBoxColumn();
            additionalNameColumn.DataPropertyName = "AdditionalName";
            additionalNameColumn.HeaderText = "Additional Name";
            additionalNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewArchivedCustomers.Columns.Add(additionalNameColumn);

            // (4) Contact Number column.
            DataGridViewTextBoxColumn contactNumberColumn = new DataGridViewTextBoxColumn();
            contactNumberColumn.DataPropertyName = "ContactNumber";
            contactNumberColumn.HeaderText = "Contact Number";
            contactNumberColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewArchivedCustomers.Columns.Add(contactNumberColumn);

            // (5) Email column.
            DataGridViewTextBoxColumn emailColumn = new DataGridViewTextBoxColumn();
            emailColumn.DataPropertyName = "Email";
            emailColumn.HeaderText = "Email";
            emailColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewArchivedCustomers.Columns.Add(emailColumn);

            // (6) Location column.
            DataGridViewTextBoxColumn locationColumn = new DataGridViewTextBoxColumn();
            locationColumn.DataPropertyName = "Location";
            locationColumn.HeaderText = "Location";
            locationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewArchivedCustomers.Columns.Add(locationColumn);

            //// (7) IsArchived column.
            //DataGridViewCheckBoxColumn archivedColumn = new DataGridViewCheckBoxColumn();
            //archivedColumn.DataPropertyName = "IsArchived";
            //archivedColumn.HeaderText = "Archived";
            //archivedColumn.Width = 70;
            //dataGridViewArchivedCustomers.Columns.Add(archivedColumn);

            // (8) IP Address column.
            DataGridViewTextBoxColumn ipColumn = new DataGridViewTextBoxColumn();
            ipColumn.DataPropertyName = "IPAddress";
            ipColumn.HeaderText = "IP Address";
            ipColumn.Width = 120;
            dataGridViewArchivedCustomers.Columns.Add(ipColumn);
        }


        private void SyncManager_DataSynced(object sender, EventArgs e)
        {
            // Use Invoke or BeginInvoke to update the UI on the main thread.
            this.BeginInvoke((MethodInvoker)delegate {
                // Refresh Datagridview and preserve scroll
                RefreshDataGridViewWithScrollPreservation();
            });
        }

        private void RefreshDataGridViewWithScrollPreservation()
        {
            // Save the current scroll position
            int firstDisplayedIndex = dataGridViewActiveCustomers.FirstDisplayedScrollingRowIndex;

            int secondDisplayedIndex = dataGridViewArchivedCustomers.FirstDisplayedScrollingRowIndex;

            // Refresh the data (this might reset the grid's scroll position)
            LoadActiveCustomers(); // Your method to refresh the data source
            LoadArchivedCustomers();

            // Restore the scroll position if valid
            if (firstDisplayedIndex >= 0 && firstDisplayedIndex < dataGridViewActiveCustomers.Rows.Count)
            {
                dataGridViewActiveCustomers.FirstDisplayedScrollingRowIndex = firstDisplayedIndex;
            }
            if (secondDisplayedIndex >= 0 && secondDisplayedIndex < dataGridViewArchivedCustomers.Rows.Count)
            {
                dataGridViewArchivedCustomers.FirstDisplayedScrollingRowIndex = secondDisplayedIndex;
            }
        }


        private void SyncMikrotikData()
        {
            try
            {
                // Use the persistent connection via syncManager (no credentials needed here)
                syncManager?.SyncCustomers();

                // Refresh the DataGridViews with updated data from the database.
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
            //try
            //{
            //    CustomerRepository repo = new CustomerRepository();
            //    // Filter active customers (IsArchived == false)
            //    var customers = repo.GetCustomers().Where(c => !c.IsArchived).ToList();
            //    dataGridViewActiveCustomers.DataSource = null; // Clear current binding
            //    dataGridViewActiveCustomers.DataSource = customers;
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error loading active customers: " + ex.Message);
            //}
            try
            {
                CustomerRepository repo = new CustomerRepository();
                // Retrieve all active customers (assuming IsArchived = false)
                var customers = repo.GetCustomers().Where(c => !c.IsArchived).ToList();

                // Get the search term from txtSearchActive
                string search = txtSearchActive.Text.Trim();

                // If search is not empty, filter the list (you can extend this filter as needed)
                if (!string.IsNullOrEmpty(search))
                {
                    customers = customers.Where(c => c.AccountName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                // Bind the filtered list to the DataGridView.
                dataGridViewActiveCustomers.DataSource = customers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading active customers: " + ex.Message);
            }

        }

        private void LoadArchivedCustomers()
        {
            //try
            //{
            //    CustomerRepository repo = new CustomerRepository();
            //    // Filter archived customers (IsArchived == true)
            //    var customers = repo.GetCustomers().Where(c => c.IsArchived).ToList();
            //    dataGridViewArchivedCustomers.DataSource = null; // Clear current binding
            //    dataGridViewArchivedCustomers.DataSource = customers;
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error loading archived customers: " + ex.Message);
            //}
            try
            {
                CustomerRepository repo = new CustomerRepository();
                // Retrieve all archived customers (assuming IsArchived = true)
                var customers = repo.GetCustomers().Where(c => c.IsArchived).ToList();

                // Get the search term from txtSearchArchived
                string search = txtSearchArchived.Text.Trim();

                // If search is not empty, filter the list
                if (!string.IsNullOrEmpty(search))
                {
                    customers = customers.Where(c => c.AccountName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                // Bind the filtered list to the DataGridView.
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
            lblsyncActive.Text = "Synced: " + DateTime.Now.ToString("h:mm:ss tt");
        }

        private void Customers_FormClosing(object sender, FormClosingEventArgs e)
        {
            SyncManager.DataSynced -= SyncManager_DataSynced;
        }


        private void Customers_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                RefreshDataGridViewWithScrollPreservation();
            }
        }

        private void txtSearchActive_TextChanged(object sender, EventArgs e)
        {
            LoadActiveCustomers();

        }

        private void txtSearchArchived_TextChanged(object sender, EventArgs e)
        {
            LoadArchivedCustomers();
        }
    }
    
}
