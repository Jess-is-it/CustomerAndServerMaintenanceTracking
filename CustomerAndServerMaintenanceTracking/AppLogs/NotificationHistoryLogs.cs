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
using System.IO;

namespace CustomerAndServerMaintenanceTracking.AppLogs
{
    public partial class NotificationHistoryLogs : Form
    {
        private readonly int _ruleId;
        private readonly string _ruleName;
        private readonly NotificationHistoryRepository _historyRepository;

        public NotificationHistoryLogs(int ruleId, string ruleName)
        {
            InitializeComponent();

            _ruleId = ruleId;
            _ruleName = ruleName;
            _historyRepository = new NotificationHistoryRepository();

            // Set the title of the form
            this.Text = $"History for: {_ruleName}";
            // Assuming your title Label is named lblTitle
            if (this.lblTitle != null)
            {
                this.lblTitle.Text = $"Notification History for Rule: {_ruleName}";
            }

            // Wire up the events
            this.Load += NotificationHistoryLogs_Load;
            // Assuming your download button is named btnDownload
            if (this.btnDownload != null)
            {
                this.btnDownload.Click += btnDownload_Click;
            }
        }

        private void NotificationHistoryLogs_Load(object sender, EventArgs e)
        {
            LoadHistoryLogs();
        }

        private void LoadHistoryLogs()
        {
            try
            {
                // Fetch the filtered logs using our optimized repository method
                List<NotificationHistoryLog> logs = _historyRepository.GetLogsForRule(_ruleId);

                // Assuming your ListBox is named listboxHistory
                listboxHistory.Items.Clear();
                if (logs != null && logs.Any())
                {
                    foreach (var log in logs)
                    {
                        string formattedMessage = $"{log.LogTimestamp:yyyy-MM-dd HH:mm:ss} [{log.LogLevel}] - {log.Message}";
                        listboxHistory.Items.Add(formattedMessage);
                    }
                }
                else
                {
                    listboxHistory.Items.Add("No history found for this rule.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- NEW METHOD for your Download Button ---
        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (listboxHistory.Items.Count == 0 ||
               (listboxHistory.Items.Count == 1 && listboxHistory.Items[0].ToString().StartsWith("No history")))
            {
                MessageBox.Show("There are no logs to download.", "No Logs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text File (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Notification History";
                saveFileDialog.FileName = $"NotificationHistory_Rule_{_ruleId}_{_ruleName.Replace(" ", "_")}.txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Get all items from the ListBox as a list of strings
                        List<string> logLines = listboxHistory.Items.OfType<string>().ToList();

                        // Write all lines to the selected file
                        File.WriteAllLines(saveFileDialog.FileName, logLines);

                        MessageBox.Show("Logs downloaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save log file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
