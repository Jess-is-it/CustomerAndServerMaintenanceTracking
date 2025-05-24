using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.DataAccess; // For ServiceLogRepository
using SharedLibrary.Models;    // For ServiceLogEntry, LogLevel
using System.ServiceProcess;   // For ServiceController
using System.Globalization;    // For DateTime parsing if needed

namespace CustomerAndServerMaintenanceTracking.AppLogs
{
    public partial class ServiceLogs : Form
    {
        private readonly string _serviceNameForFilter;
        private readonly string _serviceDisplayName;
        private readonly ServiceLogRepository _logRepository;
        private System.Windows.Forms.Timer _refreshTimer;

        // These constants help in launching the form with the correct filter.
        // Ensure they match the 'ServiceName' your services use when logging.
        public const string NetwatchPingerLogName = "NetwatchPingerService";
        public const string PPPoESyncLogName = "PPPoESyncService";

        public ServiceLogs(string serviceNameToFilter, string serviceDisplayName)
        {
            InitializeComponent();

            _serviceNameForFilter = serviceNameToFilter;
            _serviceDisplayName = serviceDisplayName;
            _logRepository = new ServiceLogRepository();

            this.Text = $"{_serviceDisplayName} - Logs";
            lblTitle.Text = _serviceDisplayName;

            // Hide or disable the Start/Stop button if it exists on the designer
            if (this.btnStartStop != null)
            {
                this.btnStartStop.Visible = false; // Or .Enabled = false;
            }
            // Repurpose or hide lblUptime
            if (this.lblUptime != null)
            {
                this.lblUptime.Text = $"Displaying last 7 days of logs.";
            }

            this.Load += ServiceLogs_Load;
            this.FormClosing += ServiceLogs_FormClosing;
        }

        private void ServiceLogs_Load(object sender, EventArgs e)
        {
            LoadLogs();

            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 30000; // Refresh logs every 30 seconds (adjust as needed)
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed || this.Disposing) return;
            LoadLogs();
        }

        private void LoadLogs()
        {
            if (this.IsDisposed || this.Disposing) return;

            try
            {
                DateTime startDate = DateTime.Now.AddDays(-7).Date;
                // Pass _serviceNameForFilter to get logs for the specific service
                List<ServiceLogEntry> logs = _logRepository.GetLogs(_serviceNameForFilter, startDate, DateTime.Now.AddDays(1).Date.AddTicks(-1));

                int selectedIndex = listboxLogs.SelectedIndex;
                object selectedItem = listboxLogs.SelectedItem; // Try to preserve by item content

                listboxLogs.BeginUpdate();
                listboxLogs.Items.Clear();
                foreach (var log in logs)
                {
                    string formattedMessage = $"{log.LogTimestamp:yyyy-MM-dd HH:mm:ss} [{log.LogLevel}] {log.Message}";
                    if (!string.IsNullOrWhiteSpace(log.ExceptionDetails))
                    {
                        // You could add a tooltip or a way to view full exception details on selection
                        formattedMessage += " (Exception - see details if available)";
                    }
                    listboxLogs.Items.Add(formattedMessage);
                }
                listboxLogs.EndUpdate();

                // Attempt to re-select
                if (selectedItem != null && listboxLogs.Items.Contains(selectedItem))
                {
                    listboxLogs.SelectedItem = selectedItem;
                }
                else if (selectedIndex >= 0 && selectedIndex < listboxLogs.Items.Count)
                {
                    listboxLogs.SelectedIndex = selectedIndex;
                }

            }
            catch (Exception ex)
            {
                if (!listboxLogs.IsDisposed)
                {
                    // Add error to listbox only once to avoid flooding
                    if (!listboxLogs.Items.OfType<string>().Any(s => s.StartsWith("Error loading logs:")))
                    {
                        listboxLogs.Items.Add($"Error loading logs: {ex.Message}");
                    }
                }
                Console.WriteLine($"Error in ServiceLogs.LoadLogs: {ex.ToString()}"); // Log to debug output
            }
        }

        // BtnStartStop_Click is no longer needed if we remove service control from this form.
        // If you keep the button for other purposes, its event handler would be different.

        private void ServiceLogs_FormClosing(object sender, FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        }
    }
}
