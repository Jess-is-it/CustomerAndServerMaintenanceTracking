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
    public partial class ViewCustomerTag : Form
    {
        private TagClass currentTag;
        public event EventHandler CustomerAssignmentsUpdated;

        public ViewCustomerTag(TagClass tag)
        {
            InitializeComponent();
            currentTag = tag;
            LoadEntities();
        }

        private void LoadEntities()
        {
            TagRepository tagRepo = new TagRepository();
            List<string> assignedEntities = tagRepo.GetAssignedEntities(currentTag.Id);

            // For simplicity, let's store them in a simple list of strings,
            // though you could separate customers from tags if needed.
            // We'll filter based on txtSearch if you have such a text box.

            string search = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                assignedEntities = assignedEntities
                    .Where(x => x.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            dataGridViewEntities.DataSource = assignedEntities
                .Select(x => new { Name = x })
                .ToList(); // minimal example
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadEntities();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            //// Open the EditAssignedTag form to modify assignments
            //EditAssignedTag editForm = new EditAssignedTag(currentTag);
            //editForm.AssignedTagEdited += (s, ea) =>
            //{
            //    // After editing, reload the list
            //    LoadEntities();
            //};
            //editForm.StartPosition = FormStartPosition.CenterScreen;
            //editForm.ShowDialog();
            // Ensure we have a valid tag loaded
            if (currentTag == null)
            {
                MessageBox.Show("No tag selected or loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- CHANGE HERE: Open AssignCustomerTag instead of EditAssignedTag ---
            AssignCustomerTag assignForm = new AssignCustomerTag(currentTag);

            // Subscribe to the event to refresh this view form when assignments are saved
            assignForm.TagAssignmentsUpdated += (s, ea) =>
            {
                // After editing assignments, reload the entity list in this view form
                LoadEntities();
                CustomerAssignmentsUpdated?.Invoke(this, EventArgs.Empty); // Notify TagForm
            };
            

            assignForm.StartPosition = FormStartPosition.CenterScreen;
            assignForm.ShowDialog(); // Show the assignment form modally
        }

    }
}
