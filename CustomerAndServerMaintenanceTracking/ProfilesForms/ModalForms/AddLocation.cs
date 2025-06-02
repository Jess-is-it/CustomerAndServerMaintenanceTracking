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
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.Profiles.ModalForms
{
    public partial class AddLocation : Form
    {
        public event EventHandler LocationSaved; // Changed from LocationAdded
        private LocationRepository _locationRepository;
        private Municipality _editingMunicipality; // To store the municipality being edited, null if in add mode

        // Constructor for ADD mode
        public AddLocation()
        {
            InitializeComponent();
            _locationRepository = new LocationRepository();
            this.Text = "Add New Location";
            this.label1.Text = "Add New Location"; // Assuming label1 is your title label from designer
            this.btnAddLocation.Text = "Add Location";
        }

        // Constructor for EDIT mode
        public AddLocation(Municipality municipalityToEdit) : this() // Calls the default constructor first
        {
            _editingMunicipality = municipalityToEdit;
            this.Text = "Edit Location";
            this.label1.Text = "Edit Location";
            this.btnAddLocation.Text = "Update Location";

            // Pre-fill the form with existing data
            if (_editingMunicipality != null)
            {
                txtMunicipalityName.Text = _editingMunicipality.Name;
                // Ensure Barangays are loaded for the municipality if not already
                if (_editingMunicipality.Barangays == null || !_editingMunicipality.Barangays.Any())
                {
                    // Fetch them if not passed in (though it's better if the calling form passes them)
                    _editingMunicipality.Barangays = _locationRepository.GetBarangaysByMunicipalityId(_editingMunicipality.Id);
                }
                txtBarangay.Text = string.Join(", ", _editingMunicipality.Barangays.Select(b => b.Name));
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAddLocation_Click(object sender, EventArgs e) // Renaming this handler might be good, e.g., btnSave_Click
        {
            string municipalityName = txtMunicipalityName.Text.Trim();
            string barangaysString = txtBarangay.Text.Trim();

            if (string.IsNullOrWhiteSpace(municipalityName))
            {
                MessageBox.Show("Municipality Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMunicipalityName.Focus();
                return;
            }

            try
            {
                List<string> barangayNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(barangaysString))
                {
                    barangayNames = barangaysString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(name => name.Trim())
                                                   .Where(name => !string.IsNullOrWhiteSpace(name))
                                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                                   .ToList();
                }

                if (_editingMunicipality == null) // ADD Mode
                {
                    int municipalityId = _locationRepository.AddOrGetMunicipality(municipalityName);
                    if (municipalityId == -1)
                    {
                        MessageBox.Show("Failed to add or retrieve the municipality.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    int newBarangaysAddedCount = 0;
                    if (barangayNames.Any())
                    {
                        newBarangaysAddedCount = _locationRepository.AddBarangays(municipalityId, barangayNames);
                    }
                    MessageBox.Show($"Municipality '{municipalityName}' saved.\n{newBarangaysAddedCount} new barangay(s) added.", "Location Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else // EDIT Mode
                {
                    _editingMunicipality.Name = municipalityName;
                    // You'll need an UpdateMunicipality method in your repository
                    _locationRepository.UpdateMunicipality(_editingMunicipality); // We'll add this method to the repo

                    // For barangays, a common approach is to delete existing and add new ones,
                    // or implement more complex diffing. Let's go with delete then add for simplicity.
                    _locationRepository.DeleteBarangaysByMunicipalityId(_editingMunicipality.Id); // We'll add this
                    int updatedBarangaysCount = 0;
                    if (barangayNames.Any())
                    {
                        updatedBarangaysCount = _locationRepository.AddBarangays(_editingMunicipality.Id, barangayNames);
                    }
                    MessageBox.Show($"Municipality '{municipalityName}' updated.\n{updatedBarangaysCount} barangay(s) set.", "Location Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                LocationSaved?.Invoke(this, EventArgs.Empty);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving location: {ex.ToString()}");
                MessageBox.Show($"An error occurred while saving the location: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

