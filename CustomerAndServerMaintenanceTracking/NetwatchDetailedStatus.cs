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
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.SidePanelForms
{
    public partial class NetwatchDetailedStatus : Form, IRefreshableForm
    {
        private int _netwatchConfigId;
        private string _netwatchConfigName; // To store the name for display
        private NetwatchConfigRepository _netwatchConfigRepository;
        private BindingList<IndividualIpStatus> _ipStatusBindingList;
        private bool _columnsAreSetup = false;
        private readonly TagRepository _tagRepository; // Added: To pass to NetwatchConfigRepository
        private readonly ServiceLogRepository _logRepository;

        public void RefreshDataViews()
        {
            // Check if the form handle has been created and if the form is not disposed
            // to prevent errors if the refresh is called too early or after disposal.
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                // Ensure that LoadDetailedStatuses is called on the UI thread if it modifies UI elements directly
                // or if the underlying BindingList is modified from a non-UI thread.
                // If LoadDetailedStatuses itself handles invoking, this direct call is fine.
                // Otherwise, consider: this.BeginInvoke((MethodInvoker)delegate { LoadDetailedStatuses(); });
                LoadDetailedStatuses();
            }
        }


        // Parameterless constructor is good for forms, especially if designer is used.
        public NetwatchDetailedStatus(ServiceLogRepository logRepository, TagRepository tagRepository)
        {
            InitializeComponent();

            // Assign the passed-in repositories to your private fields
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));

            if (!this.DesignMode)
            {
                // Now _logRepository and _tagRepository will be valid instances
                _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, _tagRepository);
            }
            _ipStatusBindingList = new BindingList<IndividualIpStatus>();

            if (this.dgvDetailedStatus != null)
            {
                this.dgvDetailedStatus.DataSource = _ipStatusBindingList;
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, this.dgvDetailedStatus, new object[] { true });
            }
        }

        // Public method to initialize and load data AFTER the form is created
        public void InitializeAndLoadDetails(int netwatchConfigId, string netwatchConfigName)
        {
            _netwatchConfigId = netwatchConfigId;
            _netwatchConfigName = netwatchConfigName;

            if (this.lblNetwatchNameTitle != null) // Assuming you have a Label named lblNetwatchNameTitle
            {
                this.lblNetwatchNameTitle.Text = $"Details for: {_netwatchConfigName}";
            }
            else
            {
                // If you use the Form's Text property for the title (since FormBorderStyle is None, this might not be visible)
                // this.Text = $"Details for: {_netwatchConfigName}";
                System.Diagnostics.Debug.WriteLine("NetwatchDetailedStatusForm: lblNetwatchNameTitle is null.");
            }

            SetupDataGridViewColumns(); // Ensure columns are defined before data binding
            LoadDetailedStatuses();     // Load the actual data
        }


        private void SetupDataGridViewColumns()
        {
            if (_columnsAreSetup || dgvDetailedStatus == null || dgvDetailedStatus.IsDisposed) return;

            dgvDetailedStatus.AutoGenerateColumns = false;
            dgvDetailedStatus.Columns.Clear();

            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntityName", HeaderText = "Monitored Entity", DataPropertyName = "EntityName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "IpAddress", HeaderText = "IP Address", DataPropertyName = "IpAddress", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "PingStatus", HeaderText = "Status", DataPropertyName = "PingStatus", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoundtripTimeDisplay", HeaderText = "RTT", DataPropertyName = "RoundtripTimeDisplay", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastPingAttemptDateTime", HeaderText = "Last Attempt", DataPropertyName = "LastPingAttemptDateTime", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25, DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });

            // Ensure CellFormatting event is wired up (can also be done in designer)
            this.dgvDetailedStatus.CellFormatting -= dgvDetailedStatus_CellFormatting; // Prevent multiple
            this.dgvDetailedStatus.CellFormatting += dgvDetailedStatus_CellFormatting;

            _columnsAreSetup = true;
        }

        private void LoadDetailedStatuses()
        {
            if (this.DesignMode || _netwatchConfigRepository == null) return;
            if (dgvDetailedStatus == null || dgvDetailedStatus.IsDisposed) return;

            // --- Start: Preserve Scroll and Selection ---
            int firstDisplayedScrollingRowIndex = -1;
            int selectedRowIndex = -1;
            object selectedRowDataBoundItem = null; // To re-select based on data if IDs are stable

            if (dgvDetailedStatus.Rows.Count > 0)
            {
                firstDisplayedScrollingRowIndex = dgvDetailedStatus.FirstDisplayedScrollingRowIndex;
                if (dgvDetailedStatus.CurrentRow != null)
                {
                    selectedRowIndex = dgvDetailedStatus.CurrentRow.Index;
                    // If you have a stable ID on IndividualIpStatus, you could store that instead of index
                    // For example, if IndividualIpStatus had a unique ID:
                    // if (dgvDetailedStatus.CurrentRow.DataBoundItem is IndividualIpStatus currentItem)
                    // {
                    //    selectedRowDataBoundItem = currentItem.SomeUniqueId; 
                    // }
                }
            }
            // --- End: Preserve Scroll and Selection ---

            // Disable list change events during bulk update for performance
            bool originalRaiseListChangedEvents = _ipStatusBindingList.RaiseListChangedEvents;
            _ipStatusBindingList.RaiseListChangedEvents = false;
            _ipStatusBindingList.Clear();

            if (_netwatchConfigId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("NetwatchDetailedStatusForm: _netwatchConfigId is 0 or less. No data loaded.");
                _ipStatusBindingList.RaiseListChangedEvents = originalRaiseListChangedEvents; // Re-enable events
                _ipStatusBindingList.ResetBindings(); // Notify grid if it was previously bound
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"NetwatchDetailedStatusForm: Loading detailed statuses for Config ID: {_netwatchConfigId}");
                List<IndividualIpStatus> statuses = _netwatchConfigRepository.GetDetailedIpStatuses(_netwatchConfigId);
                if (statuses != null)
                {
                    System.Diagnostics.Debug.WriteLine($"NetwatchDetailedStatusForm: Found {statuses.Count} IP statuses for Config ID: {_netwatchConfigId}");
                    foreach (var status in statuses)
                    {
                        _ipStatusBindingList.Add(status);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"NetwatchDetailedStatusForm: GetDetailedIpStatuses returned null for Config ID: {_netwatchConfigId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NetwatchDetailedStatusForm: Error loading detailed IP statuses for Config ID {_netwatchConfigId}: {ex.Message}");
                if (!this.DesignMode)
                {
                    // Consider if a MessageBox is appropriate here or just logging, as it can be disruptive if it keeps popping up.
                    // MessageBox.Show($"Error loading detailed IP statuses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _ipStatusBindingList.RaiseListChangedEvents = originalRaiseListChangedEvents; // Re-enable events
                _ipStatusBindingList.ResetBindings(); // This is crucial to tell the DGV to update itself from the BindingList
            }

            // --- Start: Restore Scroll and Selection ---
            if (dgvDetailedStatus.Rows.Count > 0)
            {
                if (firstDisplayedScrollingRowIndex != -1 && firstDisplayedScrollingRowIndex < dgvDetailedStatus.Rows.Count)
                {
                    dgvDetailedStatus.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
                }

                // Attempt to re-select the previously selected row (more robust if using a stable ID)
                if (selectedRowIndex != -1)
                {
                    if (selectedRowIndex < dgvDetailedStatus.Rows.Count)
                    {
                        // Simple re-selection by index (might not be the same item if data changed order)
                        dgvDetailedStatus.ClearSelection();
                        dgvDetailedStatus.Rows[selectedRowIndex].Selected = true;
                        if (dgvDetailedStatus.Columns.GetFirstColumn(DataGridViewElementStates.Visible) != null)
                        {
                            dgvDetailedStatus.CurrentCell = dgvDetailedStatus.Rows[selectedRowIndex].Cells[dgvDetailedStatus.Columns.GetFirstColumn(DataGridViewElementStates.Visible).Index];
                        }
                    }
                    // Else if re-selecting by data item ID:
                    // if (selectedRowDataBoundItem != null)
                    // {
                    //    for (int i = 0; i < dgvDetailedStatus.Rows.Count; i++)
                    //    {
                    //        if (dgvDetailedStatus.Rows[i].DataBoundItem is IndividualIpStatus item && item.SomeUniqueId.Equals(selectedRowDataBoundItem))
                    //        {
                    //            dgvDetailedStatus.ClearSelection();
                    //            dgvDetailedStatus.Rows[i].Selected = true;
                    //            dgvDetailedStatus.CurrentCell = dgvDetailedStatus.Rows[i].Cells[0]; // Or first visible cell
                    //            break;
                    //        }
                    //    }
                    // }
                }
            }
        }

        private void dgvDetailedStatus_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.DesignMode) return;
            if (e.RowIndex < 0 || dgvDetailedStatus.Rows[e.RowIndex].DataBoundItem == null || dgvDetailedStatus.Columns.Count == 0 || e.ColumnIndex < 0) return;

            if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "PingStatus")
            {
                if (e.Value != null)
                {
                    string status = e.Value.ToString().ToLower();
                    e.CellStyle.ForeColor = dgvDetailedStatus.DefaultCellStyle.ForeColor; // Reset to default

                    if (status == "success") { e.CellStyle.ForeColor = Color.Green; }
                    else if (status.Contains("timeout")) { e.CellStyle.ForeColor = Color.OrangeRed; }
                    else if (status != "" && !status.Contains("noreply") && !status.Contains("error") && !status.Equals("pending")) { e.CellStyle.ForeColor = Color.Red; } // General failures
                    else if (status.Contains("noreply") || status.Contains("error")) { e.CellStyle.ForeColor = Color.DarkRed; }
                    else if (status.Equals("pending")) { e.CellStyle.ForeColor = Color.DarkOrange; } // Pending color
                    e.FormattingApplied = true;
                }
            }
            else if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "RoundtripTimeDisplay")
            {
                if (e.RowIndex >= 0 && e.RowIndex < this.dgvDetailedStatus.Rows.Count)
                {
                    DataGridViewRow row = this.dgvDetailedStatus.Rows[e.RowIndex];
                    if (row.DataBoundItem is IndividualIpStatus ipStatus)
                    {
                        if (!ipStatus.RoundtripTimeMs.HasValue && ipStatus.LastPingStatus.ToLower() != "success" && ipStatus.LastPingStatus.ToLower() != "pending") // IP is RTO or some other failure
                        {
                            TimeSpan currentIpRtoDuration = TimeSpan.Zero;
                            DateTime? currentIpOutageStartTime = null;

                            // Get current open outage for THIS IP
                            if (_netwatchConfigRepository != null && _netwatchConfigId > 0 && !string.IsNullOrEmpty(ipStatus.IpAddress))
                            {
                                try
                                {
                                    currentIpOutageStartTime = _netwatchConfigRepository.GetCurrentOutageStartTime(_netwatchConfigId, ipStatus.IpAddress);
                                    if (currentIpOutageStartTime.HasValue)
                                    {
                                        currentIpRtoDuration = DateTime.Now - currentIpOutageStartTime.Value;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error getting current outage start time for {_netwatchConfigId}/{ipStatus.IpAddress}: {ex.Message}");
                                }
                            }

                            // Get historical total RTO for this ENTITY (excluding current IP's outages)
                            TimeSpan historicalEntityRtoDuration = TimeSpan.Zero;
                            if (_netwatchConfigRepository != null && _netwatchConfigId > 0 && !string.IsNullOrEmpty(ipStatus.EntityName) && ipStatus.EntityName != "N/A")
                            {
                                try
                                {
                                    historicalEntityRtoDuration = _netwatchConfigRepository.GetTotalHistoricalOutageDurationForEntity(_netwatchConfigId, ipStatus.EntityName, ipStatus.IpAddress);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error getting historical outage duration for entity {_netwatchConfigId}/{ipStatus.EntityName}: {ex.Message}");
                                }
                            }

                            TimeSpan totalCombinedRto = currentIpRtoDuration + historicalEntityRtoDuration;

                            string rtoText = "RTO";
                            if (totalCombinedRto > TimeSpan.Zero)
                            {
                                rtoText = $"RTO {FormatRtoDuration(totalCombinedRto)}".TrimEnd();
                            }
                            else if (ipStatus.LastPingStatus.ToLower() != "pending")
                            { // If not pending and no RTO calculated, but not success
                                rtoText = ipStatus.LastPingStatus; // Show actual status if not explicitly RTO and not success/pending
                            }


                            e.Value = rtoText;
                            e.CellStyle.ForeColor = Color.Red; // Keep it red for any non-success RTT
                        }
                        else if (ipStatus.LastPingStatus.ToLower() == "pending")
                        {
                            e.Value = "Pending";
                            e.CellStyle.ForeColor = Color.DarkOrange;
                        }
                        else if (ipStatus.LastPingStatus.ToLower() == "no ip")
                        {
                            e.Value = "No IP"; // Explicitly set "No IP" for RTT column
                            e.CellStyle.ForeColor = Color.DimGray;
                            e.CellStyle.Font = new Font(dgvDetailedStatus.DefaultCellStyle.Font, FontStyle.Italic);
                        }

                        else // Successful ping or explicitly "N/A"
                        {
                            e.Value = ipStatus.RoundtripTimeDisplay; // Should be "X ms" or "N/A"
                            e.CellStyle.ForeColor = dgvDetailedStatus.DefaultCellStyle.ForeColor;
                        }
                        e.FormattingApplied = true;
                    }
                }
            }

        }

        // Add this helper method inside the NetwatchDetailedStatus class
        private string FormatRtoDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"({(int)duration.TotalDays}d)";
            }
            if (duration.TotalHours >= 1)
            {
                return $"({(int)duration.TotalHours}hr)";
            }
            if (duration.TotalMinutes >= 1)
            {
                return $"({(int)duration.TotalMinutes}min)";
            }
            // Optional: for very short durations, you might not want to display anything or just (<1min)
            // For simplicity, if it's less than a minute but still some seconds, show (<1min)
            if (duration.TotalSeconds > 0)
            {
                return "(<1min)";
            }
            return ""; // Or "(now)" if duration is zero or negative (shouldn't happen often)
        }

    }
}
