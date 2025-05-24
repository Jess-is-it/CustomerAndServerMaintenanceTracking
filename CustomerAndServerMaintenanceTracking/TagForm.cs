using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;


namespace CustomerAndServerMaintenanceTracking
{
    public partial class TagForm: Form, IRefreshableForm
    {
       private AddCustomerTag addTagFormmodalFormTag;
        private OverlayForm overlayForm;

        // Dictionary to store each tag's collapse state (true = collapsed, false = normal)
        private Dictionary<int, bool> tagCollapseState = new Dictionary<int, bool>();

        public void RefreshDataViews()
        {
            LoadCustomerTags();
            LoadDeviceIPTags();
        }

        public TagForm(ServiceLogRepository logRepository, TagRepository tagRepository)
        {
            InitializeComponent();

            #region Customer TAG
            LoadCustomerTags();
            InitializeCustomerTagDataGridView();
            btnAddTagCustomer.Click += btnAddTagCustomer_Click;
            txtSearchCustomerTag.TextChanged += txtSearchCustomerTag_TextChanged;
            dataGridCustomerViewTags.CellPainting += dataGridViewCustomerTags_CellPainting;
            dataGridCustomerViewTags.CellClick += dataGridViewCustomerTags_CellClick;
            dataGridCustomerViewTags.DataBindingComplete += dataGridViewCustomerTags_DataBindingComplete;
            #endregion
            #region DeviceIP TAG
            LoadDeviceIPTags();
            InitializeDeviceIPTagDataGridView();
            btnAddTagDeviceIP.Click += btnAddTagDeviceIP_Click;
            txtSearchDeviceIPTag.TextChanged += txtSearchDeviceIPTag_TextChanged;
            dataGridViewDeviceIPTags.CellPainting += dataGridViewDeviceIPTags_CellPainting;
            dataGridViewDeviceIPTags.CellClick += dataGridViewDeviceIPTags_CellClick;
            dataGridViewDeviceIPTags.DataBindingComplete += dataGridViewDeviceIPTags_DataBindingComplete;
            #endregion
        }

        // Helper method to find the MDIClient control.
        private MdiClient GetMdiClient()
        {
            foreach (Control ctl in this.Controls)
            {
                if (ctl is MdiClient)
                {
                    return (MdiClient)ctl;
                }
            }
            return null;
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

        #region Customer TAG
        private void LoadCustomerTags()
        {
            try
            {
                TagRepository tagRepo = new TagRepository();
                // Filter out tags that are marked as parents.
                var tags = tagRepo.GetAllTagsWithCluster()
                   .Where(t => !t.IsParent && t.TagType == "Customer") // Added TagType filter
                   .ToList();
                dataGridCustomerViewTags.DataSource = tags;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading tags: " + ex.Message);
            }
        }
        private void InitializeCustomerTagDataGridView()
        {
            dataGridCustomerViewTags.Columns.Clear();
            dataGridCustomerViewTags.AutoGenerateColumns = false;

            // (1) ID column
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.DataPropertyName = "Id";
            idColumn.HeaderText = "ID";
            dataGridCustomerViewTags.Columns.Add(idColumn);

            // (2) TagName column
            DataGridViewTextBoxColumn tagNameColumn = new DataGridViewTextBoxColumn();
            tagNameColumn.DataPropertyName = "TagName";
            tagNameColumn.HeaderText = "Tag Name";
            dataGridCustomerViewTags.Columns.Add(tagNameColumn);

            // (3) TagDescription column
            DataGridViewTextBoxColumn tagDescColumn = new DataGridViewTextBoxColumn();
            tagDescColumn.DataPropertyName = "TagDescription";
            tagDescColumn.HeaderText = "Tag Description";
            dataGridCustomerViewTags.Columns.Add(tagDescColumn);

            // In your method that initializes columns (e.g., InitializeTagDataGridView)
            DataGridViewTextBoxColumn networkClusterColumn = new DataGridViewTextBoxColumn();
            networkClusterColumn.DataPropertyName = "NetworkCluster";
            networkClusterColumn.HeaderText = "Network Cluster";
            networkClusterColumn.Name = "NetworkClusterColumn";
            networkClusterColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridCustomerViewTags.Columns.Add(networkClusterColumn);


            // (4) TaggedEntity column (unbound)
            DataGridViewTextBoxColumn taggedEntityColumn = new DataGridViewTextBoxColumn();
            taggedEntityColumn.HeaderText = "Tagged Entity";
            taggedEntityColumn.Name = "TaggedEntity";
            dataGridCustomerViewTags.Columns.Add(taggedEntityColumn);

            // (5) Action column (unbound) for Edit/Delete buttons
            DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn();
            actionColumn.HeaderText = "Action";
            actionColumn.Name = "Action";
            actionColumn.ReadOnly = true;
            dataGridCustomerViewTags.Columns.Add(actionColumn);
        }
        private void btnAddTagCustomer_Click(object sender, EventArgs e)
        {
            Overlay();
            AddCustomerTag addTagForm = new AddCustomerTag();
            addTagForm.Owner = this;
            addTagForm.StartPosition = FormStartPosition.CenterScreen;
            addTagForm.TagSaved += (s, ea) =>
            {
                // Refresh tag list after a new tag is added.
                LoadCustomerTags();
            };

            // Show the overlay form modelessly.
            addTagForm.ShowDialog();
            overlayForm.Close();

        }
        private void txtSearchCustomerTag_TextChanged(object sender, EventArgs e)
        {
            LoadCustomerTags();
        }
        private void dataGridViewCustomerTags_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex == dataGridCustomerViewTags.Columns["Action"].Index && e.RowIndex >= 0)
            {
                e.PaintBackground(e.ClipBounds, true);
                // Calculate button dimensions.
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                int buttonHeight = e.CellBounds.Height - 4;
                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                // Draw the "Edit" button.
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                // Draw the "Delete" button.
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }

        }
        private void dataGridViewCustomerTags_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // Cast the DataBoundItem to TagDisplayModel instead of TagClass.
            TagDisplayModel displayItem = dataGridCustomerViewTags.Rows[e.RowIndex].DataBoundItem as TagDisplayModel;
            if (displayItem == null) return;

            // Retrieve the full TagClass using the ID.
            TagRepository tagRepo = new TagRepository();
            TagClass selectedTag = tagRepo.GetTagById(displayItem.Id);
            if (selectedTag == null) return;

            string columnName = dataGridCustomerViewTags.Columns[e.ColumnIndex].Name;
            if (columnName == "TaggedEntity")
            {
                string cellValue = dataGridCustomerViewTags.Rows[e.RowIndex].Cells["TaggedEntity"].Value?.ToString();
                if (cellValue == "Assign Customer >>")
                {
                    AssignCustomerTag assignTagForm = new AssignCustomerTag(selectedTag);
                    assignTagForm.TagAssignmentsUpdated += (s, ea) => LoadCustomerTags();
                    assignTagForm.StartPosition = FormStartPosition.CenterScreen;
                    Overlay();
                    assignTagForm.ShowDialog();
                    overlayForm.Close();
                }
                else
                {
                    ViewCustomerTag viewForm = new ViewCustomerTag(selectedTag);
                    viewForm.CustomerAssignmentsUpdated += ViewForm_CustomerAssignmentsUpdated; // Subscribe to the new event
                    Overlay();
                    viewForm.StartPosition = FormStartPosition.CenterScreen;
                    viewForm.ShowDialog();
                    overlayForm.Close();
                }
            }
            else if (columnName == "Action")
            {
                Rectangle cellRect = dataGridCustomerViewTags.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridCustomerViewTags.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;
                if (relativeX < buttonWidth + 2)
                {
                    // Edit
                    EditTag editTagForm = new EditTag(selectedTag);
                    editTagForm.TagSaved += (s, ea) => LoadCustomerTags();
                    editTagForm.StartPosition = FormStartPosition.CenterScreen;
                    Overlay();
                    editTagForm.ShowDialog();
                    overlayForm.Close();
                }
                else
                {
                    if (MessageBox.Show("Are you sure you want to delete this tag?",
                        "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        tagRepo.DeleteTag(selectedTag.Id);
                        LoadCustomerTags();
                    }
                }
            }
        }
        private void dataGridViewCustomerTags_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            TagRepository tagRepo = new TagRepository();
            foreach (DataGridViewRow row in dataGridCustomerViewTags.Rows)
            {
                TagDisplayModel displayItem = row.DataBoundItem as TagDisplayModel;
                if (displayItem == null)
                    continue;

                TagClass realTag = tagRepo.GetTagById(displayItem.Id);
                if (realTag == null)
                    continue;

                List<string> assignedEntities = tagRepo.GetAssignedEntities(realTag.Id);
                row.Cells["TaggedEntity"].Value = (assignedEntities == null || assignedEntities.Count == 0)
                    ? "Assign Customer >>"
                    : $"({assignedEntities.Count}) Customers";
            }
        }
        private void ViewForm_CustomerAssignmentsUpdated(object sender, EventArgs e)
        {
            // Refresh the main grid in TagForm
            LoadCustomerTags();
        }
        #endregion

        #region DeviceIP TAG
        private void LoadDeviceIPTags()
        {
            TagRepository tagRepo = new TagRepository();
            var allTags = tagRepo.GetAllTagsWithCluster(); // or your variant

            var deviceIpTags = allTags
                .Where(t => t.TagType == "DeviceIP" && t.IsParent == false)
                .ToList();


            // Optionally filter by txtSearchDeviceIPTag
            string search = txtSearchDeviceIPTag.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                allTags = allTags
                    .Where(t => t.TagName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            dataGridViewDeviceIPTags.DataSource = null;
            dataGridViewDeviceIPTags.DataSource = deviceIpTags;
        }
        private void InitializeDeviceIPTagDataGridView()
        {
            dataGridViewDeviceIPTags.Columns.Clear();
            dataGridViewDeviceIPTags.AutoGenerateColumns = false;

            // ID column
            var colId = new DataGridViewTextBoxColumn()
            {
                Name = "Id",
                HeaderText = "ID",
                DataPropertyName = "Id",
                Width = 50
            };
            dataGridViewDeviceIPTags.Columns.Add(colId);

            // TagName
            var colTagName = new DataGridViewTextBoxColumn()
            {
                Name = "TagName",
                HeaderText = "Tag Name",
                DataPropertyName = "TagName",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewDeviceIPTags.Columns.Add(colTagName);

            // TagDescription
            var colTagDescription = new DataGridViewTextBoxColumn()
            {
                Name = "TagDescription",
                HeaderText = "Tag Description",
                DataPropertyName = "TagDescription", // <--- CRITICAL: Match property name
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewDeviceIPTags.Columns.Add(colTagDescription);

            // Network Cluster column
            var colNetworkCluster = new DataGridViewTextBoxColumn()
            {
                Name = "NetworkClusterColumn", // Unique name for the column itself
                HeaderText = "Network Cluster",
                DataPropertyName = "NetworkCluster", // MUST match the property name in TagDisplayModel
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill // Adjust sizing as needed
            };
            dataGridViewDeviceIPTags.Columns.Add(colNetworkCluster);

            // TaggedEntity
            var colTaggedEntity = new DataGridViewTextBoxColumn()
            {
                Name = "TaggedEntity",
                HeaderText = "Tagged Entity"
            };
            dataGridViewDeviceIPTags.Columns.Add(colTaggedEntity);

            // Action
            var colAction = new DataGridViewTextBoxColumn()
            {
                Name = "Action",
                HeaderText = "Action"
            };
            dataGridViewDeviceIPTags.Columns.Add(colAction);
        }
        private void btnAddTagDeviceIP_Click(object sender, EventArgs e)
        {
            Overlay();
            AddDeviceIPTag addTagForm = new AddDeviceIPTag();
            addTagForm.Owner = this;
            addTagForm.StartPosition = FormStartPosition.CenterScreen;
            addTagForm.TagSaved += (s, ea) =>
            {
                LoadDeviceIPTags();
            };

            addTagForm.ShowDialog();
            overlayForm.Close();
        }
        private void txtSearchDeviceIPTag_TextChanged(object sender, EventArgs e)
        {
            LoadCustomerTags();
        }
        private void dataGridViewDeviceIPTags_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            TagRepository tagRepo = new TagRepository();
            DeviceIPRepository devRepo = new DeviceIPRepository();

            foreach (DataGridViewRow row in dataGridViewDeviceIPTags.Rows)
            {
                var displayItem = row.DataBoundItem as TagDisplayModel;
                if (displayItem == null) continue;

                // Retrieve the actual TagClass if needed:
                TagClass realTag = tagRepo.GetTagById(displayItem.Id);
                if (realTag == null) continue;

                // Now figure out how many device IPs are assigned:
                List<int> deviceIds = tagRepo.GetDeviceIPsForTag(realTag.Id);
                if (deviceIds.Count == 0)
                {
                    row.Cells["TaggedEntity"].Value = "Assign Device IP >>";
                }
                else
                {
                    row.Cells["TaggedEntity"].Value = $"({deviceIds.Count}) Device IPs";
                }
            }
        }
        private void dataGridViewDeviceIPTags_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Which Tag was clicked?
            TagDisplayModel displayItem = dataGridViewDeviceIPTags.Rows[e.RowIndex].DataBoundItem as TagDisplayModel;
            if (displayItem == null) return;

            // Retrieve the TagClass
            TagRepository tagRepo = new TagRepository();
            TagClass selectedTag = tagRepo.GetTagById(displayItem.Id);
            if (selectedTag == null) return;

            string colName = dataGridViewDeviceIPTags.Columns[e.ColumnIndex].Name;

            // 1) "TaggedEntity" column => either "Assign Device IP >>" or "(N) Device IPs"
            if (colName == "TaggedEntity")
            {
                object cellVal = dataGridViewDeviceIPTags[e.ColumnIndex, e.RowIndex].Value;
                string cellText = cellVal == null ? "" : cellVal.ToString();

                if (cellText == "Assign Device IP >>")
                {
                    // Open a form like "AssignDeviceIPForm"
                    // That form lists Device IPs in a grid with checkboxes
                    // On "Assign", it calls `tagRepo.AssignTagToDeviceIP(...)`
                    AssignDeviceIP assignForm = new AssignDeviceIP(selectedTag);
                    assignForm.DeviceIPAssigned += (s, ea) => LoadDeviceIPTags();
                    Overlay(); // Show overlay
                    assignForm.StartPosition = FormStartPosition.CenterScreen;
                    assignForm.ShowDialog();
                    overlayForm.Close(); // Close overlay

                }
                else
                {
                    // Possibly "View Device IP" form that shows which IPs are assigned
                    ViewTaggedDeviceIP viewForm = new ViewTaggedDeviceIP(selectedTag);
                    viewForm.DeviceIPUpdated += (s, ea) => LoadDeviceIPTags();
                    viewForm.ShowDialog();
                }
            }
            // 2) "Action" column => check if Edit or Delete
            else if (colName == "Action")
            {
                // same approach as your existing code: figure out if Edit or Delete
                Rectangle cellRect = dataGridViewDeviceIPTags.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridViewDeviceIPTags.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;

                if (relativeX < buttonWidth + 2)
                {
                    EditDeviceIPTag editDeviceIPTagForm = new EditDeviceIPTag(selectedTag);
                    editDeviceIPTagForm.TagSaved += (s, ea) => LoadDeviceIPTags(); // Refresh grid after saving
                    Overlay(); // Show overlay
                    editDeviceIPTagForm.StartPosition = FormStartPosition.CenterScreen;
                    editDeviceIPTagForm.ShowDialog();
                    overlayForm.Close(); // Close overlay

                }
                else
                {
                    // Delete Tag
                    if (MessageBox.Show("Are you sure you want to delete this tag?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        tagRepo.DeleteTag(selectedTag.Id);
                        LoadDeviceIPTags();
                    }
                }
            }
        }
        private void dataGridViewDeviceIPTags_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Check if this is the Action column and a valid row
            if (e.ColumnIndex == dataGridViewDeviceIPTags.Columns["Action"].Index && e.RowIndex >= 0)
            {
                // Paint the cell background and borders
                e.PaintBackground(e.ClipBounds, true);

                // Calculate the dimensions for the two buttons
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                int buttonHeight = e.CellBounds.Height - 4;
                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                // Draw the "Edit" button
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", this.Font,
                    false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                // Draw the "Delete" button
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", this.Font,
                    false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }
        #endregion

    }
}
