using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class PingTerminalForm: Form
    {
        private string customerName;
        private string targetIP;
        private int intervalMs;

        public PingTerminalForm(string customerName, string ipAddress, int pingIntervalMs)
        {
            InitializeComponent();
            this.customerName = customerName;
            targetIP = ipAddress;
            intervalMs = pingIntervalMs;
        }

        private void PingTerminalForm_Load(object sender, EventArgs e)
        {
            lblInfo.Text = $"Pinging {customerName} ({targetIP})";
            pingTimer.Interval = intervalMs;
            pingTimer.Tick += pingTimer_Tick;
            pingTimer.Start();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            AppendOutput("Ping started automatically.");
        }
        private void pingTimer_Tick(object sender, EventArgs e)
        {
            DoPing();
        }
        private void DoPing()
        {
            try
            {
                using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send(targetIP, 1000); // 1 second timeout
                    if (reply.Status == IPStatus.Success)
                    {
                        AppendOutput($"Reply from {reply.Address}: time={reply.RoundtripTime}ms");
                    }
                    else
                    {
                        AppendOutput($"Ping failed: {reply.Status}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}");
            }
        }

        private void AppendOutput(string text)
        {
            // Safely update the RichTextBox on the UI thread if needed
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendOutput), text);
                return;
            }

            // Append line + newline
            rtbOutput.AppendText(text + Environment.NewLine);

            // Optional: auto-scroll to bottom
            rtbOutput.ScrollToCaret();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            pingTimer.Stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            AppendOutput("Ping stopped.");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            pingTimer.Start();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            AppendOutput("Ping started.");
        }
    }
}
