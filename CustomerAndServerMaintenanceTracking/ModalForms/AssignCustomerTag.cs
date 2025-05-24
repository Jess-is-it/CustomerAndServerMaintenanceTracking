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

namespace CustomerAndServerMaintenanceTracking
{
    public partial class AssignCustomerTag : Form
    {// Event to signal that tag assignments have been updated.
     //public event EventHandler TagAssignmentsUpdated;

        //// The tag for which assignments are being managed.
        //private TagClass currentTag;

        //public AssignCustomerTag(TagClass tag)
        //{
        //    InitializeComponent();
        //    currentTag = tag;
        //    // Load both customers and tags.
        //    LoadCustomers();

        //}

        //// Loads customers into the customers DataGridView.
        //private void LoadCustomers()
        //{
        //    try
        //    {
        //        CustomerRepository customerRepo = new CustomerRepository();
        //        List<Customer> customers = customerRepo.GetCustomers();

        //        string search = txtSearchCustomers.Text.Trim();
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            customers = customers
        //                .Where(c => c.AccountName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
        //                .ToList();
        //        }
        //        dataGridViewCustomers.DataSource = customers;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error loading customers: " + ex.Message);
        //    }
        //}


        //// Event handler for changes in the customer search TextBox.
        //private void txtSearchCustomers_TextChanged(object sender, EventArgs e)
        //{
        //    LoadCustomers();
        //}


        //// Click event for the "Assign" button.
        //private void btnAssign_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        TagRepository tagRepo = new TagRepository();

        //        // Loop through each row in the customers DataGridView.
        //        foreach (DataGridViewRow row in dataGridViewCustomers.Rows)
        //        {
        //            bool isSelected = false;
        //            if (row.Cells["SelectCustomer"].Value != null)
        //            {
        //                isSelected = Convert.ToBoolean(row.Cells["SelectCustomer"].Value);
        //            }
        //            if (isSelected)
        //            {
        //                Customer customer = (Customer)row.DataBoundItem;
        //                tagRepo.AssignTagToCustomer(customer.Id, currentTag.Id);
        //            }
        //        }


        //        // Fire event to notify parent form.
        //        TagAssignmentsUpdated?.Invoke(this, EventArgs.Empty);
        //        MessageBox.Show("Tag assignments updated successfully.");
        //        this.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error assigning tag: " + ex.Message);
        //    }
        //}

        //// Click event for the Cancel button.
        //private void btnCancel_Click(object sender, EventArgs e)
        //{
        //    this.Close();
        //}

        // Event to signal that assignments have been updated.
        public event EventHandler TagAssignmentsUpdated; // Renamed for clarity

        private TagClass currentTag;
        private TagRepository tagRepo; // Use class-level instance
        private CustomerRepository customerRepo; // Use class-level instance

        // Helper class for DataGridView binding
        private class SelectableCustomer
        {
            public Customer Customer { get; set; }
            public bool IsSelected { get; set; }

            // Expose Customer properties
            public int Id => Customer.Id;
            public string AccountName => Customer.AccountName;
            public string AdditionalName => Customer.AdditionalName;
            public string ContactNumber => Customer.ContactNumber;
            public string Email => Customer.Email;
            public string Location => Customer.Location;
            public string IPAddress => Customer.IPAddress; // Include IPAddress if needed
            // Don't expose IsArchived unless you want to show it here
        }

        public AssignCustomerTag(TagClass tag)
        {
            InitializeComponent();
            currentTag = tag ?? throw new ArgumentNullException(nameof(tag));
            tagRepo = new TagRepository();
            customerRepo = new CustomerRepository();

            // Update form title
            this.label1.Text = $"Assign Customers to Tag: {currentTag.TagName}"; // Use the label from your designer

            // Setup Grid Columns
            InitializeCustomerGrid();

            // Load customers and check existing assignments
            LoadCustomersAndAssignments();

            // Wire up search
            txtSearchCustomers.TextChanged += TxtSearchCustomers_TextChanged;
        }

        // Method to set up DataGridView columns for Customer data + Select checkbox
        private void InitializeCustomerGrid()
        {
            dataGridViewCustomers.Columns.Clear(); // Use the correct DataGridView name from your designer
            dataGridViewCustomers.AutoGenerateColumns = false;

            // Select CheckBox Column
            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "SelectCustomer", // Match name used in old code if necessary
                HeaderText = "Select",
                DataPropertyName = "IsSelected", // Binds to SelectableCustomer.IsSelected
                Width = 50
            };
            dataGridViewCustomers.Columns.Add(colSelect);

            // Customer Columns (add only those you want to see)
            var colId = new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Visible = false };
            var colAccount = new DataGridViewTextBoxColumn { DataPropertyName = "AccountName", HeaderText = "Account Name", Width = 150 };
            var colAddName = new DataGridViewTextBoxColumn { DataPropertyName = "AdditionalName", HeaderText = "Additional Name", Width = 150 };
            var colContact = new DataGridViewTextBoxColumn { DataPropertyName = "ContactNumber", HeaderText = "Contact", Width = 120 };
            var colEmail = new DataGridViewTextBoxColumn { DataPropertyName = "Email", HeaderText = "Email", Width = 150 };
            var colLoc = new DataGridViewTextBoxColumn { DataPropertyName = "Location", HeaderText = "Location", Width = 150 };
            var colIP = new DataGridViewTextBoxColumn { DataPropertyName = "IPAddress", HeaderText = "IP Address", Width = 120 };


            dataGridViewCustomers.Columns.AddRange(new DataGridViewColumn[] {
                colId, colAccount, colAddName, colContact, colEmail, colLoc, colIP
            });
        }

        // Renamed and modified method to load customers and check assignments
        private void LoadCustomersAndAssignments()
        {
            try
            {
                List<Customer> allCustomers = customerRepo.GetCustomers()
                                                        .Where(c => !c.IsArchived) // Only show active customers
                                                        .ToList();
                List<int> currentlyAssignedCustomerIds = tagRepo.GetAssignedCustomerIds(currentTag.Id);

                // Filter based on search text
                string search = txtSearchCustomers.Text.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    allCustomers = allCustomers
                        .Where(c => (c.AccountName != null && c.AccountName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (c.AdditionalName != null && c.AdditionalName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (c.ContactNumber != null && c.ContactNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (c.IPAddress != null && c.IPAddress.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)) // Add more fields if needed
                        .ToList();
                }

                // Create SelectableCustomer objects and pre-check assigned ones
                var selectableCustomers = allCustomers.Select(cust => new SelectableCustomer
                {
                    Customer = cust,
                    IsSelected = currentlyAssignedCustomerIds.Contains(cust.Id)
                }).ToList();

                // Bind to the DataGridView
                dataGridViewCustomers.DataSource = null; // Use the correct DataGridView name
                dataGridViewCustomers.DataSource = selectableCustomers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading customers: " + ex.Message);
            }
        }

        // Event handler for search text changes
        private void TxtSearchCustomers_TextChanged(object sender, EventArgs e)
        {
            LoadCustomersAndAssignments();
        }

        // Click event for the "Assign Tag" button (now acts as Update)
        private void btnAssign_Click(object sender, EventArgs e)
        {
            try
            {
                List<int> originalAssignedIds = tagRepo.GetAssignedCustomerIds(currentTag.Id);
                HashSet<int> originalSet = new HashSet<int>(originalAssignedIds); // For efficient lookup

                List<int> idsToAssign = new List<int>();
                List<int> idsToRemove = new List<int>();

                // Loop through rows to determine changes
                foreach (DataGridViewRow row in dataGridViewCustomers.Rows)
                {
                    if (row.DataBoundItem is SelectableCustomer item)
                    {
                        bool currentlySelectedInGrid = Convert.ToBoolean(row.Cells["SelectCustomer"].Value ?? false);
                        bool wasOriginallySelected = originalSet.Contains(item.Id);

                        if (currentlySelectedInGrid && !wasOriginallySelected)
                        {
                            idsToAssign.Add(item.Id); // Add if selected now but wasn't before
                        }
                        else if (!currentlySelectedInGrid && wasOriginallySelected)
                        {
                            idsToRemove.Add(item.Id); // Remove if not selected now but was before
                        }
                    }
                }

                // Perform database updates
                foreach (int customerId in idsToAssign)
                {
                    tagRepo.AssignTagToCustomer(customerId, currentTag.Id);
                }
                foreach (int customerId in idsToRemove)
                {
                    tagRepo.RemoveTagFromCustomer(customerId, currentTag.Id);
                }

                // Notify parent form and close
                TagAssignmentsUpdated?.Invoke(this, EventArgs.Empty);
                //MessageBox.Show("Customer assignments updated successfully."); // Optional
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating customer assignments: " + ex.Message);
            }
        }

        // Click event for the Cancel button
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
