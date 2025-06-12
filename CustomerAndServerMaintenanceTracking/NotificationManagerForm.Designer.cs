namespace CustomerAndServerMaintenanceTracking
{
    partial class NotificationManagerForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationManagerForm));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvNotificationList = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.txtSearchRules = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.toolStripMenu = new System.Windows.Forms.ToolStrip();
            this.toolStripBtnDeleteCheckedNotifications = new System.Windows.Forms.ToolStripButton();
            this.ToolStripMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.stopAllProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startAllProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNotificationList)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.toolStripMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.dgvNotificationList, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1125, 542);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // dgvNotificationList
            // 
            this.dgvNotificationList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvNotificationList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvNotificationList.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvNotificationList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvNotificationList.Location = new System.Drawing.Point(3, 33);
            this.dgvNotificationList.Name = "dgvNotificationList";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvNotificationList.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvNotificationList.Size = new System.Drawing.Size(1119, 486);
            this.dgvNotificationList.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.txtSearchRules, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel1, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1125, 30);
            this.tableLayoutPanel2.TabIndex = 4;
            // 
            // txtSearchRules
            // 
            this.txtSearchRules.Location = new System.Drawing.Point(3, 3);
            this.txtSearchRules.Name = "txtSearchRules";
            this.txtSearchRules.Size = new System.Drawing.Size(214, 20);
            this.txtSearchRules.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.toolStripMenu);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(562, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(563, 30);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // toolStripMenu
            // 
            this.toolStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripBtnDeleteCheckedNotifications,
            this.ToolStripMenuItem});
            this.toolStripMenu.Location = new System.Drawing.Point(487, 0);
            this.toolStripMenu.Name = "toolStripMenu";
            this.toolStripMenu.Size = new System.Drawing.Size(76, 25);
            this.toolStripMenu.TabIndex = 10;
            this.toolStripMenu.Text = "toolStrip1";
            // 
            // toolStripBtnDeleteCheckedNotifications
            // 
            this.toolStripBtnDeleteCheckedNotifications.Image = ((System.Drawing.Image)(resources.GetObject("toolStripBtnDeleteCheckedNotifications.Image")));
            this.toolStripBtnDeleteCheckedNotifications.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripBtnDeleteCheckedNotifications.Margin = new System.Windows.Forms.Padding(0, 1, 16, 2);
            this.toolStripBtnDeleteCheckedNotifications.Name = "toolStripBtnDeleteCheckedNotifications";
            this.toolStripBtnDeleteCheckedNotifications.Size = new System.Drawing.Size(180, 22);
            this.toolStripBtnDeleteCheckedNotifications.Text = "Delete Checked Notifications";
            this.toolStripBtnDeleteCheckedNotifications.Visible = false;
            // 
            // ToolStripMenuItem
            // 
            this.ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stopAllProcessToolStripMenuItem,
            this.startAllProcessToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.ToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("ToolStripMenuItem.Image")));
            this.ToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripMenuItem.Name = "ToolStripMenuItem";
            this.ToolStripMenuItem.Size = new System.Drawing.Size(64, 22);
            this.ToolStripMenuItem.Text = "More";
            // 
            // stopAllProcessToolStripMenuItem
            // 
            this.stopAllProcessToolStripMenuItem.Name = "stopAllProcessToolStripMenuItem";
            this.stopAllProcessToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.stopAllProcessToolStripMenuItem.Text = "Stop All Process";
            // 
            // startAllProcessToolStripMenuItem
            // 
            this.startAllProcessToolStripMenuItem.Name = "startAllProcessToolStripMenuItem";
            this.startAllProcessToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.startAllProcessToolStripMenuItem.Text = "Start All Process";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            // 
            // NotificationManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1125, 542);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "NotificationManagerForm";
            this.Text = "NotificationManagerForm";
            this.Load += new System.EventHandler(this.NotificationManagerForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvNotificationList)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.toolStripMenu.ResumeLayout(false);
            this.toolStripMenu.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        public System.Windows.Forms.DataGridView dgvNotificationList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox txtSearchRules;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.ToolStrip toolStripMenu;
        private System.Windows.Forms.ToolStripDropDownButton ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopAllProcessToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startAllProcessToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
    }
}