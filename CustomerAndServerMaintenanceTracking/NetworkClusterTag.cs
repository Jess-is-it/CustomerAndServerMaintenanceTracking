using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms;
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

namespace CustomerAndServerMaintenanceTracking
{
    public partial class NetworkClusterTag: Form, IRefreshableForm
    {
        private OverlayForm overlayForm;

        public void RefreshDataViews()
        {
            LoadClusters();
        }


        public NetworkClusterTag()
        {
            InitializeComponent();
            InitializeDataGridViewColumns();
            LoadClusters();
            dataGridViewClusters.DataBindingComplete += dataGridViewClusters_DataBindingComplete;
            dataGridViewClusters.CellPainting += dataGridViewClusters_CellPainting;
            dataGridViewClusters.CellClick += dataGridViewClusters_CellClick;
        }

        // In NetworkClusterTag.Designer.cs or in the form constructor after InitializeComponent():
        private void InitializeDataGridViewColumns()
        {
            dataGridViewClusters.Columns.Clear();
            dataGridViewClusters.AutoGenerateColumns = false;

            // (A) ID column (optional display)
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "ID",
                Name = "IdColumn",
                Visible = false // or true if you want to show it
            };
            dataGridViewClusters.Columns.Add(idColumn);

            // (B) Name column
            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ClusterName",
                HeaderText = "Cluster Name",
                Name = "ClusterNameColumn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewClusters.Columns.Add(nameColumn);

            // (C) Description column
            DataGridViewTextBoxColumn descColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ClusterDescription",
                HeaderText = "Description",
                Name = "DescriptionColumn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewClusters.Columns.Add(descColumn);

            DataGridViewTextBoxColumn hierarchyColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Hierarchy",
                Name = "HierarchyColumn",
                ReadOnly = true,
                Width = 120
            };
            dataGridViewClusters.Columns.Add(hierarchyColumn);


            // (E) Action2 column (Edit/Delete side by side)
            DataGridViewTextBoxColumn action2Column = new DataGridViewTextBoxColumn
            {
                HeaderText = "Action",
                Name = "Action2Column",
                ReadOnly = true,
                Width = 120 // adjust as needed
            };
            dataGridViewClusters.Columns.Add(action2Column);
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


        private void LoadClusters()
        {
            NetworkClusterRepository repo = new NetworkClusterRepository();
            var clusters = repo.GetClusters();
            dataGridViewClusters.DataSource = clusters;
        }

        // This event handler is for the Add button.
        private void btnAddCluster_Click(object sender, EventArgs e)
        {
            Overlay();
            AddNetworkCluster addClusterForm = new AddNetworkCluster();
            addClusterForm.Owner = this;
            addClusterForm.StartPosition = FormStartPosition.CenterScreen;

            // Calculate center position relative to the main form.
            int centerX = this.Location.X + (this.Width - addClusterForm.Width) / 2;
            int centerY = this.Location.Y + (this.Height - addClusterForm.Height) / 2;
            addClusterForm.Location = new Point(centerX, centerY);
            addClusterForm.ClusterAdded += (s, ea) =>
            {
                // Refresh tag list after a new tag is added.
                LoadClusters();
            };

            // Show the overlay form modelessly.
            addClusterForm.ShowDialog();
            overlayForm.Close();

        }
        private void dataGridViewClusters_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // For each row, set the HierarchyColumn cell to "Arrange Hierarchy >>"
            foreach (DataGridViewRow row in dataGridViewClusters.Rows)
            {
                row.Cells["HierarchyColumn"].Value = "Arrange Hierarchy >>";
            }
        }


        // This event handles clicks in the Action column to open ManageTagTree.
        private void dataGridViewClusters_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // Check if the user clicked the "HierarchyColumn"
            if (dataGridViewClusters.Columns[e.ColumnIndex].Name == "HierarchyColumn")
            {
                // Confirm the cell has "Arrange Hierarchy >>"
                string cellValue = dataGridViewClusters[e.ColumnIndex, e.RowIndex].Value?.ToString();
                if (cellValue == "Arrange Hierarchy >>")
                {
                    // Retrieve the selected cluster from that row
                    NetworkCluster selectedCluster =
                        dataGridViewClusters.Rows[e.RowIndex].DataBoundItem as NetworkCluster;
                    if (selectedCluster == null)
                        return;

                    // Open ManageTagTree passing the cluster ID
                    Overlay();
                    ManageTagTree manageTreeForm = new ManageTagTree(selectedCluster.Id);
                    manageTreeForm.Owner = this;
                    manageTreeForm.StartPosition = FormStartPosition.CenterScreen;
                    manageTreeForm.ShowDialog();
                    overlayForm.Close();
                }
            }
            else if (dataGridViewClusters.Columns[e.ColumnIndex].Name == "Action2Column")
            {
                // 1) figure out if left or right half
                Rectangle cellRect = dataGridViewClusters.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridViewClusters.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;

                int buttonWidth = (cellRect.Width - 6) / 2;

                // 2) Get the selected cluster
                NetworkCluster selectedCluster = dataGridViewClusters.Rows[e.RowIndex].DataBoundItem as NetworkCluster;
                if (selectedCluster == null) return;

                // 3) If left side => Edit, else => Delete
                if (relativeX < buttonWidth + 2)
                {
                    // Edit
                    EditNetworkCluster(selectedCluster);
                }
                else
                {
                    // Delete
                    DeleteNetworkCluster(selectedCluster.Id);
                }
            }

        }


        private void DeleteNetworkCluster(int clusterId)
        {
            if (MessageBox.Show("Are you sure you want to delete this cluster?",
                "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                NetworkClusterRepository repo = new NetworkClusterRepository();
                // This method should remove references from any foreign keys if needed
                repo.DeleteCluster(clusterId);
                LoadClusters();
            }
        }


        private void EditNetworkCluster(NetworkCluster cluster)
        {
            // Option A: open a small form to let user change name/description
            Overlay();
            EditNetworkCluster editForm = new EditNetworkCluster(cluster);
            editForm.Owner = this;
            editForm.StartPosition = FormStartPosition.CenterScreen;
            // Suppose it has a ClusterUpdated event to refresh the grid
            editForm.ClusterUpdated += (s, e) => LoadClusters();
            editForm.ShowDialog();
            overlayForm.Close();
        }


        private void dataGridViewClusters_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Only paint if it's the Action2Column and a valid row
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0 &&
                dataGridViewClusters.Columns[e.ColumnIndex].Name == "Action2Column")
            {
                e.PaintBackground(e.ClipBounds, true);

                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                int buttonHeight = e.CellBounds.Height - 4;

                // Edit button rect
                Rectangle editButtonRect = new Rectangle(
                    e.CellBounds.X + 2,
                    e.CellBounds.Y + 2,
                    buttonWidth,
                    buttonHeight
                );

                // Delete button rect
                Rectangle deleteButtonRect = new Rectangle(
                    e.CellBounds.X + buttonWidth + 4,
                    e.CellBounds.Y + 2,
                    buttonWidth,
                    buttonHeight
                );

                // Draw the "Edit" button
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", this.Font,
                    false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                // Draw the "Delete" button
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", this.Font,
                    false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true; // we handled painting
            }
        }




    }
}
