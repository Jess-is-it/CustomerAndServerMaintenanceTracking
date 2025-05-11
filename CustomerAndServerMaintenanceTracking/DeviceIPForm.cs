using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.Models;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class DeviceIPForm: Form
    {
        private OverlayForm overlayForm;
        public DeviceIPForm()
        {
            InitializeComponent();
            // Setup columns in DataGridView
            SetupDataGridView();

            // Load the devices initially
            LoadDevices();

            // Wire up events
            txtSearch.TextChanged += (s, e) => LoadDevices();
            dataGridViewDevices.CellPainting += dataGridViewDevices_CellPainting;
            dataGridViewDevices.CellClick += dataGridViewDevices_CellClick;
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

        private void SetupDataGridView()
        {
            dataGridViewDevices.AutoGenerateColumns = false;
            dataGridViewDevices.Columns.Clear();

            // 1) ID column (hidden or shown as needed)
            var colId = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "ID",
                Visible = false
            };
            dataGridViewDevices.Columns.Add(colId);

            // 2) DeviceName column
            var colDeviceName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DeviceName",
                HeaderText = "Device Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewDevices.Columns.Add(colDeviceName);

            // 3) IPAddress column
            var colIp = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "IPAddress",
                HeaderText = "IP Address",
                Width = 150
            };
            dataGridViewDevices.Columns.Add(colIp);

            // 4) Location column
            var colLocation = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Location",
                HeaderText = "Location",
                Width = 150
            };
            dataGridViewDevices.Columns.Add(colLocation);

            // 5) Action column (unbound, for Edit/Delete)
            var colAction = new DataGridViewTextBoxColumn
            {
                Name = "Action",
                HeaderText = "Action",
                Width = 120
            };
            dataGridViewDevices.Columns.Add(colAction);
        }

        private void LoadDevices()
        {
            DeviceIPRepository repo = new DeviceIPRepository();
            List<DeviceIP> devices = repo.GetAllDevices();

            // Filter by search
            string searchTerm = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                devices = devices
                    .Where(d => d.DeviceName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            dataGridViewDevices.DataSource = devices;
        }

        private void btnAddIP_Click(object sender, EventArgs e)
        {
            Overlay();
            AddDeviceIP addForm = new AddDeviceIP();
            addForm.DeviceAdded += (s, ea) => LoadDevices();
            addForm.StartPosition = FormStartPosition.CenterScreen;
            addForm.ShowDialog();
            overlayForm.Close();
        }

        // Paint the Edit/Delete buttons in the "Action" column
        private void dataGridViewDevices_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                dataGridViewDevices.Columns[e.ColumnIndex].Name == "Action")
            {
                e.PaintBackground(e.ClipBounds, true);

                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                int buttonHeight = e.CellBounds.Height - 4;

                // "Edit" button
                Rectangle editRect = new Rectangle(
                    e.CellBounds.X + 2,
                    e.CellBounds.Y + 2,
                    buttonWidth,
                    buttonHeight
                );
                ButtonRenderer.DrawButton(e.Graphics, editRect, "Edit", this.Font,
                    false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                // "Delete" button
                Rectangle deleteRect = new Rectangle(
                    e.CellBounds.X + buttonWidth + 4,
                    e.CellBounds.Y + 2,
                    buttonWidth,
                    buttonHeight
                );
                ButtonRenderer.DrawButton(e.Graphics, deleteRect, "Delete", this.Font,
                    false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }

        // Handle click in the "Action" column => figure out which button
        private void dataGridViewDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridViewDevices.Columns[e.ColumnIndex].Name != "Action") return;

            // 1) Get the selected device
            DeviceIP selectedDevice = dataGridViewDevices.Rows[e.RowIndex].DataBoundItem as DeviceIP;
            if (selectedDevice == null) return;

            // 2) Determine if the user clicked "Edit" or "Delete"
            Rectangle cellRect = dataGridViewDevices.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            Point clickPoint = dataGridViewDevices.PointToClient(Cursor.Position);
            int relativeX = clickPoint.X - cellRect.X;
            int buttonWidth = (cellRect.Width - 6) / 2;

            if (relativeX < buttonWidth + 2)
            {
                // Edit
                Overlay();
                EditDeviceIP editForm = new EditDeviceIP(selectedDevice);
                editForm.DeviceUpdated += (s, ea) => LoadDevices();
                editForm.StartPosition = FormStartPosition.CenterScreen;
                editForm.ShowDialog();
                overlayForm.Close();
                
            }
            else
            {
                // Delete
                if (MessageBox.Show("Are you sure you want to delete this device?",
                    "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DeviceIPRepository repo = new DeviceIPRepository();
                    repo.DeleteDevice(selectedDevice.Id);
                    LoadDevices();
                }
            }
        }
    }
}
