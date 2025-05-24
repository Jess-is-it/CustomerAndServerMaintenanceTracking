using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.AppLogs; // For ServiceLogs form
using System.ServiceProcess; // For ServiceController
using System.Configuration;    // For ConfigurationManager

namespace CustomerAndServerMaintenanceTracking
{
    public partial class Settings : Form
    {
        private OverlayForm overlayForm;
        private System.Windows.Forms.Timer _serviceStatusRefreshTimer;

        // These MUST match the actual registered Windows Service names
        private const string ActualNetwatchPingerServiceName = "NetwatchPingerService";
        private const string ActualPPPoESyncServiceName = "PPPoESyncService";

        private readonly string _targetMachineName;

        public Settings()
        {
            InitializeComponent();

            _targetMachineName = ConfigurationManager.AppSettings["ServiceControlTargetMachine"];
            if (string.IsNullOrWhiteSpace(_targetMachineName))
            {
                _targetMachineName = ".";
                // Optionally inform the user if the setting is missing and defaulting
                // MessageBox.Show("ServiceControlTargetMachine not specified in App.config. Defaulting to local machine.", "Configuration Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            InitializeRouterTab();
            LoadRouters();

            // Remove event handlers for Start/Stop buttons from Settings form
            // if (this.btnStartStopNetwatchService != null)
            //     this.btnStartStopNetwatchService.Click -= BtnStartStopNetwatchService_Click; // Assuming it was wired
            // if (this.btnStartStopPPPoESyncService != null)
            //     this.btnStartStopPPPoESyncService.Click -= BtnStartStopPPPoESyncService_Click; // Assuming it was wired

            // Keep event handlers for View Logs buttons
            if (this.btnViewNetwatchService != null) // Assuming button1 is "View Console Logs" for Netwatch
                this.btnViewNetwatchService.Click += BtnViewNetwatchLogs_Click;
            if (this.btnViewPPPoESyncService != null)
                this.btnViewPPPoESyncService.Click += BtnViewPPPoESyncLogs_Click;
        }

        private void InitializeRouterTab()
        {
            dataGridViewRouters.Columns.Clear();
            dataGridViewRouters.AutoGenerateColumns = false;

            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Width = 50, Visible = false });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RouterName", HeaderText = "Router Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HostIPAddress", HeaderText = "Host IP Address", Width = 120 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ApiPort", HeaderText = "API Port", Width = 60 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", Width = 120 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Password", HeaderText = "Password", Width = 120 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", Name = "Action", ReadOnly = true, Width = 120 });
        }

        private void LoadRouters()
        {
            try
            {
                MikrotikRouterRepository repo = new MikrotikRouterRepository();
                var routers = repo.GetRouters();
                string search = txtSearchRouter.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(search) && routers != null)
                {
                    routers = routers.Where(r => r.RouterName != null && r.RouterName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                dataGridViewRouters.DataSource = null;
                dataGridViewRouters.DataSource = routers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading routers: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearchRouter_TextChanged(object sender, EventArgs e)
        {
            LoadRouters();
        }

        private void btnAddRouter_Click(object sender, EventArgs e)
        {
            AddRouter addRouterForm = new AddRouter();
            addRouterForm.RouterSaved += (s, ea) => LoadRouters();
            addRouterForm.StartPosition = FormStartPosition.CenterParent;
            Overlay();
            addRouterForm.ShowDialog(this);
            CloseOverlay();
        }

        private void Overlay()
        {
            if (overlayForm == null || overlayForm.IsDisposed)
            {
                overlayForm = new OverlayForm();
            }
            Form formToCover = this.MdiParent ?? this;
            overlayForm.Owner = formToCover;
            overlayForm.StartPosition = FormStartPosition.Manual;
            Point location = formToCover.PointToScreen(Point.Empty);
            overlayForm.Bounds = new Rectangle(location, formToCover.ClientSize);
            overlayForm.Show();
            overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                overlayForm.Close();
                overlayForm = null;
            }
        }

        private void dataGridViewRouters_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewRouters.Columns["Action"] != null && e.ColumnIndex == dataGridViewRouters.Columns["Action"].Index)
            {
                e.PaintBackground(e.ClipBounds, true);
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                if (buttonWidth < 10) buttonWidth = 10;
                int buttonHeight = e.CellBounds.Height - 4;
                if (buttonHeight < 5) buttonHeight = 5;

                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }

        private void dataGridViewRouters_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridViewRouters.Rows[e.RowIndex].DataBoundItem is MikrotikRouter selectedRouter)
            {
                if (dataGridViewRouters.Columns["Action"] != null && e.ColumnIndex == dataGridViewRouters.Columns["Action"].Index)
                {
                    Rectangle cellRect = dataGridViewRouters.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    Point clickPoint = dataGridViewRouters.PointToClient(Cursor.Position);
                    int relativeX = clickPoint.X - cellRect.X;
                    int buttonWidth = (cellRect.Width - 6) / 2;
                    if (buttonWidth < 10) buttonWidth = (cellRect.Width - 2) / 2;

                    if (relativeX <= buttonWidth + 2)
                    {
                        EditRouter editRouterForm = new EditRouter(selectedRouter);
                        editRouterForm.RouterSaved += (s, ea) => LoadRouters();
                        editRouterForm.StartPosition = FormStartPosition.CenterParent;
                        Overlay();
                        editRouterForm.ShowDialog(this);
                        CloseOverlay();
                    }
                    else
                    {
                        if (MessageBox.Show($"Are you sure you want to delete the router '{selectedRouter.RouterName}'?\nThis will also archive any customers associated with this router.", "Confirm Delete Router", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            try
                            {
                                // --- NEW CODE: Archive associated customers FIRST ---
                                CustomerRepository customerRepo = new CustomerRepository();
                                int customersArchived = customerRepo.ArchiveCustomersByRouterId(selectedRouter.Id);

                                if (customersArchived > 0)
                                {
                                    MessageBox.Show($"{customersArchived} customer(s) associated with router '{selectedRouter.RouterName}' have been archived.", "Customers Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                // --- END OF NEW CODE ---

                                // Now delete the router
                                MikrotikRouterRepository routerRepo = new MikrotikRouterRepository(); // Renamed 'repo' to 'routerRepo' for clarity
                                routerRepo.DeleteRouter(selectedRouter.Id);

                                MessageBox.Show($"Router '{selectedRouter.RouterName}' deleted successfully.", "Router Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                LoadRouters(); // Refresh the grid
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error deleting router or archiving customers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                // Optionally, re-load routers even on error to reflect partial changes if any, or current state.
                                LoadRouters();
                            }
                        }
                    }
                }
            }
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            UpdateAllServiceStatusesAndButtons();

            _serviceStatusRefreshTimer = new System.Windows.Forms.Timer();
            _serviceStatusRefreshTimer.Interval = 10000; // Refresh every 10 seconds (adjust as needed)
            _serviceStatusRefreshTimer.Tick += ServiceStatusRefreshTimer_Tick;
            _serviceStatusRefreshTimer.Start();

            // Hide the Start/Stop buttons as per user request
            if (btnStartStopNetwatchService != null) btnStartStopNetwatchService.Visible = false;
            if (btnStartStopPPPoESyncService != null) btnStartStopPPPoESyncService.Visible = false;
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serviceStatusRefreshTimer?.Stop();
            _serviceStatusRefreshTimer?.Dispose();
        }

        private void ServiceStatusRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed || this.Disposing) return;
            UpdateAllServiceStatusesAndButtons();
        }

        private void UpdateAllServiceStatusesAndButtons()
        {
            if (this.IsDisposed || this.Disposing) return;
            UpdateSimplifiedServiceStatusUI(ActualNetwatchPingerServiceName, lblNetwatchService);
            UpdateSimplifiedServiceStatusUI(ActualPPPoESyncServiceName, lblPPPoESyncService);
        }

        // Simplified status update method
        private void UpdateSimplifiedServiceStatusUI(string serviceName, Label statusLabel)
        {
            if (statusLabel == null || statusLabel.IsDisposed) return;
            if (this.IsDisposed || this.Disposing) return;

            string statusText = "Unreachable - Check Server Services"; // Default
            Color statusColor = Color.OrangeRed; // Default for unreachable

            try
            {
                using (ServiceController sc = new ServiceController(serviceName, _targetMachineName))
                {
                    sc.Refresh(); // Important to get the latest status
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        statusText = "Running";
                        statusColor = Color.Green;
                    }
                    // For any other status (Stopped, Paused, Pending, Not Found), it's considered "Unreachable" for this simplified view
                }
            }
            catch (InvalidOperationException) // Service not found
            {
                // Status remains "Unreachable..."
            }
            catch (Exception ex)
            {
                // Status remains "Unreachable..."
                Console.WriteLine($"Error getting status for {serviceName} on '{_targetMachineName}': {ex.Message}");
            }
            finally
            {
                if (!statusLabel.IsDisposed)
                {
                    statusLabel.Text = $"Status: {statusText}";
                    statusLabel.ForeColor = statusColor;
                }
            }
        }

        // ControlService method is removed as Start/Stop buttons are removed from this form.

        // Event Handlers for View Logs Buttons (remain unchanged)
        private void BtnViewNetwatchLogs_Click(object sender, EventArgs e)
        {
            ServiceLogs logsForm = new ServiceLogs(ActualNetwatchPingerServiceName, "Netwatch Pinger Service");
            logsForm.ShowDialog(this);
        }

        private void BtnViewPPPoESyncLogs_Click(object sender, EventArgs e)
        {
            ServiceLogs logsForm = new ServiceLogs(ActualPPPoESyncServiceName, "PPPoE Sync Service");
            logsForm.ShowDialog(this);
        }

        // Remove click event handlers for Start/Stop buttons if they were defined
        // private void BtnStartStopNetwatchService_Click(object sender, EventArgs e) { /* Removed */ }
        // private void BtnStartStopPPPoESyncService_Click(object sender, EventArgs e) { /* Removed */ }
    }
}
