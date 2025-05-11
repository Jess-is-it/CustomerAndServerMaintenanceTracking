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
    public partial class EditDeviceIPTag : Form
    {
        // Field to hold the tag being edited.
        private TagClass currentTag;

        // Event to notify the parent form that the tag was saved.
        public event EventHandler TagSaved;

        // --- Constructor ---
        // Accepts the TagClass object to be edited.
        public EditDeviceIPTag(TagClass tagToEdit)
        {
            InitializeComponent();

            // Store the passed-in tag. Throw an error if it's null.
            this.currentTag = tagToEdit ?? throw new ArgumentNullException(nameof(tagToEdit), "Tag to edit cannot be null.");

            // Check if the tag is actually a DeviceIP tag (optional safety check)
            if (currentTag.TagType != "DeviceIP")
            {
                MessageBox.Show("This form is only for editing DeviceIP tags.", "Incorrect Tag Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Optionally close the form immediately or disable save
                // this.Close();
                // btnUpdateTag.Enabled = false; // Assuming your save button is named btnUpdateTag
            }

            SetupFormForEdit();
            LoadClustersIntoComboBox(); // Load available clusters
            LoadDataForEdit();          // Populate form with the tag's current data
        }

        // --- Form Setup ---
        private void SetupFormForEdit()
        {
            // Update window title and button text for clarity
            this.Text = "Edit Device IP Tag";
            // If you have a Label control for the title (like label1 in Add form):
            // this.label1.Text = "Edit Device IP Tag";
            // Rename the save button in the Designer to something like btnUpdateTag
            this.btnUpdateTag.Text = "Update Tag"; // Make sure your button name matches
        }

        // --- Data Loading ---
        private void LoadClustersIntoComboBox()
        {
            NetworkClusterRepository clusterRepo = new NetworkClusterRepository();
            List<NetworkCluster> clusters = clusterRepo.GetClusters();
            NetworkCluster placeholder = new NetworkCluster
            {
                Id = 0, // Use 0 or -1 as a placeholder ID
                ClusterName = "Select a cluster",
                ClusterDescription = string.Empty
            };
            clusters.Insert(0, placeholder);

            cmbCluster.DataSource = clusters;
            cmbCluster.DisplayMember = "ClusterName";
            cmbCluster.ValueMember = "Id";
        }

        // Populate form controls with data from the currentTag
        private void LoadDataForEdit()
        {
            if (currentTag == null) return;

            txtTagName.Text = currentTag.TagName;
            txtTagDescription.Text = currentTag.TagDescription;

            // Load and set the currently assigned cluster
            NetworkClusterRepository clusterRepo = new NetworkClusterRepository();
            int? assignedClusterId = clusterRepo.GetClusterIdForTag(currentTag.Id);

            if (assignedClusterId.HasValue)
            {
                // Check if the assigned cluster ID actually exists in the ComboBox list
                bool clusterExists = ((List<NetworkCluster>)cmbCluster.DataSource)
                                     .Any(c => c.Id == assignedClusterId.Value);

                if (clusterExists)
                {
                    cmbCluster.SelectedValue = assignedClusterId.Value;
                }
                else
                {
                    // Handle case where assigned cluster is somehow invalid or not loaded
                    cmbCluster.SelectedValue = 0; // Select placeholder
                }
            }
            else
            {
                // No cluster assigned, select the placeholder
                cmbCluster.SelectedValue = 0; // Assuming 0 is the placeholder ID
            }
        }

        // --- Event Handlers ---

        // Handles the Cancel button click
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Handles the Update Tag button click
        private void btnUpdateTag_Click(object sender, EventArgs e) // Ensure button name matches
        {
            // Validate Tag Name
            if (string.IsNullOrWhiteSpace(txtTagName.Text))
            {
                MessageBox.Show("Please enter a Tag Name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ensure we have a tag to update
            if (currentTag == null)
            {
                MessageBox.Show("No tag loaded for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            TagRepository tagRepo = new TagRepository();

            try
            {
                // --- UPDATE TAG LOGIC ---
                // Update only the editable properties
                currentTag.TagName = txtTagName.Text.Trim();
                currentTag.TagDescription = txtTagDescription.Text.Trim();
                // IMPORTANT: Do NOT change IsParent or TagType here. Let UpdateTag handle saving them as they are.
                tagRepo.UpdateTag(currentTag);

                // --- UPDATE CLUSTER MAPPING ---
                int selectedClusterId = 0;
                if (cmbCluster.SelectedValue != null)
                {
                    selectedClusterId = (int)cmbCluster.SelectedValue;
                }

                using (SqlConnection conn = new DatabaseHelper().GetConnection())
                {
                    conn.Open();
                    // Remove any existing mapping for this tag first
                    SqlCommand cmdDelete = new SqlCommand("DELETE FROM ClusterTagMapping WHERE TagId = @TagId", conn);
                    cmdDelete.Parameters.AddWithValue("@TagId", currentTag.Id);
                    cmdDelete.ExecuteNonQuery();

                    // If a valid cluster was selected (not the placeholder), insert the new mapping
                    if (selectedClusterId != 0)
                    {
                        SqlCommand cmdInsert = new SqlCommand("INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@ClusterId, @TagId)", conn);
                        cmdInsert.Parameters.AddWithValue("@ClusterId", selectedClusterId);
                        cmdInsert.Parameters.AddWithValue("@TagId", currentTag.Id);
                        cmdInsert.ExecuteNonQuery();
                    }
                    conn.Close();
                }

                // Notify the parent form that the save was successful
                TagSaved?.Invoke(this, EventArgs.Empty);
                this.Close(); // Close the edit form

            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Database error updating tag: {sqlEx.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
    
}
