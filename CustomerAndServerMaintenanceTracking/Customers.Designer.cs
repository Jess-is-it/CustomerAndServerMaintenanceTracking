namespace CustomerAndServerMaintenanceTracking
{
    partial class Customers
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
            this.dataGridViewActiveCustomers = new System.Windows.Forms.DataGridView();
            this.btnRefreshActive = new System.Windows.Forms.Button();
            this.timerSync = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.dataGridViewArchivedCustomers = new System.Windows.Forms.DataGridView();
            this.lblsyncArchived = new System.Windows.Forms.Label();
            this.btnRefreshArchived = new System.Windows.Forms.Button();
            this.lblsyncActive = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewActiveCustomers)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewArchivedCustomers)).BeginInit();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridViewActiveCustomers
            // 
            this.dataGridViewActiveCustomers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewActiveCustomers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewActiveCustomers.Location = new System.Drawing.Point(3, 33);
            this.dataGridViewActiveCustomers.Name = "dataGridViewActiveCustomers";
            this.dataGridViewActiveCustomers.Size = new System.Drawing.Size(980, 522);
            this.dataGridViewActiveCustomers.TabIndex = 0;
            // 
            // btnRefreshActive
            // 
            this.btnRefreshActive.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshActive.Name = "btnRefreshActive";
            this.btnRefreshActive.Size = new System.Drawing.Size(112, 23);
            this.btnRefreshActive.TabIndex = 1;
            this.btnRefreshActive.Text = "Sync and Refresh";
            this.btnRefreshActive.UseVisualStyleBackColor = true;
            this.btnRefreshActive.Click += new System.EventHandler(this.btnRefreshActive_Click);
            // 
            // timerSync
            // 
            this.timerSync.Enabled = true;
            this.timerSync.Interval = 300000;
            this.timerSync.Tick += new System.EventHandler(this.timerSync_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1000, 610);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tableLayoutPanel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(992, 584);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Active Customers";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.dataGridViewActiveCustomers, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblsyncActive, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(986, 578);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.tableLayoutPanel2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(992, 584);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Archived Customers";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.dataGridViewArchivedCustomers, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.lblsyncArchived, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.btnRefreshArchived, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(986, 578);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // dataGridViewArchivedCustomers
            // 
            this.dataGridViewArchivedCustomers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewArchivedCustomers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewArchivedCustomers.Location = new System.Drawing.Point(3, 33);
            this.dataGridViewArchivedCustomers.Name = "dataGridViewArchivedCustomers";
            this.dataGridViewArchivedCustomers.Size = new System.Drawing.Size(980, 522);
            this.dataGridViewArchivedCustomers.TabIndex = 0;
            // 
            // lblsyncArchived
            // 
            this.lblsyncArchived.AutoSize = true;
            this.lblsyncArchived.Location = new System.Drawing.Point(3, 558);
            this.lblsyncArchived.Name = "lblsyncArchived";
            this.lblsyncArchived.Size = new System.Drawing.Size(46, 13);
            this.lblsyncArchived.TabIndex = 2;
            this.lblsyncArchived.Text = "Synced:";
            // 
            // btnRefreshArchived
            // 
            this.btnRefreshArchived.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshArchived.Name = "btnRefreshArchived";
            this.btnRefreshArchived.Size = new System.Drawing.Size(112, 23);
            this.btnRefreshArchived.TabIndex = 1;
            this.btnRefreshArchived.Text = "Sync and Refresh";
            this.btnRefreshArchived.UseVisualStyleBackColor = true;
            this.btnRefreshArchived.Click += new System.EventHandler(this.btnRefreshArchived_Click);
            // 
            // lblsyncActive
            // 
            this.lblsyncActive.AutoSize = true;
            this.lblsyncActive.Location = new System.Drawing.Point(3, 558);
            this.lblsyncActive.Name = "lblsyncActive";
            this.lblsyncActive.Size = new System.Drawing.Size(46, 13);
            this.lblsyncActive.TabIndex = 3;
            this.lblsyncActive.Text = "Synced:";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.btnRefreshActive);
            this.flowLayoutPanel2.Controls.Add(this.textBox1);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(986, 30);
            this.flowLayoutPanel2.TabIndex = 3;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(121, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(246, 20);
            this.textBox1.TabIndex = 2;
            // 
            // Customers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 610);
            this.Controls.Add(this.tabControl1);
            this.Name = "Customers";
            this.Text = "Customers ";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewActiveCustomers)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewArchivedCustomers)).EndInit();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnRefreshActive;
        public System.Windows.Forms.DataGridView dataGridViewActiveCustomers;
        private System.Windows.Forms.Timer timerSync;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        public System.Windows.Forms.DataGridView dataGridViewArchivedCustomers;
        private System.Windows.Forms.Label lblsyncArchived;
        private System.Windows.Forms.Button btnRefreshArchived;
        private System.Windows.Forms.Label lblsyncActive;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.TextBox textBox1;
    }
}

