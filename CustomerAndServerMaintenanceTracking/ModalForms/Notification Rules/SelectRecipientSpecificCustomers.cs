using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class SelectRecipientSpecificCustomers : Form
    {
        private List<SelectableCustomerItem> _allSelectableCustomers;
        private MikrotikRouterRepository _routerRepository;
        private CustomerRepository _customerRepository;

        // To store the final list of selected customer identifiers (e.g., their IDs)
        public List<int> SelectedCustomerIds { get; private set; }

        // To pre-load with existing selections if editing a rule
        private HashSet<int> _initiallySelectedCustomerIds;

        public SelectRecipientSpecificCustomers(List<int> initiallySelectedCustomerIds = null)
        {
            InitializeComponent();
            _routerRepository = new MikrotikRouterRepository();
            _customerRepository = new CustomerRepository();
            SelectedCustomerIds = new List<int>();
            _initiallySelectedCustomerIds = initiallySelectedCustomerIds != null ? new HashSet<int>(initiallySelectedCustomerIds) : new HashSet<int>();
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged);
            // Rename dataGridView1 to dgvCustomers in your designer for clarity
            // For now, I will use dataGridView1 as per your current designer.
            // If you rename it, change 'dataGridView1' to 'dgvCustomers' below.

            InitializeCustomersGrid();
            LoadRouters(); // This will also trigger LoadCustomers via SelectedIndexChanged if routers exist

            // Wire up events
            this.cmbSelectRouter.SelectedIndexChanged += new System.EventHandler(this.cmbSelectRouter_SelectedIndexChanged);
            this.btnInsertCustomer.Click += new System.EventHandler(this.btnInsertCustomer_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged);
            // this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCustomers_CellContentClick); // If using checkbox column for immediate effect
        }

        private void InitializeCustomersGrid()
        {
            dataGridView1.Columns.Clear(); // Use the actual name of your DataGridView
            dataGridView1.AutoGenerateColumns = false;

            DataGridViewCheckBoxColumn selectColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Select",
                Width = 50,
                DataPropertyName = "IsSelected" // We'll use a helper class for this
            };
            dataGridView1.Columns.Add(selectColumn);

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", DataPropertyName = "AccountName", HeaderText = "Account Name", Width = 150 });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "AdditionalName", DataPropertyName = "AdditionalName", HeaderText = "Additional Name", Width = 150 });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "IPAddress", DataPropertyName = "IPAddress", HeaderText = "IP Address", Width = 120 });

            dataGridView1.AllowUserToAddRows = false;
        }

        private void LoadRouters()
        {
            try
            {
                List<MikrotikRouter> routers = _routerRepository.GetRouters();

                // Add a "All Routers" option
                var placeholder = new MikrotikRouter { Id = 0, RouterName = "All Routers" };
                routers.Insert(0, placeholder);

                cmbSelectRouter.DataSource = null;
                cmbSelectRouter.DisplayMember = "RouterName";
                cmbSelectRouter.ValueMember = "Id";
                cmbSelectRouter.DataSource = routers;

                if (routers.Any())
                {
                    cmbSelectRouter.SelectedIndex = 0; // Default to "All Routers"
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading routers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCustomers()
        {
            try
            {
                List<Customer> customers;
                int selectedRouterId = 0;

                if (cmbSelectRouter.SelectedValue != null)
                {
                    selectedRouterId = (int)cmbSelectRouter.SelectedValue;
                }

                // Get all active customers from the repository first.
                customers = _customerRepository.GetCustomers(includeArchived: false);

                // If a specific router is selected (and not "All Routers"),
                // filter the list in memory.
                if (selectedRouterId != 0)
                {
                    customers = customers.Where(c => c.RouterId == selectedRouterId).ToList();
                }

                // Populate the master list for searching
                _allSelectableCustomers = customers.Select(c => new SelectableCustomerItem(c, _initiallySelectedCustomerIds.Contains(c.Id))).ToList();

                // Apply the filter (which will show all if search is empty)
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridView1.DataSource = null;
            }
        }

        private void cmbSelectRouter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCustomers();
        }

        private void btnInsertCustomer_Click(object sender, EventArgs e)
        {
            SelectedCustomerIds.Clear();
            if (dataGridView1.DataSource is List<SelectableCustomerItem> selectableItems)
            {
                foreach (var item in selectableItems)
                {
                    if (item.IsSelected)
                    {
                        SelectedCustomerIds.Add(item.Id);
                    }
                }
            }

            if (!SelectedCustomerIds.Any())
            {
                MessageBox.Show("No customers selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Optionally, don't set DialogResult.OK or don't close
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ApplyFilter()
        {
            if (_allSelectableCustomers == null) return;

            string searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<SelectableCustomerItem> filteredList;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredList = _allSelectableCustomers;
            }
            else
            {
                filteredList = _allSelectableCustomers
                    .Where(c => (c.AccountName?.ToLowerInvariant().Contains(searchText) ?? false) ||
                                (c.AdditionalName?.ToLowerInvariant().Contains(searchText) ?? false) ||
                                (c.IPAddress?.ToLowerInvariant().Contains(searchText) ?? false))
                    .ToList();
            }

            dataGridView1.DataSource = null;
            dataGridView1.DataSource = filteredList;
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // Helper class for DataGridView binding with a checkbox
        private class SelectableCustomerItem
        {
            private Customer _customer;
            public bool IsSelected { get; set; }

            public int Id => _customer.Id;
            public string AccountName => _customer.AccountName;
            public string AdditionalName => _customer.AdditionalName;
            public string IPAddress => _customer.IPAddress;
            // Add any other Customer properties you want to display

            public SelectableCustomerItem(Customer customer, bool isSelected)
            {
                _customer = customer;
                IsSelected = isSelected;
            }
        }
    }
}
