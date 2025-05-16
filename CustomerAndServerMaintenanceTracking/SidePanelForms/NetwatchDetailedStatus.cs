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

namespace CustomerAndServerMaintenanceTracking.SidePanelForms
{
    public partial class NetwatchDetailedStatus : Form
    {
        private int _netwatchConfigId;
        private string _netwatchConfigName; // To store the name for display
        private NetwatchConfigRepository _netwatchConfigRepository;
        private BindingList<IndividualIpStatus> _ipStatusBindingList;
        private bool _columnsAreSetup = false;

        // Parameterless constructor is good for forms, especially if designer is used.
        public NetwatchDetailedStatus()
        {
            InitializeComponent(); // This wires up designer controls and events (like a Load event if set)

            if (!this.DesignMode) // Good practice
            {
                _netwatchConfigRepository = new NetwatchConfigRepository();
            }
            _ipStatusBindingList = new BindingList<IndividualIpStatus>();

            if (this.dgvDetailedStatus != null) // dgvDetailedStatus is the name of your DataGridView
            {
                this.dgvDetailedStatus.DataSource = _ipStatusBindingList;
                // Enable Double Buffering
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, this.dgvDetailedStatus, new object[] { true });
            }

            // It's often better to call SetupDataGridViewColumns and LoadDetailedStatuses
            // via a public method after the form is created and its parameters are set,
            // or in the Form_Load event.
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

            _ipStatusBindingList.Clear();
            if (_netwatchConfigId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("NetwatchDetailedStatusForm: _netwatchConfigId is 0 or less. No data loaded.");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"NetwatchDetailedStatusForm: Loading detailed statuses for Config ID: {_netwatchConfigId}");
                List<IndividualIpStatus> statuses = _netwatchConfigRepository.GetDetailedIpStatuses(_netwatchConfigId); // This method should be in your repository
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
                    MessageBox.Show($"Error loading detailed IP statuses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void dgvDetailedStatus_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //if (this.DesignMode) return;
            //if (e.RowIndex < 0 || dgvDetailedStatus.Columns.Count == 0 || e.ColumnIndex < 0) return; // Added e.ColumnIndex check

            //// Existing logic for PingStatus column
            //if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "PingStatus")
            //{
            //    if (e.Value != null)
            //    {
            //        string status = e.Value.ToString().ToLower();
            //        // Default text color for this column if no other condition matches
            //        e.CellStyle.ForeColor = dgvDetailedStatus.DefaultCellStyle.ForeColor;

            //        if (status == "success") { e.CellStyle.ForeColor = Color.Green; }
            //        else if (status.Contains("timeout")) { e.CellStyle.ForeColor = Color.OrangeRed; }
            //        // Consider changing the next line to be more specific if "Red" is too general
            //        else if (status != "" && !status.Contains("no reply") && !status.Contains("error") && status != "success" && !status.Contains("timeout")) { e.CellStyle.ForeColor = Color.Red; }
            //        else if (status.Contains("no reply") || status.Contains("error")) { e.CellStyle.ForeColor = Color.DarkRed; }
            //        e.FormattingApplied = true; // Apply formatting for this column
            //    }
            //}
            //else if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "RoundtripTimeDisplay")
            //{
            //    if (e.RowIndex >= 0 && e.RowIndex < this.dgvDetailedStatus.Rows.Count)
            //    {
            //        DataGridViewRow row = this.dgvDetailedStatus.Rows[e.RowIndex];
            //        if (row.DataBoundItem is IndividualIpStatus ipStatus)
            //        {
            //            if (!ipStatus.RoundtripTimeMs.HasValue) // RTO state
            //            {
            //                TimeSpan rtoDuration = DateTime.Now - ipStatus.LastPingAttemptDateTime;
            //                string durationString = FormatRtoDuration(rtoDuration);
            //                e.Value = $"RTO {durationString}".TrimEnd(); // Display "RTO (duration)"
            //                e.CellStyle.ForeColor = Color.Red;
            //            }
            //            else
            //            {
            //                // Value is already formatted by RoundtripTimeDisplay property (e.g., "5 ms")
            //                // Reset ForeColor to default.
            //                e.CellStyle.ForeColor = dgvDetailedStatus.DefaultCellStyle.ForeColor;
            //                // e.Value will be what RoundtripTimeDisplay property returns, e.g. ipStatus.RoundtripTimeDisplay
            //            }
            //            e.FormattingApplied = true;
            //        }
            //    }
            //}


            if (this.DesignMode) return;
            if (e.RowIndex < 0 || dgvDetailedStatus.Columns.Count == 0 || e.ColumnIndex < 0) return;

            // Existing logic for PingStatus column
            if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "PingStatus")
            {
                if (e.Value != null)
                {
                    string status = e.Value.ToString().ToLower();
                    e.CellStyle.ForeColor = dgvDetailedStatus.DefaultCellStyle.ForeColor;

                    if (status == "success") { e.CellStyle.ForeColor = Color.Green; }
                    else if (status.Contains("timeout")) { e.CellStyle.ForeColor = Color.OrangeRed; }
                    else if (status != "" && !status.Contains("no reply") && !status.Contains("error") && status != "success" && !status.Contains("timeout")) { e.CellStyle.ForeColor = Color.Red; }
                    else if (status.Contains("no reply") || status.Contains("error")) { e.CellStyle.ForeColor = Color.DarkRed; }
                    e.FormattingApplied = true;
                }
            }
            // --- MODIFIED LOGIC for RTT column ("RoundtripTimeDisplay") ---
            else if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "RoundtripTimeDisplay")
            {
                if (e.RowIndex >= 0 && e.RowIndex < this.dgvDetailedStatus.Rows.Count)
                {
                    DataGridViewRow row = this.dgvDetailedStatus.Rows[e.RowIndex];
                    if (row.DataBoundItem is IndividualIpStatus ipStatus)
                    {
                        // Check the actual nullable long property: ipStatus.RoundtripTimeMs
                        if (!ipStatus.RoundtripTimeMs.HasValue) // This IP is currently in an RTO state based on NetwatchIpResults
                        {
                            string rtoText = "RTO";
                            // Now, try to get the actual start time of this ongoing outage
                            if (_netwatchConfigRepository != null && _netwatchConfigId > 0 && !string.IsNullOrEmpty(ipStatus.IpAddress))
                            {
                                try
                                {
                                    DateTime? outageStartTime = _netwatchConfigRepository.GetCurrentOutageStartTime(_netwatchConfigId, ipStatus.IpAddress);
                                    if (outageStartTime.HasValue)
                                    {
                                        TimeSpan rtoDuration = DateTime.Now - outageStartTime.Value;
                                        string durationString = FormatRtoDuration(rtoDuration); // Your helper method
                                        rtoText = $"RTO {durationString}".TrimEnd();
                                    }
                                    // else, no open outage found in log, just display "RTO"
                                }
                                catch (Exception ex)
                                {
                                    // Log error if querying outage log fails, still display basic RTO
                                    System.Diagnostics.Debug.WriteLine($"Error getting outage start time for {_netwatchConfigId}/{ipStatus.IpAddress}: {ex.Message}");
                                }
                            }
                            e.Value = rtoText;
                            e.CellStyle.ForeColor = Color.Red;
                        }
                        else
                        {
                            // Value is already formatted by RoundtripTimeDisplay property (e.g., "5 ms")
                            // Reset ForeColor to default.
                            e.CellStyle.ForeColor = dgvDetailedStatus.DefaultCellStyle.ForeColor;
                            // e.Value will be what RoundtripTimeDisplay property returns
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
