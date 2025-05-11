using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class Dashboard: Form
    {
        private System.Timers.Timer timerPPPoe;
        private SyncManager syncManager;


        public Dashboard()
        {
            InitializeComponent();
            notifyIcon1.ContextMenuStrip = contextMenuStripSystemTray; //Right click system tray to exit
            this.KeyPreview = true;
            try
            {
                MikrotikClientManager.Instance.Connect();
                // Optionally, update a status label:
                lblMikrotikStatus.Text = "Connected to Mikrotik at " + DateTime.Now.ToString("hh:mm:ss tt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to Mikrotik: " + ex.Message);
            }


        }


        private void CustmerList_Click(object sender, EventArgs e)
        {
            // Check if the form is already open; if so, focus it.
            foreach (Form child in this.MdiChildren)
            {
                if (child is Customers)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the Customers form and set its MdiParent.
            Customers customersForm = new Customers();
            customersForm.MdiParent = this;
            customersForm.Show();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            // Check if the form is already open; if so, focus it.
            foreach (Form child in this.MdiChildren)
            {
                if (child is Settings)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the TagForm and set its MdiParent.
            Settings settings = new Settings();
            settings.MdiParent = this;
            settings.Show();
        }

        private void Dashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the close is initiated by the user (e.g., clicking the X button),
            // cancel the closing and minimize/hide the form so the app continues running.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Cancel the close event.
                this.WindowState = FormWindowState.Minimized;
                this.Hide();     // Hide the form from the taskbar.
                notifyIcon1.Visible = true;
                return;        // Exit the event handler; do NOT stop the timers.
            }

            // If we're truly exiting (not just minimizing), then stop and dispose the timers.
            if (timerPPPoe != null)
            {
                timerPPPoe.Stop();
                timerPPPoe.Dispose();
            }


            // Optionally disconnect from Mikrotik if exiting.
            MikrotikClientManager.Instance.Disconnect();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            // Restore the form
            this.Show();
            this.WindowState = FormWindowState.Maximized;
            this.Activate();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit(); // fully close the app
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            // Read intervals from the database
            SyncSettingsRepository repo = new SyncSettingsRepository();
            int intervalPPPoe = repo.GetInterval("PPPoeInterval");
            if (intervalPPPoe <= 0)
                intervalPPPoe = 60000; // default value if not set


            // Create and start timer for PPPoe accounts sync
            timerPPPoe = new System.Timers.Timer(intervalPPPoe);
            timerPPPoe.AutoReset = true;
            timerPPPoe.Elapsed += TimerPPPoe_Elapsed;
            timerPPPoe.Start();


            // Hide status labels initially until connection is established
            lblSyncStatusPPPoe.Visible = false;

            // Initialize the SyncManager
            syncManager = new SyncManager();

            try
            {
                // Establish the Mikrotik connection on startup.
                MikrotikClientManager.Instance.Connect();

                // Once connected, update and show the status labels.
                lblSyncStatusPPPoe.Text = "Connected to Mikrotik at " + DateTime.Now.ToString("hh:mm:ss tt");
                lblSyncStatusPPPoe.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to Mikrotik: " + ex.Message);
            }
        }

        private void TimerPPPoe_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // Directly call the sync method from SyncManager:
                syncManager.SyncCustomers();

                // Update the UI on the main thread:
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblSyncStatusPPPoe.Text = "PPPoe Synced at " + DateTime.Now.ToString("hh:mm:ss tt");
                    lblSyncStatusPPPoe.Visible = true;
                });
            }
            catch (Exception ex)
            {
                // Handle exceptions (log or display message)
            }
        }

        public void UpdatePPPoeTimerInterval()
        {
            // Create an instance of your repository to read the value
            SyncSettingsRepository repo = new SyncSettingsRepository();
            int newInterval = repo.GetInterval("PPPoeInterval");

            // Fallback to a default value if the interval is not set or invalid
            if (newInterval <= 0)
                newInterval = 60000; // default 60 seconds

            // Update the timer's interval property for PPPoe accounts sync
            if (timerPPPoe != null)
            {
                timerPPPoe.Interval = newInterval;
            }
        }

        private void Ping_Click(object sender, EventArgs e)
        {

        }

        private void assignTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if the form is already open; if so, focus it.
            foreach (Form child in this.MdiChildren)
            {
                if (child is TagForm)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the TagForm and set its MdiParent.
            TagForm tagForm = new TagForm();
            tagForm.MdiParent = this;
            tagForm.Show();
        }

        private void NetworkClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if the form is already open; if so, focus it.
            foreach (Form child in this.MdiChildren)
            {
                if (child is NetworkClusterTag)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the TagForm and set its MdiParent.
            NetworkClusterTag networkClusterTag = new NetworkClusterTag();
            networkClusterTag.MdiParent = this;
            networkClusterTag.Show();
        }

        private void DeviceIP_Click(object sender, EventArgs e)
        {
            // Check if the form is already open; if so, focus it.
            foreach (Form child in this.MdiChildren)
            {
                if (child is DeviceIPForm)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the TagForm and set its MdiParent.
            DeviceIPForm deviceIP = new DeviceIPForm();
            deviceIP.MdiParent = this;
            deviceIP.Show();
        }

        private void Dashboard_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the pressed key is Escape
            if (e.KeyCode == Keys.Escape)
            {
                // Check if there is an active MDI child window
                if (this.ActiveMdiChild != null)
                {
                    // Close the currently active MDI child form
                    this.ActiveMdiChild.Close();

                    // Optional: Mark the event as handled if you don't want
                    // any potential parent controls to process Escape further.
                    e.Handled = true;
                    e.SuppressKeyPress = true; // Prevents the 'ding' sound on Escape
                }
                // Optional: You could add an 'else' here if you want Escape
                // to do something else when no child form is active (like maybe minimize the Dashboard).
                // else
                // {
                //     // Example: Minimize dashboard if no child is active
                //     // this.WindowState = FormWindowState.Minimized;
                // }
            }
        }

        private void pingIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is PingIP)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the TagForm and set its MdiParent.
            PingIP ping = new PingIP();
            ping.MdiParent = this;
            ping.Show();
        }

        private void pingTaskListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is PingIPTaskList)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the TagForm and set its MdiParent.
            PingIPTaskList pingtask = new PingIPTaskList();
            pingtask.MdiParent = this;
            pingtask.Show();
        }
    }
}
