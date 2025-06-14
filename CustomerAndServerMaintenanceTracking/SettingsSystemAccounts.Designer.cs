namespace CustomerAndServerMaintenanceTracking
{
    partial class SettingsSystemAccounts
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
            this.tabControl4 = new System.Windows.Forms.TabControl();
            this.tabPageEmail = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel12 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvEmail = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel13 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel16 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAddEmail = new System.Windows.Forms.Button();
            this.flowLayoutPanel17 = new System.Windows.Forms.FlowLayoutPanel();
            this.txtSearchEmail = new System.Windows.Forms.TextBox();
            this.tabPageSMS = new System.Windows.Forms.TabPage();
            this.tabPageTelegram = new System.Windows.Forms.TabPage();
            this.tabPageFBMessenger = new System.Windows.Forms.TabPage();
            this.tabControl4.SuspendLayout();
            this.tabPageEmail.SuspendLayout();
            this.tableLayoutPanel12.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEmail)).BeginInit();
            this.tableLayoutPanel13.SuspendLayout();
            this.flowLayoutPanel16.SuspendLayout();
            this.flowLayoutPanel17.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl4
            // 
            this.tabControl4.Controls.Add(this.tabPageEmail);
            this.tabControl4.Controls.Add(this.tabPageSMS);
            this.tabControl4.Controls.Add(this.tabPageTelegram);
            this.tabControl4.Controls.Add(this.tabPageFBMessenger);
            this.tabControl4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl4.Location = new System.Drawing.Point(0, 0);
            this.tabControl4.Name = "tabControl4";
            this.tabControl4.SelectedIndex = 0;
            this.tabControl4.Size = new System.Drawing.Size(800, 450);
            this.tabControl4.TabIndex = 2;
            // 
            // tabPageEmail
            // 
            this.tabPageEmail.Controls.Add(this.tableLayoutPanel12);
            this.tabPageEmail.Location = new System.Drawing.Point(4, 22);
            this.tabPageEmail.Name = "tabPageEmail";
            this.tabPageEmail.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEmail.Size = new System.Drawing.Size(792, 424);
            this.tabPageEmail.TabIndex = 0;
            this.tabPageEmail.Text = "Email";
            this.tabPageEmail.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel12
            // 
            this.tableLayoutPanel12.ColumnCount = 1;
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel12.Controls.Add(this.dgvEmail, 0, 1);
            this.tableLayoutPanel12.Controls.Add(this.tableLayoutPanel13, 0, 0);
            this.tableLayoutPanel12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel12.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel12.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel12.Name = "tableLayoutPanel12";
            this.tableLayoutPanel12.RowCount = 2;
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel12.Size = new System.Drawing.Size(786, 418);
            this.tableLayoutPanel12.TabIndex = 1;
            // 
            // dgvEmail
            // 
            this.dgvEmail.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvEmail.Location = new System.Drawing.Point(0, 40);
            this.dgvEmail.Margin = new System.Windows.Forms.Padding(0);
            this.dgvEmail.Name = "dgvEmail";
            this.dgvEmail.Size = new System.Drawing.Size(786, 378);
            this.dgvEmail.TabIndex = 2;
            // 
            // tableLayoutPanel13
            // 
            this.tableLayoutPanel13.ColumnCount = 2;
            this.tableLayoutPanel13.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel13.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel13.Controls.Add(this.flowLayoutPanel16, 1, 0);
            this.tableLayoutPanel13.Controls.Add(this.flowLayoutPanel17, 0, 0);
            this.tableLayoutPanel13.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel13.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel13.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel13.Name = "tableLayoutPanel13";
            this.tableLayoutPanel13.RowCount = 1;
            this.tableLayoutPanel13.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel13.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel13.Size = new System.Drawing.Size(786, 40);
            this.tableLayoutPanel13.TabIndex = 1;
            // 
            // flowLayoutPanel16
            // 
            this.flowLayoutPanel16.Controls.Add(this.btnAddEmail);
            this.flowLayoutPanel16.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel16.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel16.Location = new System.Drawing.Point(393, 0);
            this.flowLayoutPanel16.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel16.Name = "flowLayoutPanel16";
            this.flowLayoutPanel16.Size = new System.Drawing.Size(393, 40);
            this.flowLayoutPanel16.TabIndex = 2;
            // 
            // btnAddEmail
            // 
            this.btnAddEmail.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddEmail.Location = new System.Drawing.Point(292, 0);
            this.btnAddEmail.Margin = new System.Windows.Forms.Padding(0);
            this.btnAddEmail.Name = "btnAddEmail";
            this.btnAddEmail.Size = new System.Drawing.Size(101, 26);
            this.btnAddEmail.TabIndex = 2;
            this.btnAddEmail.Text = "Add Email";
            this.btnAddEmail.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel17
            // 
            this.flowLayoutPanel17.Controls.Add(this.txtSearchEmail);
            this.flowLayoutPanel17.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel17.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel17.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel17.Name = "flowLayoutPanel17";
            this.flowLayoutPanel17.Size = new System.Drawing.Size(393, 40);
            this.flowLayoutPanel17.TabIndex = 1;
            // 
            // txtSearchEmail
            // 
            this.txtSearchEmail.Location = new System.Drawing.Point(0, 0);
            this.txtSearchEmail.Margin = new System.Windows.Forms.Padding(0);
            this.txtSearchEmail.Name = "txtSearchEmail";
            this.txtSearchEmail.Size = new System.Drawing.Size(344, 20);
            this.txtSearchEmail.TabIndex = 0;
            // 
            // tabPageSMS
            // 
            this.tabPageSMS.Location = new System.Drawing.Point(4, 22);
            this.tabPageSMS.Name = "tabPageSMS";
            this.tabPageSMS.Size = new System.Drawing.Size(1129, 532);
            this.tabPageSMS.TabIndex = 1;
            this.tabPageSMS.Text = "SMS (Smart A2P)";
            this.tabPageSMS.UseVisualStyleBackColor = true;
            // 
            // tabPageTelegram
            // 
            this.tabPageTelegram.Location = new System.Drawing.Point(4, 22);
            this.tabPageTelegram.Name = "tabPageTelegram";
            this.tabPageTelegram.Size = new System.Drawing.Size(1129, 532);
            this.tabPageTelegram.TabIndex = 2;
            this.tabPageTelegram.Text = "Telegram";
            this.tabPageTelegram.UseVisualStyleBackColor = true;
            // 
            // tabPageFBMessenger
            // 
            this.tabPageFBMessenger.Location = new System.Drawing.Point(4, 22);
            this.tabPageFBMessenger.Name = "tabPageFBMessenger";
            this.tabPageFBMessenger.Size = new System.Drawing.Size(1129, 532);
            this.tabPageFBMessenger.TabIndex = 3;
            this.tabPageFBMessenger.Text = "FB Messenger";
            this.tabPageFBMessenger.UseVisualStyleBackColor = true;
            // 
            // SettingsSystemAccounts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabControl4);
            this.Name = "SettingsSystemAccounts";
            this.Text = "SettingsSystemAccounts";
            this.tabControl4.ResumeLayout(false);
            this.tabPageEmail.ResumeLayout(false);
            this.tableLayoutPanel12.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEmail)).EndInit();
            this.tableLayoutPanel13.ResumeLayout(false);
            this.flowLayoutPanel16.ResumeLayout(false);
            this.flowLayoutPanel17.ResumeLayout(false);
            this.flowLayoutPanel17.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl4;
        private System.Windows.Forms.TabPage tabPageEmail;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel12;
        private System.Windows.Forms.DataGridView dgvEmail;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel13;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel16;
        private System.Windows.Forms.Button btnAddEmail;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel17;
        private System.Windows.Forms.TextBox txtSearchEmail;
        private System.Windows.Forms.TabPage tabPageSMS;
        private System.Windows.Forms.TabPage tabPageTelegram;
        private System.Windows.Forms.TabPage tabPageFBMessenger;
    }
}