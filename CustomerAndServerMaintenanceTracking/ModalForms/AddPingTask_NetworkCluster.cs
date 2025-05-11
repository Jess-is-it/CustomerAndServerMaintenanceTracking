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
    public partial class AddPingTask_NetworkCluster : Form
    {
        // Event to notify that a router has been saved.
        public event EventHandler RouterSaved;

        public AddPingTask_NetworkCluster()
        {
            InitializeComponent();
            txtPingInterval.Text = "1000"; //default set to 1second
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void AddPingTags_Load(object sender, EventArgs e)
        {
            // Assume you have a TagRepository with GetAllTags() method.
            TagRepository tagRepo = new TagRepository();
            var tags = tagRepo.GetAllTags();

            //// Bind tags to DataGridView (assuming your DataGridView is named dgvTags)
            //dgvTags.DataSource = tags;

        }

        private void btnAddPing_Click(object sender, EventArgs e)
        {
            // 1) Validate pingName, pingInterval, etc.
            string pingNameBase = txtPingName.Text.Trim();
            if (string.IsNullOrEmpty(pingNameBase))
            {
                MessageBox.Show("Please enter a Ping Name.");
                return;
            }

            if (!int.TryParse(txtPingInterval.Text.Trim(), out int pingInterval) || pingInterval <= 0)
            {
                MessageBox.Show("Please enter a valid number for Ping Interval (milliseconds) > 0.");
                return;
            }

            // 2) Gather selected tag IDs from the DataGridView
            List<int> selectedTagIds = new List<int>();
            //foreach (DataGridViewRow row in dgvTags.Rows)
            //{
            //    bool isSelected = row.Cells["Select"].Value != null
            //                      && Convert.ToBoolean(row.Cells["Select"].Value);
            //    if (isSelected)
            //    {
            //        int parentTagId = Convert.ToInt32(row.Cells["Id"].Value);
            //        selectedTagIds.Add(parentTagId);
            //    }
            //}
            //if (selectedTagIds.Count == 0)
            //{
            //    MessageBox.Show("No tags selected.");
            //    return;
            //}

            // 2A) For each selected tag, gather its entire subtree (parent + child tags).
            TagRepository tagRepo = new TagRepository();
            List<int> allTagIds = new List<int>();
            foreach (int parentId in selectedTagIds)
            {
                // This method should return the parentId plus all child tags in the hierarchy.
                List<int> subtree = tagRepo.GetAllDescendantTagIds(parentId);
                allTagIds.AddRange(subtree);
            }
            // Remove duplicates in case multiple selected tags share child tags.
            allTagIds = allTagIds.Distinct().ToList();

            // 3) Retrieve all customers for allTagIds
            CustomerRepository custRepo = new CustomerRepository();
            List<Customer> customers = custRepo.GetCustomersByTagIds(allTagIds);
            if (customers.Count == 0)
            {
                MessageBox.Show("No customers found for the selected tag(s).");
                return;
            }

            // 4) Create ping tasks
            PingTaskRepository pingRepo = new PingTaskRepository();
            List<PingTask> createdTasks = new List<PingTask>();

            // We'll create tasks for each tag in the subtree. This way, child tags get tasks too.
            foreach (int tagId in allTagIds)
            {
                // Retrieve only the customers for this specific tagId
                List<Customer> custForTag = custRepo.GetCustomersByTagIds(new List<int> { tagId });
                foreach (var cust in custForTag)
                {
                    PingTask newTask = new PingTask
                    {
                        PingName = pingNameBase + " - " + cust.AccountName,
                        TargetIP = cust.IPAddress,
                        PingIntervalMs = pingInterval,
                        Status = "Pending",
                        CreatedDate = DateTime.Now,
                        TagId = tagId
                    };

                    pingRepo.AddPingTask(newTask);
                    createdTasks.Add(newTask);
                }
            }

            // 5) If "Run ping upon saving" is checked, open a PingTerminalForm for each newly created task
            if (chkRunPingUponSave.Checked)
            {
                foreach (var task in createdTasks)
                {
                    // 1 terminal = 1 IP => create a new form
                    // Use task.PingName for display, or pass the customer's name if you prefer
                    PingTerminalForm terminal = new PingTerminalForm(task.PingName, task.TargetIP, task.PingIntervalMs);
                    terminal.Show();
                }
            }

            MessageBox.Show($"Ping tasks created for {customers.Count} customers.");
            this.Close();
        }

    }
}
