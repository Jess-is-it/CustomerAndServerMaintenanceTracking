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
using SharedLibrary.Models;
using SharedLibrary.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class NetwatchList : Form, IRefreshableForm
    {
        private NetwatchConfigRepository _netwatchConfigRepository;
        private List<NetwatchConfigDisplay> _allNetwatchConfigsMasterList;
        private BindingList<NetwatchConfigDisplay> _allNetwatchConfigsBindingList;
        private readonly ServiceLogRepository _logRepository; // Added for NetwatchConfigRepository
        private readonly TagRepository _tagRepository; // Added for NetwatchConfigRepository

        private bool _isNetwatchServiceOnline = true;
        private DateTime _lastKnownServiceHeartbeat = DateTime.MinValue;
        private const int ServiceOfflineThresholdSeconds = 180;

        private OverlayForm _overlayForm;

        // Constructor updated to accept repositories
        public NetwatchList(ServiceLogRepository logger, TagRepository tagRepository)
        {
            InitializeComponent();
            _logRepository = logger ?? throw new ArgumentNullException(nameof(logger));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository)); // Store TagRepository

            // Pass both to NetwatchConfigRepository constructor
            _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, _tagRepository);

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
                LoadAllNetwatchData();

                this.dgvNetwatchConfigs.CellClick -= dgvNetwatchConfigs_CellClick;
                this.dgvNetwatchConfigs.CellClick += dgvNetwatchConfigs_CellClick;

                this.dgvNetwatchConfigs.CellFormatting -= dgvNetwatchConfigs_CellFormatting;
                this.dgvNetwatchConfigs.CellFormatting += dgvNetwatchConfigs_CellFormatting;

                if (this.txtSearch != null)
                {
                    this.txtSearch.TextChanged -= TxtSearch_TextChanged;
                    this.txtSearch.TextChanged += TxtSearch_TextChanged;
                }

            }
            if (this.dgvNetwatchConfigs != null)
            {
                // ... your existing code ...
                this.dgvNetwatchConfigs.MouseDown -= dgvNetwatchConfigs_MouseDown;
                this.dgvNetwatchConfigs.MouseDown += dgvNetwatchConfigs_MouseDown;
            }

        }

        private void SetupAllNetwatchDataGridView()
        {
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

        public void LoadAllNetwatchData()
        {
            if (this.dgvNetwatchConfigs == null || _netwatchConfigRepository == null) return;

            try
            {
                DateTime? lastHeartbeat = _netwatchConfigRepository.GetLastServiceHeartbeat("NetwatchPingerService");
                if (lastHeartbeat.HasValue)
                {
                    _lastKnownServiceHeartbeat = lastHeartbeat.Value;
                    _isNetwatchServiceOnline = (DateTime.Now - _lastKnownServiceHeartbeat).TotalSeconds <= ServiceOfflineThresholdSeconds;
                }
                else
                {
                    _isNetwatchServiceOnline = false;
                }
            }
            catch (Exception ex)
            {
                _isNetwatchServiceOnline = false;
                Debug.WriteLine($"NetwatchList: Error fetching service heartbeat: {ex.Message}. Service considered OFFLINE.");
            }

            try
            {
                _allNetwatchConfigsMasterList = _netwatchConfigRepository.GetNetwatchConfigsForDisplay();
                ApplyFilterAndRefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading all Netwatch configurations: {ex.Message}",
                                "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allNetwatchConfigsMasterList.Clear();
                _allNetwatchConfigsBindingList.Clear();
            }
            dgvNetwatchConfigs.Invalidate();
        }

        private void ApplyFilterAndRefreshGrid()
        {
            if (this.dgvNetwatchConfigs == null) return;

            int? selectedConfigId = null;
            if (dgvNetwatchConfigs.CurrentRow != null && dgvNetwatchConfigs.CurrentRow.DataBoundItem is NetwatchConfigDisplay currentDisp)
            {
                selectedConfigId = currentDisp.Id;
            }
            int firstDisplayIndex = dgvNetwatchConfigs.FirstDisplayedScrollingRowIndex;
            if (firstDisplayIndex < 0) firstDisplayIndex = 0;

            dgvNetwatchConfigs.SuspendLayout();
            _allNetwatchConfigsBindingList.RaiseListChangedEvents = false;

            _allNetwatchConfigsBindingList.Clear();

            string searchText = (this.txtSearch != null) ? this.txtSearch.Text.Trim().ToLowerInvariant() : string.Empty;

            IEnumerable<NetwatchConfigDisplay> filteredData = _allNetwatchConfigsMasterList;

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredData = _allNetwatchConfigsMasterList.Where(config =>
                    (config.NetwatchName?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (config.TargetSourceName?.ToLowerInvariant().Contains(searchText) ?? false) ||
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
            _allNetwatchConfigsBindingList.ResetBindings();

            if (dgvNetwatchConfigs.Rows.Count > 0)
            {
                if (firstDisplayIndex >= dgvNetwatchConfigs.Rows.Count)
                {
                    firstDisplayIndex = dgvNetwatchConfigs.Rows.Count - 1;
                }
                if (firstDisplayIndex < 0) firstDisplayIndex = 0;

                if (dgvNetwatchConfigs.Rows.Count > 0 && firstDisplayIndex < dgvNetwatchConfigs.Rows.Count)
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
                            if (dgvNetwatchConfigs.Columns.GetFirstColumn(DataGridViewElementStates.Visible)?.Index is int firstVisibleColIndex && firstVisibleColIndex != -1)
                            {
                                dgvNetwatchConfigs.CurrentCell = dgvNetwatchConfigs.Rows[rowIndexToSelect].Cells[firstVisibleColIndex];
                            }
                        }
                    }
                }
            }
            dgvNetwatchConfigs.ResumeLayout();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilterAndRefreshGrid();
        }

        private void ShowOverlay()
        {
            if (_overlayForm == null || _overlayForm.IsDisposed)
            {
                _overlayForm = new OverlayForm();
            }
            Form formToCover = this.MdiParent ?? this;
            _overlayForm.Owner = formToCover;
            _overlayForm.StartPosition = FormStartPosition.Manual;
            Point location = formToCover.PointToScreen(Point.Empty);
            _overlayForm.Bounds = new Rectangle(location, formToCover.ClientSize);
            _overlayForm.Show();
            _overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            if (_overlayForm != null && !_overlayForm.IsDisposed)
            {
                _overlayForm.Close();
                _overlayForm = null;
            }
        }

        private void btnAddNetwatch_Click(object sender, EventArgs e)
        {
            ShowOverlay();
            using (NetwatchAdd netwatchAddInitialForm = new NetwatchAdd()) // Assuming NetwatchAdd has ServiceLogRepository in constructor
            {
                // If NetwatchAdd now needs the logger, you'd pass it here:
                // using (NetwatchAdd netwatchAddInitialForm = new NetwatchAdd(_logRepository, _tagRepository)) 
                netwatchAddInitialForm.StartPosition = FormStartPosition.CenterParent;
                netwatchAddInitialForm.ShowDialog(this);
            }
            CloseOverlay();
            LoadAllNetwatchData();
        }

        private void dgvNetwatchConfigs_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvNetwatchConfigs.Rows.Count <= e.RowIndex) return;

            NetwatchConfigDisplay selectedConfig = dgvNetwatchConfigs.Rows[e.RowIndex].DataBoundItem as NetwatchConfigDisplay;
            if (selectedConfig == null) return;

            if (dgvNetwatchConfigs.Columns[e.ColumnIndex].Name == "LastStatus")
            {
                if (this.MdiParent is Dashboard mainDashboard)
                {
                    string statusToSend = !selectedConfig.IsEnabled ? "Disabled" : (selectedConfig.LastStatus ?? "Unknown");
                    mainDashboard.ShowNetwatchDetailInPanel(selectedConfig.Id, selectedConfig.NetwatchName, statusToSend);
                }
                return;
            }

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

                    switch (clickedButton)
                    {
                        case ActionDataGridViewMultiButtonCell.ActionButtonType.StartStop:
                            selectedConfig.IsEnabled = !selectedConfig.IsEnabled;
                            try
                            {
                                _netwatchConfigRepository.UpdateNetwatchConfigEnabledStatus(selectedConfig.Id, selectedConfig.IsEnabled);
                                dgvNetwatchConfigs.InvalidateCell(e.ColumnIndex, e.RowIndex);
                                if (dgvNetwatchConfigs.Columns["LastStatus"]?.Index is int statusColIndex && statusColIndex != -1)
                                {
                                    dgvNetwatchConfigs.InvalidateCell(statusColIndex, e.RowIndex);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error updating Netwatch status for '{selectedConfig.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                selectedConfig.IsEnabled = !selectedConfig.IsEnabled;
                                dgvNetwatchConfigs.InvalidateCell(e.ColumnIndex, e.RowIndex);
                                if (dgvNetwatchConfigs.Columns["LastStatus"]?.Index is int statusColIndex && statusColIndex != -1)
                                {
                                    dgvNetwatchConfigs.InvalidateCell(statusColIndex, e.RowIndex);
                                }
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

        private void dgvNetwatchConfigs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvNetwatchConfigs.Columns.Count == 0 || e.ColumnIndex < 0 || dgvNetwatchConfigs.Columns[e.ColumnIndex].Name != "LastStatus")
            {
                // Also ensure it's the "LastStatus" column if you have column-specific formatting
                return;
            }


            NetwatchConfigDisplay dataItem = dgvNetwatchConfigs.Rows[e.RowIndex].DataBoundItem as NetwatchConfigDisplay;

            if (dataItem != null)
            {
                e.CellStyle.ForeColor = SystemColors.ControlText;
                e.CellStyle.BackColor = (e.RowIndex % 2 == 0) ? SystemColors.Window : SystemColors.ControlLight;
                e.CellStyle.Font = dgvNetwatchConfigs.DefaultCellStyle.Font;
                e.FormattingApplied = false;

                string originalStatusText = dataItem.LastStatus ?? string.Empty;
                string displayStatusText = originalStatusText;
                string lastCheckedSuffix = "";

                if (!_isNetwatchServiceOnline)
                {
                    e.Value = "Netwatch Service Offline";
                    e.CellStyle.ForeColor = Color.White;
                    e.CellStyle.BackColor = Color.DarkSlateGray;
                    e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Bold | FontStyle.Italic);
                    e.FormattingApplied = true;
                    return;
                }

                if (!dataItem.IsEnabled)
                {
                    displayStatusText = "Disabled";
                    e.CellStyle.ForeColor = Color.DimGray;
                    e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Italic);
                }
                else
                {
                    if (dataItem.LastChecked.HasValue)
                    {
                        TimeSpan timeSinceLastCheck = DateTime.Now - dataItem.LastChecked.Value;
                        if (timeSinceLastCheck.TotalSeconds < 5) lastCheckedSuffix = " (just updated)";
                        else if (timeSinceLastCheck.TotalMinutes < 1) lastCheckedSuffix = $" ({timeSinceLastCheck.TotalSeconds:F0}s ago)";
                        else if (timeSinceLastCheck.TotalHours < 1) lastCheckedSuffix = $" ({timeSinceLastCheck.TotalMinutes:F0}m ago)";
                        else if (timeSinceLastCheck.TotalDays < 1) lastCheckedSuffix = $" ({timeSinceLastCheck.TotalHours:F0}h ago)";
                        else lastCheckedSuffix = $" ({dataItem.LastChecked:MMM d, h:mm tt})";
                    }
                    else
                    {
                        lastCheckedSuffix = " (never checked)";
                    }

                    string lowerStatus = originalStatusText.ToLowerInvariant();

                    if (lowerStatus.Contains("all") && lowerStatus.Contains("  "))
                    {
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.FromArgb(40, 167, 69); // Green
                    }
                    else if (lowerStatus.StartsWith("partial") && lowerStatus.Contains("up"))
                    {
                        try
                        {
                            var parts = originalStatusText.Split(' ')[1].Split('/');
                            if (parts.Length == 2 && int.TryParse(parts[0], out int upCountParsed) && upCountParsed > 0)
                            {
                                e.CellStyle.ForeColor = Color.Black;
                                e.CellStyle.BackColor = Color.FromArgb(255, 193, 7); // Yellow
                            }
                            else
                            {
                                e.CellStyle.ForeColor = Color.White;
                                e.CellStyle.BackColor = Color.FromArgb(220, 53, 69); // Red
                            }
                        }
                        catch
                        {
                            e.CellStyle.ForeColor = Color.Black;
                            e.CellStyle.BackColor = Color.FromArgb(255, 193, 7);
                        }
                    }
                    else if (lowerStatus.Contains("down") || lowerStatus.Contains("timeout"))
                    {
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.FromArgb(220, 53, 69); // Red
                    }
                    else if (lowerStatus.Contains("no ip") || lowerStatus.Contains("no entities"))
                    {
                        e.CellStyle.ForeColor = Color.DarkGray;
                        e.CellStyle.Font = new Font(dgvNetwatchConfigs.DefaultCellStyle.Font, FontStyle.Italic);
                    }
                    else if (lowerStatus.StartsWith("error:"))
                    {
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.FromArgb(200, 35, 51);
                    }
                }

                e.Value = displayStatusText + lastCheckedSuffix;
                e.FormattingApplied = true;
            }
            else
            {
                e.FormattingApplied = false;
            }
        }

        public void RefreshDataViews()
        {
            LoadAllNetwatchData();
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to start all currently displayed Netwatch configurations?",
                        "Confirm Start All", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool anyChangesMade = false;
                foreach (NetwatchConfigDisplay config in _allNetwatchConfigsBindingList)
                {
                    if (!config.IsEnabled)
                    {
                        try
                        {
                            config.IsEnabled = true;
                            _netwatchConfigRepository.UpdateNetwatchConfigEnabledStatus(config.Id, true);
                            anyChangesMade = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error starting Netwatch '{config.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            config.IsEnabled = false;
                        }
                    }
                }

                if (anyChangesMade)
                {
                    dgvNetwatchConfigs.Invalidate();
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
                            config.IsEnabled = false;
                            _netwatchConfigRepository.UpdateNetwatchConfigEnabledStatus(config.Id, false);
                            anyChangesMade = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error stopping Netwatch '{config.NetwatchName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            config.IsEnabled = true;
                        }
                    }
                }

                if (anyChangesMade)
                {
                    dgvNetwatchConfigs.Invalidate();
                    MessageBox.Show("Stop command issued for all applicable Netwatch configurations. Server will cease monitoring accordingly.", "Stop All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("All displayed Netwatch configurations are already disabled or there are no items to stop.", "Stop All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

        }
        private void OpenAddNotificationRuleForm(NetwatchConfigDisplay selectedConfig)
        {
            if (selectedConfig == null)
            {
                MessageBox.Show("Please select a Netwatch configuration first.", "No Netwatch Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // We'll enhance this later to pass more info to AddNotificationRule
            // For now, it just opens the form.
            // The AddNotificationRule constructor will need to be adapted in the next step.
            AddNotificationRule addNotificationRuleForm = new AddNotificationRule(selectedConfig.NetwatchName, selectedConfig.Id, "Netwatch"); // Example: Passing Name, ID and SourceType

            // ShowOverlay(); // If you have ShowOverlay/CloseOverlay methods and want to use them
            addNotificationRuleForm.Owner = this.MdiParent ?? this; // Set owner to MDI parent or self
            addNotificationRuleForm.StartPosition = FormStartPosition.CenterParent;
            addNotificationRuleForm.ShowDialog();
            // CloseOverlay();
        }
        private void addNotificationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgvNetwatchConfigs.CurrentRow != null && dgvNetwatchConfigs.CurrentRow.DataBoundItem is NetwatchConfigDisplay selectedConfig)
            {
                OpenAddNotificationRuleForm(selectedConfig);
            }
            else
            {
                MessageBox.Show("Please select a Netwatch configuration from the list first.", "No Netwatch Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void dgvNetwatchConfigs_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTestInfo = dgvNetwatchConfigs.HitTest(e.X, e.Y);
                if (hitTestInfo.Type == DataGridViewHitTestType.Cell && hitTestInfo.RowIndex >= 0) // Ensure it's a cell click
                {
                    // If the row right-clicked is not already the current row, make it current.
                    if (dgvNetwatchConfigs.CurrentRow == null || dgvNetwatchConfigs.CurrentRow.Index != hitTestInfo.RowIndex)
                    {
                        dgvNetwatchConfigs.CurrentCell = dgvNetwatchConfigs[hitTestInfo.ColumnIndex, hitTestInfo.RowIndex];
                    }
                    // Ensure the row is selected (useful if SelectionMode is FullRowSelect)
                    dgvNetwatchConfigs.Rows[hitTestInfo.RowIndex].Selected = true;
                }
            }
        }
        private void addNotificationContextMenuItem_Click(object sender, EventArgs e)
        {
            // The MouseDown event should have already set the CurrentRow.
            // If dgvNetwatchConfigs.CurrentRow is null here, it means the MouseDown logic
            // might not be working as expected or the context menu is appearing without a row selection.
            if (dgvNetwatchConfigs.CurrentRow != null &&
                dgvNetwatchConfigs.CurrentRow.DataBoundItem is NetwatchConfigDisplay selectedConfig)
            {
                OpenAddNotificationRuleForm(selectedConfig); // Call the same helper method
            }
            else
            {
                // This message might appear if the MouseDown event isn't correctly selecting the row
                // before the context menu shows, or if the DGV is empty.
                MessageBox.Show("Please ensure a Netwatch configuration is properly selected in the grid.",
                                "No Netwatch Selected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        }

    }

}

