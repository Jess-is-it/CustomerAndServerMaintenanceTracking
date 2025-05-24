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
    public partial class EditTag : Form
    {
        private OverlayForm overlayForm;
        private TagClass currentTag;

        // Event that is fired after a tag is successfully saved.
        public event EventHandler TagSaved;

        // Default constructor for add mode.
        public EditTag()
        {
            InitializeComponent();
        }

        public EditTag(TagClass tag)
        {
            InitializeComponent();
            currentTag = tag;

            // Pre-fill text boxes with the current tag details.
            txtTagName.Text = tag.TagName;
            txtTagDescription.Text = tag.TagDescription;

            LoadClustersIntoComboBox();
            LoadTagClusterAssignment();
        }

        private void LoadClustersIntoComboBox()
        {
            NetworkClusterRepository clusterRepo = new NetworkClusterRepository();
            List<NetworkCluster> clusters = clusterRepo.GetClusters();

            // Option B: Insert a "Select a cluster" placeholder.
            NetworkCluster placeholder = new NetworkCluster
            {
                Id = 0,
                ClusterName = "Select a cluster",
                ClusterDescription = string.Empty
            };

            clusters.Insert(0, placeholder);

            cmbCluster.DataSource = clusters;
            cmbCluster.DisplayMember = "ClusterName";
            cmbCluster.ValueMember = "Id";
        }

        private void LoadTagClusterAssignment()
        {
            NetworkClusterRepository clusterRepo = new NetworkClusterRepository();
            int? assignedClusterId = clusterRepo.GetClusterIdForTag(currentTag.Id);

            if (assignedClusterId.HasValue)
            {
                cmbCluster.SelectedValue = assignedClusterId.Value;
            }
            else
            {
                cmbCluster.SelectedIndex = 0; // Select placeholder if none assigned.
            }
        }


        // This event handler should be wired to your Cancel button.
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddTag_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTagName.Text))
            {
                MessageBox.Show("Please enter a tag name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TagRepository tagRepo = new TagRepository();

            // Edit mode: Update the existing tag.
            currentTag.TagName = txtTagName.Text.Trim();
            currentTag.TagDescription = txtTagDescription.Text.Trim();
            currentTag.TagType = "Customer";
            tagRepo.UpdateTag(currentTag);

            // Now update the cluster assignment:
            int selectedClusterId = (int)cmbCluster.SelectedValue;
            if (selectedClusterId == 0)
            {
                // Option: Warn user if no cluster selected, or leave as is.
                MessageBox.Show("Please select a valid cluster.", "Cluster Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                // Update the mapping.
                // First, remove any existing mapping for this tag.
                using (SqlConnection conn = new DatabaseHelper().GetConnection())
                {
                    conn.Open();
                    SqlCommand cmdDelete = new SqlCommand("DELETE FROM ClusterTagMapping WHERE TagId = @TagId", conn);
                    cmdDelete.Parameters.AddWithValue("@TagId", currentTag.Id);
                    cmdDelete.ExecuteNonQuery();

                    // Now insert the new mapping.
                    SqlCommand cmdInsert = new SqlCommand("INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@ClusterId, @TagId)", conn);
                    cmdInsert.Parameters.AddWithValue("@ClusterId", selectedClusterId);
                    cmdInsert.Parameters.AddWithValue("@TagId", currentTag.Id);
                    cmdInsert.ExecuteNonQuery();

                    conn.Close();
                }
            }

            // Fire the TagSaved event to notify parent form to refresh.
            TagSaved?.Invoke(this, EventArgs.Empty);

            // Close the EditTag form.
            this.Close();


        }
    }
}
