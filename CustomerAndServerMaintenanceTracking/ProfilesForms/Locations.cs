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
using CustomerAndServerMaintenanceTracking.Profiles.ModalForms;


namespace CustomerAndServerMaintenanceTracking.Profiles
{
    public partial class Locations : Form, IRefreshableForm
    {
        private LocationRepository _locationRepository;
        private OverlayForm _overlayForm;

        public Locations()
        {
            InitializeComponent();
            _locationRepository = new LocationRepository();
            InitializeDataGridView();
            LoadLocationsData();

            this.btnAddMunicipality.Click += new System.EventHandler(this.btnAddMunicipality_Click);
            this.txtSearchLocation.TextChanged += new System.EventHandler(this.txtSearchLocation_TextChanged);
            this.dataGridLocations.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridLocations_CellClick);
            this.dataGridLocations.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridLocations_CellPainting);
        }

        // ... (InitializeDataGridView, LoadLocationsData, RefreshDataViews, ShowOverlay, CloseOverlay methods remain the same as before) ...
        // Make sure LoadLocationsData calls _locationRepository.GetMunicipalities(includeBarangays: true);

        private void InitializeDataGridView() // Ensure this matches what you have
        {
            dataGridLocations.Columns.Clear();
            dataGridLocations.AutoGenerateColumns = false;

            // --- Crucial for row height adjustment based on wrapped content ---
            dataGridLocations.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Define columns
            dataGridLocations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                HeaderText = "ID",
                Visible = false
            });
            dataGridLocations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MunicipalityName",
                DataPropertyName = "Name",
                HeaderText = "Municipality",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 30 // Adjust fill weight as needed
            });
            dataGridLocations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BarangayCount",
                HeaderText = "Brgy Count",
                // Set AutoSizeMode to None to ensure the Width property is respected
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 85, // Set desired fixed width to 100
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // --- Column for listing Barangay names with Text Wrapping Enabled ---
            DataGridViewTextBoxColumn barangayListColumn = new DataGridViewTextBoxColumn
            {
                Name = "BarangayListColumn",
                HeaderText = "Barangays",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // Let it take available space
                FillWeight = 50, // Give it a good portion of space to fill
                ReadOnly = true
            };
            // THIS IS KEY: Enable text wrapping for this column
            barangayListColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridLocations.Columns.Add(barangayListColumn);


            dataGridLocations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DateAdded",
                DataPropertyName = "DateAdded",
                HeaderText = "Date Added",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                Width = 90, // Fixed width can be good for dates
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn();
            actionColumn.HeaderText = "Action";
            actionColumn.Name = "ActionColumn";
            actionColumn.ReadOnly = true;
            actionColumn.Width = 120;
            actionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // Ensure fixed width for action buttons
            dataGridLocations.Columns.Add(actionColumn);

            dataGridLocations.AllowUserToAddRows = false;
            dataGridLocations.ReadOnly = true;
        }

        private void LoadLocationsData()
        {
            try
            {
                List<Municipality> municipalities = _locationRepository.GetMunicipalities(includeBarangays: true);

                string searchTerm = txtSearchLocation.Text.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    municipalities = municipalities
                        .Where(m => m.Name.ToLowerInvariant().Contains(searchTerm) ||
                                    (m.Barangays != null && m.Barangays.Any(b => b.Name.ToLowerInvariant().Contains(searchTerm))))
                        .ToList();
                }

                dataGridLocations.DataSource = null;
                dataGridLocations.DataSource = municipalities;

                // Manually populate the BarangayCount and BarangayListColumn columns
                foreach (DataGridViewRow row in dataGridLocations.Rows)
                {
                    if (row.DataBoundItem is Municipality muni)
                    {
                        // Populate "Brgy Count"
                        row.Cells["BarangayCount"].Value = muni.Barangays?.Count ?? 0;

                        // Populate "Barangays" list
                        if (muni.Barangays != null && muni.Barangays.Any())
                        {
                            row.Cells["BarangayListColumn"].Value = string.Join(", ", muni.Barangays.Select(b => b.Name));
                        }
                        else
                        {
                            row.Cells["BarangayListColumn"].Value = string.Empty; // Or "N/A", or leave blank
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading locations: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void RefreshDataViews()
        {
            LoadLocationsData();
        }

        private void ShowOverlay()
        {
            if (_overlayForm == null || _overlayForm.IsDisposed)
            {
                _overlayForm = new OverlayForm();
            }
            Form mdiParent = this.MdiParent;
            if (mdiParent != null)
            {
                _overlayForm.Owner = mdiParent;
                _overlayForm.StartPosition = FormStartPosition.Manual;
                Point location = mdiParent.PointToScreen(Point.Empty);
                _overlayForm.Bounds = new Rectangle(location, mdiParent.ClientSize);
            }
            else
            {
                _overlayForm.StartPosition = FormStartPosition.CenterScreen;
            }
            _overlayForm.Show();
            _overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            _overlayForm?.Close();
            _overlayForm = null;
        }


        private void btnAddMunicipality_Click(object sender, EventArgs e)
        {
            // Use the default constructor for AddLocation for adding new locations
            AddLocation addLocationForm = new AddLocation();
            addLocationForm.Owner = this;
            addLocationForm.LocationSaved += AddEditLocationForm_LocationSaved; 
            addLocationForm.StartPosition = FormStartPosition.CenterScreen;

            ShowOverlay();
            addLocationForm.ShowDialog();
            CloseOverlay();

            addLocationForm.LocationSaved -= AddEditLocationForm_LocationSaved;
        }

        // Renamed event handler to be generic for add/edit
        private void AddEditLocationForm_LocationSaved(object sender, EventArgs e)
        {
            LoadLocationsData(); // Refresh the grid
        }

        private void txtSearchLocation_TextChanged(object sender, EventArgs e)
        {
            LoadLocationsData();
        }

        private void dataGridLocations_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dataGridLocations.Columns["ActionColumn"].Index)
            {
                e.PaintBackground(e.ClipBounds, true);
                int buttonWidth = (e.CellBounds.Width - 10) / 2;
                int buttonHeight = e.CellBounds.Height - 6;
                if (buttonWidth < 10) buttonWidth = 10;
                if (buttonHeight < 5) buttonHeight = 5;
                int xStart = e.CellBounds.X + 3;
                int yStart = e.CellBounds.Y + 3;
                Rectangle editButtonRect = new Rectangle(xStart, yStart, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(xStart + buttonWidth + 4, yStart, buttonWidth, buttonHeight);
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", dataGridLocations.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", dataGridLocations.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                e.Handled = true;
            }
        }

        private void dataGridLocations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dataGridLocations.Columns["ActionColumn"].Index)
                return;

            if (dataGridLocations.Rows[e.RowIndex].DataBoundItem is Municipality selectedMunicipality)
            {
                // Important: Ensure selectedMunicipality.Barangays is populated if needed by Edit form
                // The LoadLocationsData already fetches with includeBarangays: true, so it should be populated.
                // If not, you might need to fetch it specifically:
                // selectedMunicipality.Barangays = _locationRepository.GetBarangaysByMunicipalityId(selectedMunicipality.Id);

                Rectangle cellRect = dataGridLocations.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridLocations.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 10) / 2;
                if (buttonWidth < 10) buttonWidth = (cellRect.Width - 2) / 2; // Basic fallback

                if (relativeX <= buttonWidth + 3) // Clicked on Edit
                {
                    // Pass the selectedMunicipality to the AddLocation constructor for edit mode
                    AddLocation editLocationForm = new AddLocation(selectedMunicipality); // Using the new constructor
                    editLocationForm.LocationSaved += AddEditLocationForm_LocationSaved;
                    editLocationForm.StartPosition = FormStartPosition.CenterScreen;

                    ShowOverlay();
                    editLocationForm.ShowDialog(this);
                    CloseOverlay();

                    editLocationForm.LocationSaved -= AddEditLocationForm_LocationSaved;
                }
                else // Clicked on Delete
                {
                    if (MessageBox.Show($"Are you sure you want to delete '{selectedMunicipality.Name}' and all its associated barangays?",
                                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            _locationRepository.DeleteMunicipality(selectedMunicipality.Id);
                            LoadLocationsData(); // Refresh after delete
                            MessageBox.Show($"'{selectedMunicipality.Name}' deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting location: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}
