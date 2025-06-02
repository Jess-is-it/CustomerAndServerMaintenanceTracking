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
using SharedLibrary.DataAccess;



namespace CustomerAndServerMaintenanceTracking
{
    public partial class Customers : Form, IRefreshableForm
    {
        private DateTime? _lastSuccessfulSyncTimestamp = null;
        private NetwatchConfigRepository _netwatchConfigRepo;
        private ServiceLogRepository _serviceLogRepoForForm;

        public void RefreshDataViews()
        {
            RefreshDataGridViewWithScrollPreservation();
        }

        public Customers()
        {
            InitializeComponent();

            try
            {
                _serviceLogRepoForForm = new ServiceLogRepository(); // Instantiate the logger
                                                                     // The NetwatchConfigRepository from SharedLibrary needs at least a ServiceLogRepository
                                                                     // If you don't need TagRepository for this form's _netwatchConfigRepo instance:
                _netwatchConfigRepo = new NetwatchConfigRepository(_serviceLogRepoForForm);
                // Or if you do need TagRepository for other potential uses of _netwatchConfigRepo:
                // TagRepository tagRepoForForm = new TagRepository();
                // _netwatchConfigRepo = new NetwatchConfigRepository(_serviceLogRepoForForm, tagRepoForForm);
            }
            catch (Exception ex)
            {
                LogStatus($"CustomersForm Error: Could not initialize NetwatchConfigRepository. Sync time display might be affected. {ex.Message}");
                _netwatchConfigRepo = null; // Ensure it's null if init fails
            }

            InitializeCustomerDataGridView();
            InitializeArchivedCustomersDataGridView();
            CustomerRepository.CustomerUpdated += OnCustomerDataUpdated;
        }

        // --- MODIFIED: InitializeCustomerDataGridView ---
        private void InitializeCustomerDataGridView()
        {
            dataGridViewActiveCustomers.Columns.Clear();
            dataGridViewActiveCustomers.AutoGenerateColumns = false;

            // --- NEW: Router Column (First Column) ---
            DataGridViewTextBoxColumn routerColumn = new DataGridViewTextBoxColumn();
            routerColumn.DataPropertyName = "RouterName"; // From Customer model (populated by CustomerRepository)
            routerColumn.HeaderText = "Router";
            routerColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; // Or Fill, adjust as needed
            routerColumn.DisplayIndex = 0; // Ensure it's first
            dataGridViewActiveCustomers.Columns.Add(routerColumn);

            // ID column (Hidden)
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.DataPropertyName = "Id";
            idColumn.HeaderText = "ID";
            idColumn.Width = 50;
            idColumn.Visible = false; // --- MODIFIED: Hide ID column ---
            idColumn.DisplayIndex = 1;
            dataGridViewActiveCustomers.Columns.Add(idColumn);

            // Account Name column
            DataGridViewTextBoxColumn accountNameColumn = new DataGridViewTextBoxColumn();
            accountNameColumn.DataPropertyName = "AccountName";
            accountNameColumn.HeaderText = "Account Name";
            accountNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            accountNameColumn.FillWeight = 150; // Give it more weight if using Fill
            accountNameColumn.DisplayIndex = 2;
            dataGridViewActiveCustomers.Columns.Add(accountNameColumn);

            // Additional Name column
            DataGridViewTextBoxColumn additionalNameColumn = new DataGridViewTextBoxColumn();
            additionalNameColumn.DataPropertyName = "AdditionalName";
            additionalNameColumn.HeaderText = "Additional Name";
            additionalNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            additionalNameColumn.FillWeight = 100;
            additionalNameColumn.DisplayIndex = 3;
            dataGridViewActiveCustomers.Columns.Add(additionalNameColumn);

            // --- NEW: MAC Address Column ---
            DataGridViewTextBoxColumn macAddressColumn = new DataGridViewTextBoxColumn();
            macAddressColumn.DataPropertyName = "MacAddress"; // From Customer model
            macAddressColumn.HeaderText = "MAC Address";
            macAddressColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; // Or set a fixed width
            macAddressColumn.Width = 130;
            macAddressColumn.DisplayIndex = 4;
            dataGridViewActiveCustomers.Columns.Add(macAddressColumn);

            // IP Address column
            DataGridViewTextBoxColumn ipColumn = new DataGridViewTextBoxColumn();
            ipColumn.DataPropertyName = "IPAddress";
            ipColumn.HeaderText = "IP Address";
            ipColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ipColumn.Width = 120;
            ipColumn.DisplayIndex = 5;
            dataGridViewActiveCustomers.Columns.Add(ipColumn);

            // Contact Number column
            DataGridViewTextBoxColumn contactNumberColumn = new DataGridViewTextBoxColumn();
            contactNumberColumn.DataPropertyName = "ContactNumber";
            contactNumberColumn.HeaderText = "Contact Number";
            contactNumberColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            contactNumberColumn.Width = 120;
            contactNumberColumn.DisplayIndex = 6;
            dataGridViewActiveCustomers.Columns.Add(contactNumberColumn);

            // Email column
            DataGridViewTextBoxColumn emailColumn = new DataGridViewTextBoxColumn();
            emailColumn.DataPropertyName = "Email";
            emailColumn.HeaderText = "Email";
            emailColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            emailColumn.FillWeight = 100;
            emailColumn.DisplayIndex = 7;
            dataGridViewActiveCustomers.Columns.Add(emailColumn);

            // Location column
            DataGridViewTextBoxColumn locationColumn = new DataGridViewTextBoxColumn();
            locationColumn.DataPropertyName = "Location";
            locationColumn.HeaderText = "Location";
            locationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            locationColumn.FillWeight = 100;
            locationColumn.DisplayIndex = 8;
            dataGridViewActiveCustomers.Columns.Add(locationColumn);

            // Optional: Apply default sort order if desired
            // dataGridViewActiveCustomers.Sort(dataGridViewActiveCustomers.Columns["RouterName"], ListSortDirection.Ascending);
        }

        // --- MODIFIED: InitializeArchivedCustomersDataGridView ---
        private void InitializeArchivedCustomersDataGridView()
        {
            dataGridViewArchivedCustomers.Columns.Clear();
            dataGridViewArchivedCustomers.AutoGenerateColumns = false;

            // --- NEW: Router Column (First Column) ---
            DataGridViewTextBoxColumn routerColumn = new DataGridViewTextBoxColumn();
            routerColumn.DataPropertyName = "RouterName";
            routerColumn.HeaderText = "Router";
            routerColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            routerColumn.DisplayIndex = 0;
            dataGridViewArchivedCustomers.Columns.Add(routerColumn);

            // ID column (Hidden)
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.DataPropertyName = "Id";
            idColumn.HeaderText = "ID";
            idColumn.Width = 50;
            idColumn.Visible = false; // --- MODIFIED: Hide ID column ---
            idColumn.DisplayIndex = 1;
            dataGridViewArchivedCustomers.Columns.Add(idColumn);

            // Account Name column
            DataGridViewTextBoxColumn accountNameColumn = new DataGridViewTextBoxColumn();
            accountNameColumn.DataPropertyName = "AccountName";
            accountNameColumn.HeaderText = "Account Name";
            accountNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            accountNameColumn.FillWeight = 150;
            accountNameColumn.DisplayIndex = 2;
            dataGridViewArchivedCustomers.Columns.Add(accountNameColumn);

            // Additional Name column
            DataGridViewTextBoxColumn additionalNameColumn = new DataGridViewTextBoxColumn();
            additionalNameColumn.DataPropertyName = "AdditionalName";
            additionalNameColumn.HeaderText = "Additional Name";
            additionalNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            additionalNameColumn.FillWeight = 100;
            additionalNameColumn.DisplayIndex = 3;
            dataGridViewArchivedCustomers.Columns.Add(additionalNameColumn);

            // --- NEW: MAC Address Column ---
            DataGridViewTextBoxColumn macAddressColumn = new DataGridViewTextBoxColumn();
            macAddressColumn.DataPropertyName = "MacAddress";
            macAddressColumn.HeaderText = "MAC Address";
            macAddressColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            macAddressColumn.Width = 130;
            macAddressColumn.DisplayIndex = 4;
            dataGridViewArchivedCustomers.Columns.Add(macAddressColumn);

            // IP Address column
            DataGridViewTextBoxColumn ipColumn = new DataGridViewTextBoxColumn();
            ipColumn.DataPropertyName = "IPAddress";
            ipColumn.HeaderText = "IP Address";
            ipColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ipColumn.Width = 120;
            ipColumn.DisplayIndex = 5;
            dataGridViewArchivedCustomers.Columns.Add(ipColumn);

            // Contact Number column
            DataGridViewTextBoxColumn contactNumberColumn = new DataGridViewTextBoxColumn();
            contactNumberColumn.DataPropertyName = "ContactNumber";
            contactNumberColumn.HeaderText = "Contact Number";
            contactNumberColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            contactNumberColumn.Width = 120;
            contactNumberColumn.DisplayIndex = 6;
            dataGridViewArchivedCustomers.Columns.Add(contactNumberColumn);

            // Email column
            DataGridViewTextBoxColumn emailColumn = new DataGridViewTextBoxColumn();
            emailColumn.DataPropertyName = "Email";
            emailColumn.HeaderText = "Email";
            emailColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            emailColumn.FillWeight = 100;
            emailColumn.DisplayIndex = 7;
            dataGridViewArchivedCustomers.Columns.Add(emailColumn);

            // Location column
            DataGridViewTextBoxColumn locationColumn = new DataGridViewTextBoxColumn();
            locationColumn.DataPropertyName = "Location";
            locationColumn.HeaderText = "Location";
            locationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            locationColumn.FillWeight = 100;
            locationColumn.DisplayIndex = 8;
            dataGridViewArchivedCustomers.Columns.Add(locationColumn);

            // Optional: Apply default sort order
            // dataGridViewArchivedCustomers.Sort(dataGridViewArchivedCustomers.Columns["RouterName"], ListSortDirection.Ascending);
        }

        // Event handler for CustomerRepository.CustomerUpdated
        private void OnCustomerDataUpdated(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate {
                    RefreshDataGridViewWithScrollPreservation();
                });
            }
            else
            {
                RefreshDataGridViewWithScrollPreservation();
            }
        }

        private void RefreshDataGridViewWithScrollPreservation()
        {
            //int firstDisplayedIndexActive = -1;
            //if (dataGridViewActiveCustomers.Rows.Count > 0)
            //    firstDisplayedIndexActive = dataGridViewActiveCustomers.FirstDisplayedScrollingRowIndex;

            //int firstDisplayedIndexArchived = -1;
            //if (dataGridViewArchivedCustomers.Rows.Count > 0)
            //    firstDisplayedIndexArchived = dataGridViewArchivedCustomers.FirstDisplayedScrollingRowIndex;

            //LoadActiveCustomers();
            //LoadArchivedCustomers();

            //if (firstDisplayedIndexActive >= 0 && firstDisplayedIndexActive < dataGridViewActiveCustomers.Rows.Count)
            //{
            //    dataGridViewActiveCustomers.FirstDisplayedScrollingRowIndex = firstDisplayedIndexActive;
            //}
            //if (firstDisplayedIndexArchived >= 0 && firstDisplayedIndexArchived < dataGridViewArchivedCustomers.Rows.Count)
            //{
            //    dataGridViewArchivedCustomers.FirstDisplayedScrollingRowIndex = firstDisplayedIndexArchived;
            //}
            // --- Preserve state for dataGridViewActiveCustomers ---
            int? active_currentCell_RowIndex = dataGridViewActiveCustomers.CurrentCell?.RowIndex;
            int? active_currentCell_ColumnIndex = dataGridViewActiveCustomers.CurrentCell?.ColumnIndex;
            int active_firstDisplayedScrollingRowIndex = dataGridViewActiveCustomers.FirstDisplayedScrollingRowIndex;
            // If you want to preserve the exact selected rows (multi-select):
            // List<object> active_selectedDataBoundItems = dataGridViewActiveCustomers.SelectedRows
            //                                              .Cast<DataGridViewRow>()
            //                                              .Select(row => row.DataBoundItem)
            //                                              .Where(item => item != null)
            //                                              .ToList();

            // --- Preserve state for dataGridViewArchivedCustomers ---
            int? archived_currentCell_RowIndex = dataGridViewArchivedCustomers.CurrentCell?.RowIndex;
            int? archived_currentCell_ColumnIndex = dataGridViewArchivedCustomers.CurrentCell?.ColumnIndex;
            int archived_firstDisplayedScrollingRowIndex = dataGridViewArchivedCustomers.FirstDisplayedScrollingRowIndex;
            // List<object> archived_selectedDataBoundItems = dataGridViewArchivedCustomers.SelectedRows
            //                                               .Cast<DataGridViewRow>()
            //                                               .Select(row => row.DataBoundItem)
            //                                               .Where(item => item != null)
            //                                               .ToList();

            // --- Load data (this will clear selection and scroll) ---
            LoadActiveCustomers();
            LoadArchivedCustomers();

            // --- Restore state for dataGridViewActiveCustomers ---
            try
            {
                if (dataGridViewActiveCustomers.Rows.Count > 0)
                {
                    // 1. Restore current cell FIRST
                    if (active_currentCell_RowIndex.HasValue && active_currentCell_ColumnIndex.HasValue &&
                        active_currentCell_RowIndex.Value >= 0 && active_currentCell_RowIndex.Value < dataGridViewActiveCustomers.Rows.Count &&
                        active_currentCell_ColumnIndex.Value >= 0 && active_currentCell_ColumnIndex.Value < dataGridViewActiveCustomers.Columns.Count)
                    {
                        // It's good practice to clear any existing (default) selection before setting a new one.
                        dataGridViewActiveCustomers.ClearSelection();
                        dataGridViewActiveCustomers.CurrentCell = dataGridViewActiveCustomers.Rows[active_currentCell_RowIndex.Value].Cells[active_currentCell_ColumnIndex.Value];
                        // Setting CurrentCell usually also selects the row, especially with FullRowSelect.
                        // If you want to be absolutely sure the row is selected:
                        // dataGridViewActiveCustomers.Rows[active_currentCell_RowIndex.Value].Selected = true;
                    }

                    // 2. Then, restore scroll position. This should now be the dominant scroll command.
                    if (active_firstDisplayedScrollingRowIndex >= 0 && active_firstDisplayedScrollingRowIndex < dataGridViewActiveCustomers.Rows.Count)
                    {
                        dataGridViewActiveCustomers.FirstDisplayedScrollingRowIndex = active_firstDisplayedScrollingRowIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error restoring state for active customers grid: {ex.Message}");
            }

            // --- Restore state for dataGridViewArchivedCustomers ---
            try
            {
                if (dataGridViewArchivedCustomers.Rows.Count > 0)
                {
                    // 1. Restore current cell FIRST
                    if (archived_currentCell_RowIndex.HasValue && archived_currentCell_ColumnIndex.HasValue &&
                        archived_currentCell_RowIndex.Value >= 0 && archived_currentCell_RowIndex.Value < dataGridViewArchivedCustomers.Rows.Count &&
                        archived_currentCell_ColumnIndex.Value >= 0 && archived_currentCell_ColumnIndex.Value < dataGridViewArchivedCustomers.Columns.Count)
                    {
                        dataGridViewArchivedCustomers.ClearSelection();
                        dataGridViewArchivedCustomers.CurrentCell = dataGridViewArchivedCustomers.Rows[archived_currentCell_RowIndex.Value].Cells[archived_currentCell_ColumnIndex.Value];
                        // dataGridViewArchivedCustomers.Rows[archived_currentCell_RowIndex.Value].Selected = true; // Optional explicit selection
                    }

                    // 2. Then, restore scroll position
                    if (archived_firstDisplayedScrollingRowIndex >= 0 && archived_firstDisplayedScrollingRowIndex < dataGridViewArchivedCustomers.Rows.Count)
                    {
                        dataGridViewArchivedCustomers.FirstDisplayedScrollingRowIndex = archived_firstDisplayedScrollingRowIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error restoring state for archived customers grid: {ex.Message}");
            }
        }
        private void LoadActiveCustomers()
        {
            try
            {
                CustomerRepository repo = new CustomerRepository();
                // GetCustomers(false) will get only active customers.
                // The repository already sorts by RouterName, then AccountName.
                var customers = repo.GetCustomers(includeArchived: false);

                string search = txtSearchActive.Text.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(search))
                {
                    customers = customers.Where(c =>
                        (c.AccountName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.RouterName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.MacAddress?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.IPAddress?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.AdditionalName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.Location?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.Email?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.ContactNumber?.ToLowerInvariant().Contains(search) ?? false)
                    ).ToList();
                }
                dataGridViewActiveCustomers.DataSource = null;
                dataGridViewActiveCustomers.DataSource = customers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading active customers: " + ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadArchivedCustomers()
        {
            try
            {
                CustomerRepository repo = new CustomerRepository();
                // Get all customers and then filter locally for archived ones.
                // Or, modify GetCustomers to better support this, but for now, GetCustomers(true) gets all.
                var allCustomers = repo.GetCustomers(includeArchived: true);
                var customers = allCustomers.Where(c => c.IsArchived).ToList();

                string search = txtSearchArchived.Text.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(search))
                {
                    customers = customers.Where(c =>
                        (c.AccountName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.RouterName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.MacAddress?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.IPAddress?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.AdditionalName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.Location?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.Email?.ToLowerInvariant().Contains(search) ?? false) ||
                        (c.ContactNumber?.ToLowerInvariant().Contains(search) ?? false)
                    ).ToList();
                }
                dataGridViewArchivedCustomers.DataSource = null;
                dataGridViewArchivedCustomers.DataSource = customers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading archived customers: " + ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void timerSync_Tick(object sender, EventArgs e)
        {
            DateTime? lastSyncTime = null;
            string timeAgoStr = "error"; // Default to "error"

            try
            {
                ServiceLogRepository logRepo = new ServiceLogRepository(); // Assuming default constructor is okay
                NetwatchConfigRepository netwatchRepo = new NetwatchConfigRepository(logRepo); // Pass the logger

                lastSyncTime = netwatchRepo.GetLastDataSyncTimestamp("PPPoESyncService");
                timeAgoStr = FormatTimeAgo(lastSyncTime);

                // Optional: Log the fetched time for debugging this specific timer's action
                LogStatus($"timerSync_Tick (Customers Form): Fetched lastSyncTime='{lastSyncTime?.ToString("o") ?? "null"}', formatted timeAgoStr='{timeAgoStr}'");
            }
            catch (Exception ex)
            {
                LogStatus($"Error in Customers.timerSync_Tick fetching last sync time: {ex.Message}");
                // timeAgoStr remains "error" as set initially
            }

            // 3. Update lblsyncActive
            if (lblsyncActive != null)
            {
                lblsyncActive.Text = "Synced: " + timeAgoStr;
            }

            // 4. Update lblsyncArchived
            if (lblsyncArchived != null)
            {
                lblsyncArchived.Text = "Synced: " + timeAgoStr;
            }
        }

        // Helper method for logging status to UI or console (optional)
        private void LogStatus(string message)
        {
            System.Diagnostics.Debug.WriteLine($"CustomersForm: {message}");
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            LogStatus("Form Load: Initializing and loading customer data...");
            LoadActiveCustomers();
            LoadArchivedCustomers();
        }

        private void Customers_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Unsubscribe from the event when the form is closing
            CustomerRepository.CustomerUpdated -= OnCustomerDataUpdated;
        }
        private void Customers_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                LogStatus("Form became visible. Refreshing data views and sync status...");
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
        private string FormatTimeAgo(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return "never";
            }

            TimeSpan timeSpan = DateTime.Now - dateTime.Value;

            if (timeSpan.TotalSeconds < 1) // Handle future or same time as "just now"
            {
                return "just now";
            }
            if (timeSpan.TotalSeconds < 60)
            {
                return $"{Math.Floor(timeSpan.TotalSeconds)}s ago";
            }
            if (timeSpan.TotalMinutes < 60)
            {
                return $"{Math.Floor(timeSpan.TotalMinutes)}m ago";
            }
            if (timeSpan.TotalHours < 24)
            {
                return $"{Math.Floor(timeSpan.TotalHours)}h ago";
            }
            if (timeSpan.TotalDays < 30)
            {
                return $"{Math.Floor(timeSpan.TotalDays)}d ago";
            }
            if (timeSpan.TotalDays < 365)
            {
                return $"{Math.Floor(timeSpan.TotalDays / 30)}mo ago";
            }
            return $"{Math.Floor(timeSpan.TotalDays / 365)}y ago";
        }
    }

}
