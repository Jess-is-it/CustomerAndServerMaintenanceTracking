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

namespace CustomerAndServerMaintenanceTracking.UserControl
{
    public partial class UC_NetwatchDetailedStatus : System.Windows.Forms.UserControl
    {
        private int _netwatchConfigId;
        private string _initialNetwatchConfigName;
        private NetwatchConfigRepository _netwatchConfigRepository;
        private BindingList<IndividualIpStatus> _ipStatusBindingList;

        public UC_NetwatchDetailedStatus(int netwatchConfigId, string netwatchConfigName)
        {
            InitializeComponent();

            if (!this.DesignMode)
            {
                _netwatchConfigRepository = new NetwatchConfigRepository();
            }
            _ipStatusBindingList = new BindingList<IndividualIpStatus>();

            if (this.dgvDetailedStatus != null)
            {
                this.dgvDetailedStatus.DataSource = _ipStatusBindingList;
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, this.dgvDetailedStatus, new object[] { true });

                // Setup columns and event handlers here or in Load event
                // Doing it here ensures it's done once after InitializeComponent
                SetupDataGridViewColumns();
            }

        }

        private void UC_NetwatchDetailedStatus_Load(object sender, EventArgs e)
        {
            if (lblNetwatchNameTitle != null)
            {
                lblNetwatchNameTitle.Text = $"Details for: {_initialNetwatchConfigName}";
            }
            // Make sure columns are set up before loading data that relies on those columns.
            SetupDataGridViewColumns();
            LoadDetailedStatuses();
        }
      
        // SetupDataGridViewColumns should only be called once.
        private bool _columnsSetup = false; // Flag to ensure columns are set up only once
        private void SetupDataGridViewColumns()
        {
            if (_columnsSetup || dgvDetailedStatus == null || dgvDetailedStatus.IsDisposed) return;

            dgvDetailedStatus.AutoGenerateColumns = false;
            dgvDetailedStatus.Columns.Clear();

            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntityName", HeaderText = "Monitored Entity", DataPropertyName = "EntityName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "IpAddress", HeaderText = "IP Address", DataPropertyName = "IpAddress", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 25 });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "PingStatus", HeaderText = "Status", DataPropertyName = "PingStatus", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 20 });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoundtripTimeDisplay", HeaderText = "RTT", DataPropertyName = "RoundtripTimeDisplay", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastPingAttemptDateTime", HeaderText = "Last Attempt", DataPropertyName = "LastPingAttemptDateTime", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 25, DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });

            this.dgvDetailedStatus.CellFormatting -= dgvDetailedStatus_CellFormatting; // Prevent multiple
            this.dgvDetailedStatus.CellFormatting += dgvDetailedStatus_CellFormatting;

            _columnsSetup = true;
        }

        private void LoadDetailedStatuses()
        {
            if (this.DesignMode || _netwatchConfigRepository == null) return;
            if (dgvDetailedStatus == null || dgvDetailedStatus.IsDisposed) return;

            _ipStatusBindingList.Clear();
            if (_netwatchConfigId <= 0) return;

            try
            {
                List<IndividualIpStatus> statuses = _netwatchConfigRepository.GetDetailedIpStatuses(_netwatchConfigId);
                if (statuses != null)
                {
                    foreach (var status in statuses)
                    {
                        _ipStatusBindingList.Add(status);
                    }
                }
                if (!_ipStatusBindingList.Any())
                {
                    // Optional: Display a message in the grid if no details are found
                    // This requires more complex handling, e.g., showing a label over the grid.
                    // For now, it will just be an empty grid.
                }
            }
            catch (Exception ex)
            {
                if (!this.DesignMode)
                {
                    MessageBox.Show($"Error loading detailed IP statuses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Optional: Add a CellFormatting event for dgvDetailedStatus for status coloring
        private void dgvDetailedStatus_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.DesignMode) return;
            if (e.RowIndex < 0 || dgvDetailedStatus.Columns.Count == 0) return;

            if (dgvDetailedStatus.Columns[e.ColumnIndex].Name == "PingStatus")
            {
                if (e.Value != null)
                {
                    string status = e.Value.ToString().ToLower();
                    if (status == "success") { e.CellStyle.ForeColor = Color.Green; }
                    else if (status.Contains("timeout")) { e.CellStyle.ForeColor = Color.OrangeRed; }
                    else if (status != "" && !status.Contains("no reply") && !status.Contains("error")) { e.CellStyle.ForeColor = Color.Red; } // General failures
                    else if (status.Contains("no reply") || status.Contains("error")) { e.CellStyle.ForeColor = Color.DarkRed; }
                }
            }
        }


    }
}
