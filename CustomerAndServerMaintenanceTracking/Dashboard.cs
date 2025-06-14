using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.Services;
using CustomerAndServerMaintenanceTracking.UserControl;
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
using CustomerAndServerMaintenanceTracking.SidePanelForms;
using SharedLibrary.DataAccess;
using CustomerAndServerMaintenanceTracking.Profiles;


namespace CustomerAndServerMaintenanceTracking
{
    public partial class Dashboard : Form
    {
        private Control _currentDetailControl = null;
        private Dictionary<int, NetwatchDetailedStatus> _openNetwatchDetailForms = new Dictionary<int, NetwatchDetailedStatus>();

        private ServiceLogRepository _serviceLogRepository;
        private TagRepository _tagRepository;
        // If MikrotikClientManager is used by Dashboard directly:
        // private MikrotikClientManager _clientManager; 

        public Dashboard()
        {
            InitializeComponent();
            notifyIcon1.ContextMenuStrip = contextMenuStripSystemTray;
            this.KeyPreview = true;

            _serviceLogRepository = new ServiceLogRepository();
            _tagRepository = new TagRepository();
            // _clientManager = MikrotikClientManager.Instance; // If used

            try
            {
                MikrotikClientManager.Instance.Connect();
                lblMikrotikStatus.Text = "Mikrotik Connected at " + DateTime.Now.ToString("hh:mm:ss tt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to Mikrotik: " + ex.Message, "Mikrotik Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblMikrotikStatus.Text = "Mikrotik Connection Failed";
            }
        }

        private void Dashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                notifyIcon1.Visible = true;
                return;
            }

            autoRefreshTimer.Stop();
            MikrotikClientManager.Instance.Disconnect();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Maximized;
            this.Activate();
            notifyIcon1.Visible = false;
        }

        private void assignTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is TagForm)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if TagForm's constructor needs them
            TagForm tagForm = new TagForm(_serviceLogRepository, _tagRepository);
            tagForm.MdiParent = this;
            tagForm.Show();
        }

        private void Dashboard_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (this.ActiveMdiChild != null)
                {
                    this.ActiveMdiChild.Close();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void autoRefreshTimer_Tick(object sender, EventArgs e)
        {
            foreach (Form mdiChild in this.MdiChildren)
            {
                if (mdiChild != null && !mdiChild.IsDisposed && mdiChild.Visible)
                {
                    if (mdiChild is IRefreshableForm refreshableForm)
                    {
                        try
                        {
                            refreshableForm.RefreshDataViews();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during auto-refresh of form {mdiChild.Name}: {ex.Message}");
                            // Consider logging this to _serviceLogRepository for persistence
                        }
                    }
                }
            }
        }

        #region Navigation Links
        private void CustmerList_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is Customers)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Customers form's constructor needs them
            Customers customersForm = new Customers();
            customersForm.MdiParent = this;
            customersForm.Show();
        }

        private void NetworkClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is NetworkClusterTag)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if NetworkClusterTag form's constructor needs them
            NetworkClusterTag networkClusterTag = new NetworkClusterTag();
            networkClusterTag.MdiParent = this;
            networkClusterTag.Show();
        }

        private void DeviceIP_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is DeviceIPForm)
                {
                    child.Activate();
                    return;
                }
            }
            DeviceIPForm deviceIP = new DeviceIPForm();
            deviceIP.MdiParent = this;
            deviceIP.Show();
        }

        private void pingAddNetwatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is NetwatchAdd)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if NetwatchAdd form's constructor needs them
            NetwatchAdd netwatchAddForm = new NetwatchAdd();
            netwatchAddForm.MdiParent = this;
            netwatchAddForm.Show();
        }

        private void pingNetwatchListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is NetwatchList)
                {
                    child.Activate();
                    return;
                }
            }
            NetwatchList netwatchlist = new NetwatchList(_serviceLogRepository, _tagRepository);
            netwatchlist.MdiParent = this;
            netwatchlist.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.ExitThread();
            Application.Exit();
        }

        private void LocationstoolStripMenu_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is Locations)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Settings form's constructor needs them
            Locations loc = new Locations();
            loc.MdiParent = this;
            loc.Show();
        }

        private void toolStripMenuItemNotifRules_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is NotificationManagerForm)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Settings form's constructor needs them
            NotificationManagerForm nmf = new NotificationManagerForm();
            nmf.MdiParent = this;
            nmf.Show();
        }

        private void mikrotikRouterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is SettingsMikrotikRouter)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Settings form's constructor needs them
            SettingsMikrotikRouter smr = new SettingsMikrotikRouter();
            smr.MdiParent = this;
            smr.Show();
        }
        private void usersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is SettingsUsers)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Settings form's constructor needs them
            SettingsUsers su = new SettingsUsers();
            su.MdiParent = this;
            su.Show();
        }
        private void serviceManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is SettingsServiceManagement)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Settings form's constructor needs them
            SettingsServiceManagement ssm = new SettingsServiceManagement();
            ssm.MdiParent = this;
            ssm.Show();
        }
        private void systemAccountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is SettingsSystemAccounts)
                {
                    child.Activate();
                    return;
                }
            }
            // Pass repositories if Settings form's constructor needs them
            SettingsSystemAccounts ssa = new SettingsSystemAccounts();
            ssa.MdiParent = this;
            ssa.Show();
        }

        #endregion

        #region Right Side Panel

        public void ShowNetwatchDetailInPanel(int netwatchConfigId, string netwatchConfigName, string currentStatus)
        {
            if (currentStatus != null && currentStatus.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_openNetwatchDetailForms.TryGetValue(netwatchConfigId, out NetwatchDetailedStatus existingForm))
            {
                if (existingForm != null && !existingForm.IsDisposed)
                {
                    existingForm.WindowState = FormWindowState.Normal;
                    existingForm.Activate();
                    return;
                }
                else
                {
                    _openNetwatchDetailForms.Remove(netwatchConfigId);
                }
            }

            // Pass the repositories from Dashboard to NetwatchDetailedStatus
            NetwatchDetailedStatus netwatchDetailForm = new NetwatchDetailedStatus(_serviceLogRepository, _tagRepository);

            netwatchDetailForm.MdiParent = this;
            netwatchDetailForm.InitializeAndLoadDetails(netwatchConfigId, netwatchConfigName);
            netwatchDetailForm.Tag = netwatchConfigId;
            netwatchDetailForm.FormClosed += NetwatchDetailForm_FormClosed;
            _openNetwatchDetailForms.Add(netwatchConfigId, netwatchDetailForm);
            netwatchDetailForm.Show();
        }

        private void NetwatchDetailForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender is NetwatchDetailedStatus closedForm && closedForm.Tag is int netwatchConfigId)
            {
                _openNetwatchDetailForms.Remove(netwatchConfigId);
                closedForm.FormClosed -= NetwatchDetailForm_FormClosed;
            }
        }






        #endregion


    }
}
