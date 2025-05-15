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

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddNetwatchConfigForm : Form
    {
        // Properties to receive data from the calling form (NetwatchAdd)
        public int SourceNetworkClusterId { get; set; }
        public string SourceNetworkClusterName { get; set; }
        public int SourceTagId { get; set; }
        public string SourceTagName { get; set; }
        public List<int> InitialTagIdsToMonitor { get; set; } = new List<int>();
        public NetwatchSourceType CurrentSourceType { get; set; }

        // Event to notify that a Netwatch configuration has been saved.
        public event EventHandler NetwatchConfigSaved;

        public AddNetwatchConfigForm()
        {
            InitializeComponent();
            // Set default values as per your designer/previous discussion
            // comboBox1 for Type should be pre-filled and potentially disabled if only ICMP is supported
            comboBox1.Items.Add("ICMP");
            comboBox1.SelectedItem = "ICMP";
            comboBox1.Enabled = false; // Only ICMP for now

            // Default values for interval and timeout based on our agreement
            txtInterval.Text = "1"; // Default to 1 second
            txtTimeout.Text = "1000"; // Default to 1000 milliseconds
            this.btnSaveNetwatch.Click += btnSaveNetwatch_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        private void AddNetwatchConfigForm_Load(object sender, EventArgs e)
        {
            switch (CurrentSourceType)
            {
                case NetwatchSourceType.NetworkCluster:
                    lbltitle.Text = "Add Netwatch for Network Cluster";
                    break;
                case NetwatchSourceType.Customer:
                    lbltitle.Text = "Add Netwatch for Customer";
                    break;
                case NetwatchSourceType.DeviceIP:
                    lbltitle.Text = "Add Netwatch for Device IP";
                    break;
                default:
                    lbltitle.Text = "Add Netwatch Configuration";
                    break;
            }
        }

        private void btnSaveNetwatch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPingName.Text))
            {
                MessageBox.Show("Netwatch Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPingName.Focus();
                return;
            }
            if (!int.TryParse(txtInterval.Text, out int intervalSeconds) || intervalSeconds <= 0)
            {
                MessageBox.Show("Interval must be a positive number (seconds).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtInterval.Focus();
                return;
            }
            if (!int.TryParse(txtTimeout.Text, out int timeoutMilliseconds) || timeoutMilliseconds <= 0)
            {
                MessageBox.Show("Timeout must be a positive number (milliseconds).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtTimeout.Focus();
                return;
            }

            NetwatchConfig newConfig = new NetwatchConfig
            {
                NetwatchName = txtPingName.Text.Trim(),
                Type = comboBox1.SelectedItem.ToString(), // Should be "ICMP"
                IntervalSeconds = intervalSeconds,
                TimeoutMilliseconds = timeoutMilliseconds,
                SourceType = this.CurrentSourceType, // Set by the calling UC
                RunUponSave = chkRunNetwatchUponSave.Checked,
                IsEnabled = chkRunNetwatchUponSave.Checked,
                CreatedDate = DateTime.Now,
                MonitoredTagIds = this.InitialTagIdsToMonitor // Use the passed-in list
            };

            // Set TargetId based on the source type (ClusterId for NetworkCluster source)
            if (this.CurrentSourceType == NetwatchSourceType.NetworkCluster)
            {
                newConfig.TargetId = this.SourceNetworkClusterId;
            }
            // else if (this.CurrentSourceType == NetwatchSourceType.Customer) { /* newConfig.TargetId = this.SourceCustomerId; */ }
            // else if (this.CurrentSourceType == NetwatchSourceType.DeviceIP) { /* newConfig.TargetId = this.SourceDeviceIpId; */ }


            try
            {
                // We are about to update NetwatchConfigRepository
                NetwatchConfigRepository repo = new NetwatchConfigRepository();
                repo.AddNetwatchConfig(newConfig);

                MessageBox.Show("Netwatch configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NetwatchConfigSaved?.Invoke(this, EventArgs.Empty);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving Netwatch configuration: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // This was in your original code, removing as it's not used for this specific form's purpose.
        // If you need it elsewhere, it should be in the relevant form.
        // private void AddPingTags_Load(object sender, EventArgs e)
        // {
        // }

    }
}
