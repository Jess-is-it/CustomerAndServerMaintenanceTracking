using CustomerAndServerMaintenanceTracking.AppLogs;
using System;
using System.Configuration;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class SettingsServiceManagement : Form
    {
        private System.Windows.Forms.Timer _serviceStatusRefreshTimer;

        // These MUST match the actual registered Windows Service names
        private const string ActualNetwatchPingerServiceName = "NetwatchPingerService";
        private const string ActualPPPoESyncServiceName = "PPPoESyncService";

        private readonly string _targetMachineName;

        public SettingsServiceManagement()
        {
            InitializeComponent();
            _targetMachineName = ConfigurationManager.AppSettings["ServiceControlTargetMachine"] ?? ".";

            #region Event Handlers
            this.Load += new System.EventHandler(this.SettingsServiceManagement_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsServiceManagement_FormClosing);

            // Service Management Tab
            this.btnViewNetwatchService.Click += BtnViewNetwatchLogs_Click;
            this.btnViewPPPoESyncService.Click += BtnViewPPPoESyncLogs_Click;

            // Placeholder for Notification Service - handlers are present to prevent errors
            this.btnViewNotificationService.Click += BtnViewNotificationService_Click;
            #endregion
        }


        #region Form Load/Close
        private void SettingsServiceManagement_Load(object sender, EventArgs e)
        {
            UpdateAllServiceStatusesAndButtons();

            _serviceStatusRefreshTimer = new System.Windows.Forms.Timer();
            _serviceStatusRefreshTimer.Interval = 10000; // Refresh every 10 seconds
            _serviceStatusRefreshTimer.Tick += ServiceStatusRefreshTimer_Tick;
            _serviceStatusRefreshTimer.Start();

            // Hide the Start/Stop buttons as they were in the original form
            if (btnStartStopNetwatchService != null) btnStartStopNetwatchService.Visible = false;
            if (btnStartStopPPPoESyncService != null) btnStartStopPPPoESyncService.Visible = false;
            if (btnStartStopNotificationService != null) btnStartStopNotificationService.Visible = false;
        }

        private void SettingsServiceManagement_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serviceStatusRefreshTimer?.Stop();
            _serviceStatusRefreshTimer?.Dispose();
        }
        #endregion

        #region Service Management
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
            // Add call for Notification service if it becomes a real service
            UpdateSimplifiedServiceStatusUI("NotificationService", label1); // Placeholder update
        }

        private void UpdateSimplifiedServiceStatusUI(string serviceName, Label statusLabel)
        {
            if (statusLabel == null || statusLabel.IsDisposed || this.IsDisposed || this.Disposing) return;

            string statusText = "Unreachable";
            Color statusColor = Color.OrangeRed;

            try
            {
                using (ServiceController sc = new ServiceController(serviceName, _targetMachineName))
                {
                    sc.Refresh();
                    statusText = sc.Status.ToString();
                    statusColor = sc.Status == ServiceControllerStatus.Running ? Color.Green : Color.OrangeRed;
                }
            }
            catch (InvalidOperationException) // Service not found
            {
                statusText = "Not Found";
                statusColor = Color.Gray;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting status for {serviceName} on '{_targetMachineName}': {ex.Message}");
                statusText = "Error";
                statusColor = Color.DarkRed;
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

        // Placeholder for future implementation
        private void BtnViewNotificationService_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Log viewer for this service is not yet implemented.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
