using CustomerAndServerMaintenanceTracking.CustomCells;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.Models;
using CustomerAndServerMaintenanceTracking.Services;
using CustomerAndServerMaintenanceTracking.UserControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class NetwatchAdd: Form
    {
        private OverlayForm overlayForm;
        private NetworkClusterRepository _networkClusterRepository;
        public event EventHandler ConfigSaved;

        public NetwatchAdd()
        {
            InitializeComponent();
            _networkClusterRepository = new NetworkClusterRepository();
            DataGridViewTextBoxColumn actionCol = new DataGridViewTextBoxColumn();
            actionCol.Name = "Action";          // Must match exactly
            actionCol.HeaderText = "Action";
            //dgvTags.Columns.Add(actionCol);

        }
        private void Overlay()
        {
            overlayForm = new OverlayForm();
            overlayForm.StartPosition = FormStartPosition.Manual;
            // Set the overlay to cover the entire Dashboard form.
            // If Dashboard is your main form, "this" refers to it.
            overlayForm.Bounds = this.Bounds;
            overlayForm.Show();
        }
        private void Netwatch_Load(object sender, EventArgs e)
        {
            // --- Start: Dynamic Tab Creation ---
            tabControlNetworkCluster.TabPages.Clear(); // Clear any tabs potentially added in the designer

            // Fetch and add dynamic Network Cluster tabs
            try
            {
                List<NetworkCluster> clusters = _networkClusterRepository.GetClusters(); // Use the repository instance
                foreach (var cluster in clusters)
                {
                    TabPage clusterTab = new TabPage();
                    clusterTab.Name = "tpCluster_" + cluster.Id; // Use ID for a unique name
                    clusterTab.Text = cluster.ClusterName;       // Set the tab text to the cluster name
                    clusterTab.Tag = cluster.Id;                 // Store cluster ID for potential later use

                    // --- Start: Add User Control ---
                    // Create an instance of your User Control
                    UC_NetworkCluster_Netwatch clusterPingControl = new UC_NetworkCluster_Netwatch();

                    // Set its Dock property to fill the TabPage
                    clusterPingControl.Dock = DockStyle.Fill;

                    // **Important:** Pass the Cluster ID to the User Control.
                    // Assuming UC_NetworkCluster_Ping has a public property named 'ClusterId'.
                    // You might need to add this property to your UC_NetworkCluster_Ping.cs file.
                    clusterPingControl.ClusterId = cluster.Id;

                    // Add the User Control to the TabPage's controls
                    clusterTab.Controls.Add(clusterPingControl);
                    // --- End: Add User Control ---

                    tabControlNetworkCluster.TabPages.Add(clusterTab); // Add the tab *after* adding the control to it
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading network clusters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Handle the error appropriately, maybe log it
            }
            LoadTags();
            SetupTagsGrid();
        }
        private void SetupTagsGrid()
        {
            //dgvTags.AutoGenerateColumns = false;
           // dgvTags.Columns.Clear();

            // 1) TagName column
            var tagNameCol = new DataGridViewTextBoxColumn
            {
                Name = "TagName",
                HeaderText = "Tag Name",
                DataPropertyName = "TagName"  // MUST match property in TagPingDisplay
            };
           // dgvTags.Columns.Add(tagNameCol);

            // 2) Entity column
            var entityCol = new DataGridViewTextBoxColumn
            {
                Name = "Entity",
                HeaderText = "Entity",
                DataPropertyName = "Entity"   // MUST match TagPingDisplay.Entity
            };
           // dgvTags.Columns.Add(entityCol);

            // 3) RTO Entities column
            var rtoCol = new DataGridViewTextBoxColumn
            {
                Name = "RtoEntitiesToday",
                HeaderText = "RTO Entities Today",
                DataPropertyName = "RtoEntitiesToday" // MUST match TagPingDisplay.RtoEntitiesToday
            };
           // dgvTags.Columns.Add(rtoCol);

            // 4) Status column
            var statusCol = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "Status"   // MUST match TagPingDisplay.Status
            };
           // dgvTags.Columns.Add(statusCol);

            // 5) Action column (no DataPropertyName needed if you replace cells w/ custom multi-button cell)
            var actionCol = new DataGridViewTextBoxColumn
            {
                Name = "Action",
                HeaderText = "Action"
            };
           // dgvTags.Columns.Add(actionCol);
        }
     
        private void LoadTags(string searchTerm = "")
        {
            TagRepository tagRepo = new TagRepository();
            var allTags = tagRepo.GetAllTags(); // returns List<TagClass> or similar

            // If searchTerm is not empty, filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                allTags = allTags
                    .Where(t => t.TagName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            //dgvTags.DataSource = null;     // Clear old binding
            //dgvTags.DataSource = allTags;  // Bind new data
        }

    }
}

