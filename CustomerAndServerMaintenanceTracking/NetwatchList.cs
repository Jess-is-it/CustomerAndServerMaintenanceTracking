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
using CustomerAndServerMaintenanceTracking.Models;
using CustomerAndServerMaintenanceTracking;
using CustomerAndServerMaintenanceTracking.CustomCells;
using CustomerAndServerMaintenanceTracking.ModalForms;
using System.Diagnostics;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class NetwatchList : Form, IRefreshableForm
    {
        private NetwatchConfigRepository _netwatchConfigRepository;

        // --- MODIFICATION 1: Add a master list ---
        private List<NetwatchConfigDisplay> _allNetwatchConfigsMasterList;
        private BindingList<NetwatchConfigDisplay> _allNetwatchConfigsBindingList;

        private bool _isNetwatchServiceOnline = true; // Assume online by default
        private DateTime _lastKnownServiceHeartbeat = DateTime.MinValue;
        private const int ServiceOfflineThresholdSeconds = 180; // e.g., 3 minutes (should be > heartbeat interval * 2 or 3)

        private OverlayForm _overlayForm;

        public NetwatchList()
        {
            InitializeComponent();
            _netwatchConfigRepository = new NetwatchConfigRepository();

            // --- MODIFICATION 2: Initialize the master list ---
            _allNetwatchConfigsMasterList = new List<NetwatchConfigDisplay>();
            _allNetwatchConfigsBindingList = new BindingList<NetwatchConfigDisplay>();

            if (this.dgvNetwatchConfigs != null)
            {
                this.dgvNetwatchConfigs.DataSource = _allNetwatchConfigsBindingList;
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, this.dgvNetwatchConfigs, new object[] { true });
            }
            else
            {
                MessageBox.Show("Error: dgvNetwatchConfigs is not found on tabPage1. Please check the designer.", "UI Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NetwatchList_Load(object sender, EventArgs e)
        {
            if (this.dgvNetwatchConfigs != null)
            {
                SetupAllNetwatchDataGridView();
                LoadAllNetwatchData(); // This will now populate master list and apply initial (empty) filter

                this.dgvNetwatchConfigs.CellClick -= dgvNetwatchConfigs_CellClick;
                this.dgvNetwatchConfigs.CellClick += dgvNetwatchConfigs_CellClick;

                this.dgvNetwatchConfigs.CellFormatting -= dgvNetwatchConfigs_CellFormatting;
                this.dgvNetwatchConfigs.CellFormatting += dgvNetwatchConfigs_CellFormatting;

                // --- MODIFICATION 3: Subscribe to txtSearch.TextChanged event ---
                // Assuming your TextBox is named txtSearch. Adjust if it's different.
                if (this.txtSearch != null) // Check if txtSearch exists
                {
                    this.txtSearch.TextChanged -= TxtSearch_TextChanged; // Prevent multiple subscriptions
                    this.txtSearch.TextChanged += TxtSearch_TextChanged;
                }
                else
                {
                    // This MessageBox indicates that 'txtSearch' control was not found by that name.
                    // You might have a different name in your designer for the search TextBox.
                    MessageBox.Show("Search TextBox 'txtSearch' not found on the form. Search will not work.", "UI Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SetupAllNetwatchDataGridView()
        {
            // ... (your existing SetupAllNetwatchDataGridView code remains the same) ...
            // It defines columns for dgvNetwatchConfigs
            if (this.dgvNetwatchConfigs == null) return;

            dgvNetwatchConfigs.AutoGenerateColumns = false;
            dgvNetwatchConfigs.Columns.Clear();

            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClusterName", HeaderText = "Cluster", DataPropertyName = "TargetSourceName", FillWeight = 15, ReadOnly = true });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Visible = false });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetwatchName", HeaderText = "Netwatch Name", DataPropertyName = "NetwatchName", FillWeight = 20 });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", DataPropertyName = "Type", FillWeight = 5 });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "MonitoredTagsDisplay", HeaderText = "Monitored Tag(s)", DataPropertyName = "MonitoredTagsDisplay", FillWeight = 25 });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "IntervalSeconds", HeaderText = "Interval (s)", DataPropertyName = "IntervalSeconds", FillWeight = 5, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "TimeoutMilliseconds", HeaderText = "Timeout (ms)", DataPropertyName = "TimeoutMilliseconds", FillWeight = 5, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsEnabledCol", HeaderText = "Enabled", DataPropertyName = "IsEnabled", FillWeight = 7, AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader, ReadOnly = true, Visible = false });
            dgvNetwatchConfigs.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastStatus", HeaderText = "Status", DataPropertyName = "LastStatus", FillWeight = 25 });

            var actionColumn = new ActionDataGridViewMultiButtonColumn { Name = "Actions", HeaderText = "Actions", FillWeight = 10, MinimumWidth = 50 };
            dgvNetwatchConfigs.Columns.Add(actionColumn);

            dgvNetwatchConfigs.ReadOnly = true;
            dgvNetwatchConfigs.AllowUserToAddRows = false;
            dgvNetwatchConfigs.AllowUserToDeleteRows = false;
            dgvNetwatchConfigs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNetwatchConfigs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        // --- MODIFICATION 4: Update LoadAllNetwatchData ---
        public void LoadAllNetwatchData()
        {
            if (this.dgvNetwatchConfigs == null) return;

            // --- Check Service Heartbeat First ---
            try
            {
                DateTime? lastHeartbeat = _netwatchConfigRepository.GetLastServiceHeartbeat("NetwatchPingerService");
                if (lastHeartbeat.HasValue)
                {
                    _lastKnownServiceHeartbeat = lastHeartbeat.Value;
                    if ((DateTime.Now - _lastKnownServiceHeartbeat).TotalSeconds > ServiceOfflineThresholdSeconds)
                    {
                        _isNetwatchServiceOnline = false;
                        Debug.WriteLine($"NetwatchList: Service considered OFFLINE. Last heartbeat: {_lastKnownServiceHeartbeat}, Threshold: {ServiceOfflineThresholdSeconds}s");
                    }
                    else
                    {
                        _isNetwatchServiceOnline = true;
                        Debug.WriteLine($"NetwatchList: Service considered ONLINE. Last heartbeat: {_lastKnownServiceHeartbeat}");
                    }
                }
                else
                {
                    _isNetwatchServiceOnline = false; // No heartbeat record found, assume offline
                    Debug.WriteLine("NetwatchList: No heartbeat record found for NetwatchPingerService. Service considered OFFLINE.");
                }
            }
            catch (Exception ex)
            {
                _isNetwatchServiceOnline = false; // Error fetching heartbeat, assume offline for safety
                Debug.WriteLine($"NetwatchList: Error fetching service heartbeat: {ex.Message}. Service considered OFFLINE.");
            }
            // --- End Heartbeat Check ---

            // If service is offline, we might still want to load existing data from NetwatchConfigs table,
            // but the CellFormatting will then show "Netwatch Service Offline".
            // Alternatively, you could clear the grid or show a different message.
            // For now, we proceed to load and let CellFormatting handle display.

            try
            {
                _allNetwatchConfigsMasterList = _netwatchConfigRepository.GetNetwatchConfigsForDisplay();
                ApplyFilterAndRefreshGrid(); // This applies filter and updates the BindingList
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading all Netwatch configurations: {ex.Message}",
                                "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allNetwatchConfigsMasterList.Clear();
                _allNetwatchConfigsBindingList.Clear();
            }

            // After data is potentially reloaded and filtered, invalidate the DGV to force repaint
            // which will re-trigger CellFormatting for all visible cells.
            dgvNetwatchConfigs.Invalidate();
        }

        // --- MODIFICATION 5: Add ApplyFilterAndRefreshGrid method ---
        private void ApplyFilterAndRefreshGrid()
        {
            if (this.dgvNetwatchConfigs == null) return;

            // 1. Store current state (scroll, selection)
            int? selectedConfigId = null;
            if (dgvNetwatchConfigs.CurrentRow != null && dgvNetwatchConfigs.CurrentRow.DataBoundItem is NetwatchConfigDisplay currentDisp) // Renamed 'current' to 'currentDisp'
            {
                selectedConfigId = currentDisp.Id;
            }
            int firstDisplayIndex = dgvNetwatchConfigs.FirstDisplayedScrollingRowIndex;
            if (firstDisplayIndex < 0) firstDisplayIndex = 0;

            dgvNetwatchConfigs.SuspendLayout();
            _allNetwatchConfigsBindingList.RaiseListChangedEvents = false; // Perf optimization

            _allNetwatchConfigsBindingList.Clear();

            string searchText = (this.txtSearch != null) ? this.txtSearch.Text.Trim().ToLowerInvariant() : string.Empty;

            IEnumerable<NetwatchConfigDisplay> filteredData = _allNetwatchConfigsMasterList;

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredData = _allNetwatchConfigsMasterList.Where(config =>
                    (config.NetwatchName?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (config.TargetSourceName?.ToLowerInvariant().Contains(searchText) ?? false) || // Cluster Name
                    (config.MonitoredTagsDisplay?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (config.Type?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (config.LastStatus?.ToLowerInvariant().Contains(searchText) ?? false)
                );
            }

            foreach (var item in filteredData)
            {
                _allNetwatchConfigsBindingList.Add(item);
            }

            _allNetwatchConfigsBindingList.RaiseListChangedEvents = true;
            _allNetwatchConfigsBindingList.ResetBindings(); // Notify grid of changes

            // 3. Restore state
            if (dgvNetwatchConfigs.Rows.Count > 0)
            {
                if (firstDisplayIndex >= dgvNetwatchConfigs.Rows.Count) // If previous scroll pos is out of bounds
                {
                    firstDisplayIndex = dgvNetwatchConfigs.Rows.Count - 1;
                }
                if (firstDisplayIndex < 0) firstDisplayIndex = 0; // Ensure it's not negative

                if (dgvNetwatchConfigs.Rows.Count > 0) // Re-check after potential modification
                    dgvNetwatchConfigs.FirstDisplayedScrollingRowIndex = firstDisplayIndex;


                if (selectedConfigId.HasValue)
                {
                    NetwatchConfigDisplay itemToSelect = _allNetwatchConfigsBindingList.FirstOrDefault(item => item.Id == selectedConfigId.Value);
                    if (itemToSelect != null)
                    {
                        int rowIndexToSelect = _allNetwatchConfigsBindingList.IndexOf(itemToSelect);
                        if (rowIndexToSelect >= 0)
                        {
                            dgvNetwatchConfigs.ClearSelection();
                            dgvNetwatchConfigs.Rows[rowIndexToSelect].Selected = true;
                            if (dgvNetwatchConfigs.Columns.GetFirstColumn(DataGridViewElementStates.Visible).Index != -1)
                            {
                                dgvNetwatchConfigs.CurrentCell = dgvNetwatchConfigs.Rows[rowIndexToSelect].Cells[dgvNetwatchConfigs.Columns.GetFirstColumn(DataGridViewElementStates.Visible).Index];
                            }
                        }
                    }
                }
            }
            dgvNetwatchConfigs.ResumeLayout();
        }

        // --- MODIFICATION 6: Add TxtSearch_TextChanged event handler ---
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilterAndRefreshGrid();
        }


        // --- OverlayForm Methods (ensure they are present) ---
        private void ShowOverlay()
        {
            _overlayForm = new OverlayForm();
            _overlayForm.StartPosition = FormStartPosition.Manual;
            Form formToCover = this.MdiParent ?? this;
            _overlayForm.Bounds = formToCover.Bounds;
            _overlayForm.Show(formToCover);
            _overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            if (_overlayForm != null && !_overlayForm.IsDisposed)
            {
                _overlayForm.Close();
                _overlayForm.Dispose();
                _overlayForm = null;
            }
        }

        // --- btnAddNetwatch_Click (ensure it's present and correct) ---
        private void btnAddNetwatch_Click(object sender, EventArgs e)
        {
            ShowOverlay();
            using (NetwatchAdd netwatchAddInitialForm = new NetwatchAdd())
            {
                netwatchAddInitialForm.StartPosition = FormStartPosition.CenterParent;
                netwatchAddInitialForm.ShowDialog(this);
            }
            CloseOverlay();
            LoadAllNetwatchData(); // This will now call ApplyFilterAndRefreshGrid internally
        }

        // --- CellClick Event Handler for dgvNetwatchConfigs (ensure it's present and correct) ---
        private void dgvNetwatchConfigs_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvNetwatchConfigs.Rows.Count <= e.RowIndex) return;

            // Attempt to get the DataBoundItem first
            NetwatchConfigDisplay selectedConfig = dgvNetwatchConfigs.Rows[e.RowIndex].DataBoundItem as NetwatchConfigDisplay;
            if (selectedConfig == null)
            {
                // This can happen if the row is a new row placeholder or some other issue.
                return;
            }

            // Check if the click is on the "LastStatus" column
            if (dgvNetwatchConfigs.Columns[e.ColumnIndex].Name == "LastStatus")
            {
                if (this.MdiParent is Dashboard mainDashboard)
                {
                    if (selectedConfig != null) // Ensure selectedConfig is not null
                    {
                        string statusToSend;

                        // --- KEY CHANGE: Determine statusToSend based on IsEnabled first ---
                        if (!selectedConfig.IsEnabled)
                        {
                            statusToSend = "Disabled"; // If not enabled, treat as "Disabled"
                        }
                        else
                        {
                            // If enabled, use its LastStatus, ensuring it's not null
                            statusToSend = selectedConfig.LastStatus ?? "Unknown";
                        }
                        // --- END KEY CHANGE ---

                        // Now pass statusToSend to the Dashboard
                        mainDashboard.ShowNetwatchDetailInPanel(selectedConfig.Id, selectedConfig.NetwatchName, statusToSend);
                    }
                    else
                    {
                        MessageBox.Show("Cannot display details: Selected item data is missing.", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Could not display details: NetwatchList is not parented by the Dashboard.", "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return; // Important: Stop further processing
            }

            // --- Existing Action Button Logic ---
            if (dgvNetwatchConfigs.Columns[e.ColumnIndex] is ActionDataGridViewMultiButtonColumn)
            {
                var cell = dgvNetwatchConfigs[e.ColumnIndex, e.RowIndex] as ActionDataGridViewMultiButtonCell;
                if (cell != null)
                {
                    Rectangle cellDisplayRectangle = dgvNetwatchConfigs.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    Point mousePositionInControl = dgvNetwatchConfigs.PointToClient(Cursor.Position);
                    Point cellRelativeMousePosition = new Point(
                        mousePositionInControl.X - cellDisplayRectangle.Left,
                        mousePositionInControl.Y - cellDisplayRectangle.Top);

                    ActionDataGridViewMultiButtonCell.ActionButtonType clickedButton = cell.GetButtonAt(cellRelativeMousePosition);
                    // The selectedConfig is already retrieved above

                    switch (clickedButton)
                    {
                        case ActionDataGridViewMultiButtonCell.ActionButtonType.StartStop:
                            selectedConfig.IsEnabled = !selectedConfig.IsEnabled;
                            try
                            {
                                _netwatchConfigRepository.UpdateNetwatchConfigEnabledStatus(selectedConfig.Id, selectedConfig.IsEnabled);
                                dgvNetwatchConfigs.InvalidateCell(e.ColumnIndex, e.RowIndex);
                                if (dgvNetwatchConfigs.Columns["IsEnabledCol"] != null) { dgvNetwatchConfigs.InvalidateCell(dgvNetwatchConfigs.Columns["IsEnabledCol"].Index, e.RowIndex); }
                                if (dgvNetwatchConfigs.Columns["LastStatus"] != null) { dgvNetwatchConfigs.InvalidateCell(dgvNetwatchConfigs.Columns["LastStatus"].Index, e.RowIndex); }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error updating Netwatch status for '{selectedConfig.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                selectedConfig.IsEnabled = !selectedConfig.IsEnabled; // Revert
                                dgvNetwatchConfigs.InvalidateCell(e.ColumnIndex, e.RowIndex);
                                if (dgvNetwatchConfigs.Columns["IsEnabledCol"] != null) { dgvNetwatchConfigs.InvalidateCell(dgvNetwatchConfigs.Columns["IsEnabledCol"].Index, e.RowIndex); }
                                if (dgvNetwatchConfigs.Columns["LastStatus"] != null) { dgvNetwatchConfigs.InvalidateCell(dgvNetwatchConfigs.Columns["LastStatus"].Index, e.RowIndex); }
                            }
                            break;
                        case ActionDataGridViewMultiButtonCell.ActionButtonType.Delete:
                            if (MessageBox.Show($"Are you sure you want to delete Netwatch config '{selectedConfig.NetwatchName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                            {
                                try
                                {
                                    bool success = _netwatchConfigRepository.DeleteNetwatchConfigById(selectedConfig.Id);
                                    if (success)
                                    {
                                        _allNetwatchConfigsMasterList.RemoveAll(item => item.Id == selectedConfig.Id);
                                        ApplyFilterAndRefreshGrid();
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Failed to delete Netwatch config '{selectedConfig.NetwatchName}'.", "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error deleting Netwatch config '{selectedConfig.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            break;
                    }
                }
            }
            
        }

        // --- CellFormatting Event Handler for dgvNetwatchConfigs (ensure it's present and correct) ---
        private void dgvNetwatchConfigs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvNetwatchConfigs.Columns[e.ColumnIndex].Name != "LastStatus")
            {
                return;
            }

            NetwatchConfigDisplay dataItem = dgvNetwatchConfigs.Rows[e.RowIndex].DataBoundItem as NetwatchConfigDisplay;

            if (dataItem != null)
            {
                // Set default cell styles first
                e.CellStyle.ForeColor = SystemColors.ControlText;
                e.CellStyle.BackColor = (e.RowIndex % 2 == 0) ? SystemColors.Window : SystemColors.ControlLight; // Alternating default
                e.CellStyle.Font = dgvNetwatchConfigs.DefaultCellStyle.Font;
                e.FormattingApplied = false;

                if (!_isNetwatchServiceOnline)
                {
                    e.Value = "Netwatch Service Offline";
                    e.CellStyle.ForeColor = Color.White;
                    e.CellStyle.BackColor = Color.DarkSlateGray;
                    e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Bold | FontStyle.Italic);
                    e.FormattingApplied = true;
                    return;
                }

                string originalStatusText = dataItem.LastStatus ?? string.Empty;
                string displayStatusText = originalStatusText;
                string lastCheckedSuffix = "";

                if (dataItem.LastChecked.HasValue)
                {
                    TimeSpan timeSinceLastCheck = DateTime.Now - dataItem.LastChecked.Value;
                    if (timeSinceLastCheck.TotalSeconds < 1) // Extremely recent, likely means an update just happened
                    {
                        lastCheckedSuffix = " (just updated)";
                    }
                    else if (timeSinceLastCheck.TotalSeconds < 60)
                    {
                        lastCheckedSuffix = $" ({timeSinceLastCheck.TotalSeconds:F0}s ago)";
                    }
                    else if (timeSinceLastCheck.TotalMinutes < 60)
                    {
                        lastCheckedSuffix = $" ({timeSinceLastCheck.TotalMinutes:F0}m ago)";
                    }
                    else if (timeSinceLastCheck.TotalHours < 24)
                    {
                        lastCheckedSuffix = $" ({timeSinceLastCheck.TotalHours:F0}h ago)";
                    }
                    else
                    {
                        lastCheckedSuffix = $" ({dataItem.LastChecked:MMM d, h:mm tt})";
                    }
                }

                if (!dataItem.IsEnabled)
                {
                    displayStatusText = "Disabled";
                    e.CellStyle.ForeColor = Color.DimGray;
                    e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Italic);
                    // Ensure default background for disabled unless you want a specific one
                    e.CellStyle.BackColor = (e.RowIndex % 2 == 0) ? SystemColors.Window : SystemColors.ControlLight;
                }
                else // Item is Enabled
                {
                    // Apply base background and foreground colors based on originalStatusText first
                    if (originalStatusText.Equals("All Up", StringComparison.OrdinalIgnoreCase))
                    {
                        displayStatusText = "All Up"; // Ensure consistent text
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.FromArgb(40, 167, 69); // Green
                    }
                    else if (originalStatusText.StartsWith("Partial:", StringComparison.OrdinalIgnoreCase))
                    {
                        // displayStatusText is originalStatusText
                        e.CellStyle.ForeColor = Color.Black;
                        e.CellStyle.BackColor = Color.FromArgb(255, 193, 7); // Yellow/Amber
                    }
                    else if (originalStatusText.StartsWith("Major Outage:", StringComparison.OrdinalIgnoreCase) ||
                             originalStatusText.Equals("All Down", StringComparison.OrdinalIgnoreCase))
                    {
                        // displayStatusText is originalStatusText
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.FromArgb(220, 53, 69); // Red
                    }
                    else if (originalStatusText.Equals("Timeout", StringComparison.OrdinalIgnoreCase))
                    {
                        displayStatusText = "Timeout";
                        e.CellStyle.ForeColor = Color.Black;
                        e.CellStyle.BackColor = Color.FromArgb(240, 173, 78); // Orange
                    }
                    else if (originalStatusText.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                    {
                        // displayStatusText is originalStatusText
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.FromArgb(200, 35, 51); // Darker Red
                    }
                    else if (originalStatusText.Equals("No IPs configured/found for tags", StringComparison.OrdinalIgnoreCase))
                    {
                        displayStatusText = "No IPs Configured";
                        e.CellStyle.ForeColor = Color.DarkSlateGray;
                        e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Italic);
                        e.CellStyle.BackColor = (e.RowIndex % 2 == 0) ? SystemColors.Window : SystemColors.ControlLight; // Keep default background
                    }
                    else if (string.IsNullOrWhiteSpace(originalStatusText) ||
                             originalStatusText.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        displayStatusText = "Pending";
                        e.CellStyle.ForeColor = Color.DarkOrange;
                        e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Italic);
                        e.CellStyle.BackColor = (e.RowIndex % 2 == 0) ? SystemColors.Window : SystemColors.ControlLight; // Keep default background
                    }
                    // else: default styles (ForeColor, BackColor already set at the top) apply for other unhandled statuses
                }

                // Append suffix only if not disabled and not genuinely pending (empty original status)
                if (dataItem.IsEnabled &&
                    !displayStatusText.Equals("Disabled") &&
                    !(string.IsNullOrWhiteSpace(originalStatusText) && displayStatusText.Equals("Pending")))
                {
                    e.Value = displayStatusText + lastCheckedSuffix;
                }
                else
                {
                    e.Value = displayStatusText;
                }
                e.FormattingApplied = true;
            }

        }

        // --- Implementation of IRefreshableForm (ensure it calls LoadAllNetwatchData) ---
        public void RefreshDataViews()
        {
            LoadAllNetwatchData();
            Console.WriteLine($"NetwatchList: Refreshed all Netwatch data via IRefreshableForm at {DateTime.Now}");
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to start all currently displayed Netwatch configurations?",
                        "Confirm Start All", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool anyChangesMade = false;
                // Iterate through the items currently in the BindingList (which reflects the filtered view)
                foreach (NetwatchConfigDisplay config in _allNetwatchConfigsBindingList)
                {
                    if (!config.IsEnabled)
                    {
                        try
                        {
                            config.IsEnabled = true; // Update the local object
                            _netwatchConfigRepository.UpdateNetwatchConfigEnabledStatus(config.Id, true); // Update database
                            anyChangesMade = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error starting Netwatch '{config.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            config.IsEnabled = false; // Revert local object on error
                        }
                    }
                }

                if (anyChangesMade)
                {
                    // Refresh the relevant parts of the grid to show changes.
                    // The auto-refresh timer will eventually update the whole grid with server status,
                    // but this provides more immediate visual feedback for the IsEnabled state.
                    dgvNetwatchConfigs.Invalidate(); // Invalidates the whole grid, triggering repaint and CellFormatting
                    MessageBox.Show("Start command issued for all applicable Netwatch configurations. Statuses will be updated by the server.", "Start All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("All displayed Netwatch configurations are already enabled or there are no items to start.", "Start All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to stop all currently displayed Netwatch configurations?",
                        "Confirm Stop All", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool anyChangesMade = false;
                foreach (NetwatchConfigDisplay config in _allNetwatchConfigsBindingList)
                {
                    if (config.IsEnabled)
                    {
                        try
                        {
                            config.IsEnabled = false; // Update the local object
                            _netwatchConfigRepository.UpdateNetwatchConfigEnabledStatus(config.Id, false); // Update database
                            anyChangesMade = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error stopping Netwatch '{config.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            config.IsEnabled = true; // Revert local object on error
                        }
                    }
                }

                if (anyChangesMade)
                {
                    dgvNetwatchConfigs.Invalidate(); // Invalidates the whole grid
                    MessageBox.Show("Stop command issued for all applicable Netwatch configurations. Server will cease monitoring accordingly.", "Stop All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("All displayed Netwatch configurations are already disabled or there are no items to stop.", "Stop All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
