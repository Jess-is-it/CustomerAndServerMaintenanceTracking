namespace CustomerAndServerMaintenanceTracking
{
    partial class Dashboard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dashboard));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.CustomerList = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemNotifRules = new System.Windows.Forms.ToolStripMenuItem();
            this.Ping = new System.Windows.Forms.ToolStripMenuItem();
            this.pingAddNetwatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pingNetwatchListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Tags = new System.Windows.Forms.ToolStripMenuItem();
            this.assignCustomerTagToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NetworkClusterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.IP = new System.Windows.Forms.ToolStripMenuItem();
            this.DeviceIPtoolStripMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.mikrotikRouterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serviceManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.systemAccountsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Profiles = new System.Windows.Forms.ToolStripMenuItem();
            this.LocationstoolStripMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStripSystemTray = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSyncStatusPPPoe = new System.Windows.Forms.Label();
            this.lblMikrotikStatus = new System.Windows.Forms.Label();
            this.autoRefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            this.contextMenuStripSystemTray.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.AutoSize = false;
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(0);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem1,
            this.CustomerList,
            this.toolStripMenuItem4,
            this.toolStripMenuItemNotifRules,
            this.Ping,
            this.Tags,
            this.IP,
            this.Settings,
            this.Profiles});
            this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.menuStrip1.Location = new System.Drawing.Point(0, 96);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(24, 16, 0, 0);
            this.menuStrip1.Size = new System.Drawing.Size(200, 423);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripMenuItem2.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Padding = new System.Windows.Forms.Padding(0);
            this.toolStripMenuItem2.Size = new System.Drawing.Size(175, 25);
            this.toolStripMenuItem2.Text = "Inbox";
            this.toolStripMenuItem2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripMenuItem1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Padding = new System.Windows.Forms.Padding(0);
            this.toolStripMenuItem1.Size = new System.Drawing.Size(175, 25);
            this.toolStripMenuItem1.Text = "Drafts";
            this.toolStripMenuItem1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CustomerList
            // 
            this.CustomerList.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CustomerList.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.CustomerList.Margin = new System.Windows.Forms.Padding(0, 40, 0, 0);
            this.CustomerList.Name = "CustomerList";
            this.CustomerList.Padding = new System.Windows.Forms.Padding(0);
            this.CustomerList.Size = new System.Drawing.Size(175, 25);
            this.CustomerList.Text = "Customer List";
            this.CustomerList.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.CustomerList.Click += new System.EventHandler(this.CustmerList_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripMenuItem4.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Padding = new System.Windows.Forms.Padding(0);
            this.toolStripMenuItem4.Size = new System.Drawing.Size(175, 25);
            this.toolStripMenuItem4.Text = "New Installations";
            this.toolStripMenuItem4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripMenuItemNotifRules
            // 
            this.toolStripMenuItemNotifRules.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripMenuItemNotifRules.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItemNotifRules.Name = "toolStripMenuItemNotifRules";
            this.toolStripMenuItemNotifRules.Padding = new System.Windows.Forms.Padding(0);
            this.toolStripMenuItemNotifRules.Size = new System.Drawing.Size(175, 25);
            this.toolStripMenuItemNotifRules.Text = "Notification Rules";
            this.toolStripMenuItemNotifRules.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolStripMenuItemNotifRules.Click += new System.EventHandler(this.toolStripMenuItemNotifRules_Click);
            // 
            // Ping
            // 
            this.Ping.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pingAddNetwatchToolStripMenuItem,
            this.pingNetwatchListToolStripMenuItem});
            this.Ping.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Ping.Name = "Ping";
            this.Ping.Size = new System.Drawing.Size(175, 25);
            this.Ping.Text = "Tools";
            this.Ping.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pingAddNetwatchToolStripMenuItem
            // 
            this.pingAddNetwatchToolStripMenuItem.Name = "pingAddNetwatchToolStripMenuItem";
            this.pingAddNetwatchToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.pingAddNetwatchToolStripMenuItem.Text = "Add Netwatch";
            this.pingAddNetwatchToolStripMenuItem.Click += new System.EventHandler(this.pingAddNetwatchToolStripMenuItem_Click);
            // 
            // pingNetwatchListToolStripMenuItem
            // 
            this.pingNetwatchListToolStripMenuItem.Name = "pingNetwatchListToolStripMenuItem";
            this.pingNetwatchListToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.pingNetwatchListToolStripMenuItem.Text = "Netwatch List";
            this.pingNetwatchListToolStripMenuItem.Click += new System.EventHandler(this.pingNetwatchListToolStripMenuItem_Click);
            // 
            // Tags
            // 
            this.Tags.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.assignCustomerTagToolStripMenuItem,
            this.NetworkClusterToolStripMenuItem});
            this.Tags.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Tags.Name = "Tags";
            this.Tags.Size = new System.Drawing.Size(175, 25);
            this.Tags.Text = "Tags";
            this.Tags.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // assignCustomerTagToolStripMenuItem
            // 
            this.assignCustomerTagToolStripMenuItem.Name = "assignCustomerTagToolStripMenuItem";
            this.assignCustomerTagToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.assignCustomerTagToolStripMenuItem.Text = "Tags";
            this.assignCustomerTagToolStripMenuItem.Click += new System.EventHandler(this.assignTagToolStripMenuItem_Click);
            // 
            // NetworkClusterToolStripMenuItem
            // 
            this.NetworkClusterToolStripMenuItem.Name = "NetworkClusterToolStripMenuItem";
            this.NetworkClusterToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.NetworkClusterToolStripMenuItem.Text = "Network Cluster";
            this.NetworkClusterToolStripMenuItem.Click += new System.EventHandler(this.NetworkClusterToolStripMenuItem_Click);
            // 
            // IP
            // 
            this.IP.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DeviceIPtoolStripMenu});
            this.IP.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IP.Name = "IP";
            this.IP.Size = new System.Drawing.Size(175, 25);
            this.IP.Text = "IP";
            this.IP.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DeviceIPtoolStripMenu
            // 
            this.DeviceIPtoolStripMenu.Name = "DeviceIPtoolStripMenu";
            this.DeviceIPtoolStripMenu.Size = new System.Drawing.Size(143, 26);
            this.DeviceIPtoolStripMenu.Text = "Device IP";
            this.DeviceIPtoolStripMenu.Click += new System.EventHandler(this.DeviceIP_Click);
            // 
            // Settings
            // 
            this.Settings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mikrotikRouterToolStripMenuItem,
            this.usersToolStripMenuItem,
            this.serviceManagerToolStripMenuItem,
            this.systemAccountsToolStripMenuItem});
            this.Settings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Settings.Name = "Settings";
            this.Settings.Size = new System.Drawing.Size(175, 25);
            this.Settings.Text = "Settings";
            this.Settings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mikrotikRouterToolStripMenuItem
            // 
            this.mikrotikRouterToolStripMenuItem.Name = "mikrotikRouterToolStripMenuItem";
            this.mikrotikRouterToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.mikrotikRouterToolStripMenuItem.Text = "Mikrotik Router";
            this.mikrotikRouterToolStripMenuItem.Click += new System.EventHandler(this.mikrotikRouterToolStripMenuItem_Click);
            // 
            // usersToolStripMenuItem
            // 
            this.usersToolStripMenuItem.Name = "usersToolStripMenuItem";
            this.usersToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.usersToolStripMenuItem.Text = "Users";
            this.usersToolStripMenuItem.Click += new System.EventHandler(this.usersToolStripMenuItem_Click);
            // 
            // serviceManagerToolStripMenuItem
            // 
            this.serviceManagerToolStripMenuItem.Name = "serviceManagerToolStripMenuItem";
            this.serviceManagerToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.serviceManagerToolStripMenuItem.Text = "Service Manager";
            this.serviceManagerToolStripMenuItem.Click += new System.EventHandler(this.serviceManagerToolStripMenuItem_Click);
            // 
            // systemAccountsToolStripMenuItem
            // 
            this.systemAccountsToolStripMenuItem.Name = "systemAccountsToolStripMenuItem";
            this.systemAccountsToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.systemAccountsToolStripMenuItem.Text = "System Accounts";
            this.systemAccountsToolStripMenuItem.Click += new System.EventHandler(this.systemAccountsToolStripMenuItem_Click);
            // 
            // Profiles
            // 
            this.Profiles.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LocationstoolStripMenu});
            this.Profiles.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Profiles.Margin = new System.Windows.Forms.Padding(0, 48, 0, 0);
            this.Profiles.Name = "Profiles";
            this.Profiles.Size = new System.Drawing.Size(175, 25);
            this.Profiles.Text = "Profiles";
            this.Profiles.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LocationstoolStripMenu
            // 
            this.LocationstoolStripMenu.Name = "LocationstoolStripMenu";
            this.LocationstoolStripMenu.Size = new System.Drawing.Size(146, 26);
            this.LocationstoolStripMenu.Text = "Locations";
            this.LocationstoolStripMenu.Click += new System.EventHandler(this.LocationstoolStripMenu_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "CSMT";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            // 
            // contextMenuStripSystemTray
            // 
            this.contextMenuStripSystemTray.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.contextMenuStripSystemTray.Name = "contextMenuStripSystemTray";
            this.contextMenuStripSystemTray.Size = new System.Drawing.Size(94, 26);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.menuStrip1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 16, 0, 0);
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(200, 652);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.pictureBox1);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 19);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(24, 0, 0, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(194, 74);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(24, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(58, 80);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.lblSyncStatusPPPoe);
            this.flowLayoutPanel2.Controls.Add(this.lblMikrotikStatus);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 552);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(200, 100);
            this.flowLayoutPanel2.TabIndex = 4;
            // 
            // lblSyncStatusPPPoe
            // 
            this.lblSyncStatusPPPoe.AutoSize = true;
            this.lblSyncStatusPPPoe.Location = new System.Drawing.Point(3, 0);
            this.lblSyncStatusPPPoe.Name = "lblSyncStatusPPPoe";
            this.lblSyncStatusPPPoe.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblSyncStatusPPPoe.Size = new System.Drawing.Size(54, 21);
            this.lblSyncStatusPPPoe.TabIndex = 2;
            this.lblSyncStatusPPPoe.Text = "Loading...";
            // 
            // lblMikrotikStatus
            // 
            this.lblMikrotikStatus.AutoSize = true;
            this.lblMikrotikStatus.Location = new System.Drawing.Point(3, 21);
            this.lblMikrotikStatus.Name = "lblMikrotikStatus";
            this.lblMikrotikStatus.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblMikrotikStatus.Size = new System.Drawing.Size(54, 21);
            this.lblMikrotikStatus.TabIndex = 4;
            this.lblMikrotikStatus.Text = "Loading...";
            // 
            // autoRefreshTimer
            // 
            this.autoRefreshTimer.Enabled = true;
            this.autoRefreshTimer.Interval = 1000;
            this.autoRefreshTimer.Tick += new System.EventHandler(this.autoRefreshTimer_Tick);
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1148, 652);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Dashboard";
            this.Text = "Dashboard";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Dashboard_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Dashboard_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStripSystemTray.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem CustomerList;
        private System.Windows.Forms.ToolStripMenuItem Tags;
        private System.Windows.Forms.ToolStripMenuItem Settings;
        private System.Windows.Forms.ToolStripMenuItem Ping;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripSystemTray;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblSyncStatusPPPoe;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label lblMikrotikStatus;
        private System.Windows.Forms.ToolStripMenuItem assignCustomerTagToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NetworkClusterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pingAddNetwatchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pingNetwatchListToolStripMenuItem;
        private System.Windows.Forms.Timer autoRefreshTimer;
        private System.Windows.Forms.ToolStripMenuItem Profiles;
        private System.Windows.Forms.ToolStripMenuItem LocationstoolStripMenu;
        private System.Windows.Forms.ToolStripMenuItem IP;
        private System.Windows.Forms.ToolStripMenuItem DeviceIPtoolStripMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemNotifRules;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem mikrotikRouterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem usersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem serviceManagerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem systemAccountsToolStripMenuItem;
    }
}