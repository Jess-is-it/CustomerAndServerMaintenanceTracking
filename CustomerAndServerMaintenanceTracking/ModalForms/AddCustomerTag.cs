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
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddCustomerTag: Form
    {
        private OverlayForm overlayForm;
        private TagClass currentTag;

        // Event that is fired after a tag is successfully saved.
        public event EventHandler TagSaved;

        // Default constructor for add mode.
        public AddCustomerTag()
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
        {// Validate that a tag name is provided.
            if (string.IsNullOrWhiteSpace(txtTagName.Text))
            {
                MessageBox.Show("Please enter a tag name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TagRepository tagRepo = new TagRepository();

            if (currentTag == null)
            {
                // Add mode: Create a new tag object.
                TagClass newTag = new TagClass()
                {
                    TagName = txtTagName.Text.Trim(),
                    TagDescription = txtTagDescription.Text.Trim(),
                    IsParent = false, // Mark this tag as not a parent.
                    TagType = "Customer"
                };

                // Insert tag and return its new Id.
                int newTagId = tagRepo.AddTagAndReturnId(newTag);

                // Check if the user selected a valid cluster.
                int selectedClusterId = (int)cmbCluster.SelectedValue;
                if (selectedClusterId != 0)
                {
                    using (SqlConnection conn = new DatabaseHelper().GetConnection())
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@ClusterId, @TagId)", conn);
                        cmd.Parameters.AddWithValue("@ClusterId", selectedClusterId);
                        cmd.Parameters.AddWithValue("@TagId", newTagId);
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
                else
                {
                    // Optionally, warn the user if they didn't select a cluster
                    MessageBox.Show("Please select a network cluster.", "Cluster Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                // Edit mode: Update the existing tag.
                currentTag.TagName = txtTagName.Text.Trim();
                currentTag.TagDescription = txtTagDescription.Text.Trim();
                tagRepo.UpdateTag(currentTag);
            }

            // Fire the TagSaved event to notify parent form to refresh.
            TagSaved?.Invoke(this, EventArgs.Empty);

            // Close the AddTag form.
            this.Close();
        }
    }
}
