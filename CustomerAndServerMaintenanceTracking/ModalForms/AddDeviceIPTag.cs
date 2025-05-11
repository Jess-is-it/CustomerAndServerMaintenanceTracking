using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddDeviceIPTag : Form
    {
        private OverlayForm overlayForm;
        private TagClass currentTag;

        // Event that is fired after a tag is successfully saved.
        public event EventHandler TagSaved;

        // Default constructor for add mode.
        public AddDeviceIPTag()
        {
            InitializeComponent();
            LoadClustersIntoComboBox();
        }
        private void LoadClustersIntoComboBox()
        {
            NetworkClusterRepository clusterRepo = new NetworkClusterRepository();
            List<NetworkCluster> clusters = clusterRepo.GetClusters();

            // Option B: Insert a "Select a cluster" placeholder.
            // Create a placeholder cluster object with Id = 0.
            NetworkCluster placeholder = new NetworkCluster
            {
                Id = 0,
                ClusterName = "Select a cluster",
                ClusterDescription = string.Empty
            };

            // Insert the placeholder at the beginning of the list.
            clusters.Insert(0, placeholder);

            // Bind the list to the ComboBox.
            cmbCluster.DataSource = clusters;
            cmbCluster.DisplayMember = "ClusterName";  // What is shown in the dropdown.
            cmbCluster.ValueMember = "Id";               // The value you’ll use (Cluster ID).
        }


        // This event handler should be wired to your Cancel button.
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddTag_Click(object sender, EventArgs e)
        { // Validate that the Tag Name field is not empty.
            if (string.IsNullOrWhiteSpace(txtTagName.Text))
            {
                MessageBox.Show("Please enter a Tag Name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create a new TagClass instance and set its properties.
            TagClass newTag = new TagClass()
            {
                TagName = txtTagName.Text.Trim(),
                TagDescription = txtTagDescription.Text.Trim(),
                IsParent = false,              // This tag is not a parent tag.
                TagType = "DeviceIP"           // IMPORTANT: Set TagType to "DeviceIP" so it is saved as a Device IP tag.
            };


            // Insert the new tag into the database using the repository.
            TagRepository tagRepo = new TagRepository();
            int newTagId = tagRepo.AddTagAndReturnId(newTag);

            // Raise the event so that the calling form can refresh its list.
            TagSaved?.Invoke(this, EventArgs.Empty);

            // Close the AddDeviceIPTag form.
            this.Close();
        }
    }
    
}
