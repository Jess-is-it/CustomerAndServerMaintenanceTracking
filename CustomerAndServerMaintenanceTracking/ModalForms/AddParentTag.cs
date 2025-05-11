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

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddParentTag : Form
    {

        // This event notifies that a new parent tag was added.
        public event EventHandler ParentTagAdded;
        // This property will be set from ManageTagTree so we know which cluster to assign the tag to.
        public int CurrentClusterId { get; set; }

        // Event that is fired after a tag is successfully saved.
        public event EventHandler TagSaved;

        // Default constructor for add mode.
        public AddParentTag()
        {
            InitializeComponent();
        }

        // This event handler should be wired to your Cancel button.
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddTag_Click(object sender, EventArgs e)
        {
            // Step 0: Declare the repository first
            TagRepository tagRepo = new TagRepository();

            // Step 1: Capture the new tag name
            string newTagName = txtTagName.Text.Trim();

            // Step 2: Check if the tag name already exists
            TagClass existing = tagRepo.GetAllTags()
                                       .FirstOrDefault(t => t.TagName.Equals(newTagName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                MessageBox.Show("A tag with that name already exists.");
                return;
            }

            // Step 3: Create a new TagClass object
            TagClass newTag = new TagClass()
            {
                TagName = newTagName,
                TagDescription = txtTagDescription.Text.Trim(),
                IsParent = true
            };

            // Step 4: Insert the new tag and retrieve its new ID
            int newTagId = tagRepo.AddTagAndReturnId(newTag);

            // Step 5: Insert a mapping row for the cluster -> new tag association
            using (SqlConnection conn = new DatabaseHelper().GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@ClusterId, @TagId)", conn);
                cmd.Parameters.AddWithValue("@ClusterId", CurrentClusterId);
                cmd.Parameters.AddWithValue("@TagId", newTagId);
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            // Step 6: Notify listeners and close
            ParentTagAdded?.Invoke(this, EventArgs.Empty);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
