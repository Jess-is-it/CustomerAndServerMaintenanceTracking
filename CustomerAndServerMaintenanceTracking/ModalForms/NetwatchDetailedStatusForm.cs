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

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class NetwatchDetailedStatusForm : Form
    {
        private int _netwatchConfigId;
        private string _netwatchConfigName;
        private NetwatchConfigRepository _netwatchConfigRepository;
        private BindingList<IndividualIpStatus> _ipStatusBindingList;

        public NetwatchDetailedStatusForm(int netwatchConfigId, string netwatchConfigName)
        {
            InitializeComponent();
            _netwatchConfigId = netwatchConfigId;
            _netwatchConfigName = netwatchConfigName; // To display in the title or a label
            _netwatchConfigRepository = new NetwatchConfigRepository();
            _ipStatusBindingList = new BindingList<IndividualIpStatus>();

            // Set the DataSource for the DataGridView
            this.dgvDetailedStatus.DataSource = _ipStatusBindingList;

            // Enable Double Buffering for dgvDetailedStatus
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, this.dgvDetailedStatus, new object[] { true });
        }

        private void NetwatchDetailedStatusForm_Load(object sender, EventArgs e)
        {
            if (lblNetwatchName != null) // Check if the label exists
            {
                lblNetwatchName.Text = $"Status Details for: {_netwatchConfigName}";
            }
            this.Text = $"Details: {_netwatchConfigName}"; // Set form title

            SetupDataGridViewColumns();
            LoadDetailedStatuses();
        }

        private void SetupDataGridViewColumns()
        {
            dgvDetailedStatus.AutoGenerateColumns = false;
            dgvDetailedStatus.Columns.Clear();

            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntityName",
                HeaderText = "Monitored Entity",
                DataPropertyName = "EntityName",
                FillWeight = 30,
                MinimumWidth = 150
            });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IpAddress",
                HeaderText = "IP Address",
                DataPropertyName = "IpAddress",
                FillWeight = 25,
                MinimumWidth = 100
            });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PingStatus",
                HeaderText = "Status",
                DataPropertyName = "PingStatus",
                FillWeight = 20,
                MinimumWidth = 100
            });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RoundtripTimeDisplay", // Use the display property from the model
                HeaderText = "RTT",
                DataPropertyName = "RoundtripTimeDisplay",
                FillWeight = 10,
                MinimumWidth = 60,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            dgvDetailedStatus.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LastPingAttemptDateTime",
                HeaderText = "Last Attempt",
                DataPropertyName = "LastPingAttemptDateTime",
                FillWeight = 25,
                MinimumWidth = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } // General date/time short
            });
        }

        private void LoadDetailedStatuses()
        {
            _ipStatusBindingList.Clear();
            try
            {
                List<IndividualIpStatus> statuses = _netwatchConfigRepository.GetDetailedIpStatuses(_netwatchConfigId);
                foreach (var status in statuses)
                {
                    _ipStatusBindingList.Add(status);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading detailed IP statuses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Optional: Add a CellFormatting event for dgvDetailedStatus for status coloring
        private void dgvDetailedStatus_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Color code the "PingStatus" column
            if (dgvDetailedStatus.Columns[e.ColumnIndex].DataPropertyName == "PingStatus")
            {
                if (e.Value != null)
                {
                    string status = e.Value.ToString().ToLower();
                    if (status == "success")
                    {
                        e.CellStyle.ForeColor = Color.Green;
                    }
                    else if (status.Contains("timeout"))
                    {
                        e.CellStyle.ForeColor = Color.OrangeRed;
                    }
                    else if (status != "") // Other errors
                    {
                        e.CellStyle.ForeColor = Color.Red;
                    }
                }
            }
        }


        private void btnClose_Click(object sender, EventArgs e) // Assuming your button is named btnClose
        {
            this.Close();
        }
    }
}
