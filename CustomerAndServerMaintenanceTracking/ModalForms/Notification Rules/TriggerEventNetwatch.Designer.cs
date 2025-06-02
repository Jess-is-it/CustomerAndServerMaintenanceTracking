namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    partial class TriggerEventNetwatch
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.textSearchNetwatch = new System.Windows.Forms.TextBox();
            this.chkListBoxNetwatch = new System.Windows.Forms.CheckedListBox();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.rbAll_IPs = new System.Windows.Forms.RadioButton();
            this.rbSpecificIPs = new System.Windows.Forms.RadioButton();
            this.grpBoxIPs = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.txtSearchIPs = new System.Windows.Forms.TextBox();
            this.chkListBoxNetwatchIPs = new System.Windows.Forms.CheckedListBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.chkListBoxNetwatchStatus = new System.Windows.Forms.CheckedListBox();
            this.flowLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.grpBoxIPs.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Controls.Add(this.groupBox1);
            this.flowLayoutPanel1.Controls.Add(this.groupBox8);
            this.flowLayoutPanel1.Controls.Add(this.grpBoxIPs);
            this.flowLayoutPanel1.Controls.Add(this.groupBox2);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(436, 581);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.White;
            this.groupBox1.Controls.Add(this.tableLayoutPanel2);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(429, 164);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "1. Select Netwatch Configuration(s):";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.textSearchNetwatch, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.chkListBoxNetwatch, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(423, 145);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // textSearchNetwatch
            // 
            this.textSearchNetwatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textSearchNetwatch.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textSearchNetwatch.Location = new System.Drawing.Point(3, 3);
            this.textSearchNetwatch.Name = "textSearchNetwatch";
            this.textSearchNetwatch.Size = new System.Drawing.Size(417, 26);
            this.textSearchNetwatch.TabIndex = 0;
            // 
            // chkListBoxNetwatch
            // 
            this.chkListBoxNetwatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkListBoxNetwatch.FormattingEnabled = true;
            this.chkListBoxNetwatch.Location = new System.Drawing.Point(3, 33);
            this.chkListBoxNetwatch.Name = "chkListBoxNetwatch";
            this.chkListBoxNetwatch.Size = new System.Drawing.Size(417, 109);
            this.chkListBoxNetwatch.TabIndex = 1;
            // 
            // groupBox8
            // 
            this.groupBox8.BackColor = System.Drawing.Color.White;
            this.groupBox8.Controls.Add(this.tableLayoutPanel5);
            this.groupBox8.Location = new System.Drawing.Point(3, 173);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(429, 74);
            this.groupBox8.TabIndex = 7;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "2. Specification";
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.Controls.Add(this.rbAll_IPs, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.rbSpecificIPs, 1, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.Padding = new System.Windows.Forms.Padding(8);
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(423, 55);
            this.tableLayoutPanel5.TabIndex = 0;
            // 
            // rbAll_IPs
            // 
            this.rbAll_IPs.AutoSize = true;
            this.rbAll_IPs.Checked = true;
            this.rbAll_IPs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAll_IPs.Location = new System.Drawing.Point(11, 11);
            this.rbAll_IPs.Name = "rbAll_IPs";
            this.rbAll_IPs.Size = new System.Drawing.Size(197, 33);
            this.rbAll_IPs.TabIndex = 4;
            this.rbAll_IPs.TabStop = true;
            this.rbAll_IPs.Text = "All IPs in a netwatch";
            this.rbAll_IPs.UseVisualStyleBackColor = true;
            // 
            // rbSpecificIPs
            // 
            this.rbSpecificIPs.AutoSize = true;
            this.rbSpecificIPs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbSpecificIPs.Location = new System.Drawing.Point(214, 11);
            this.rbSpecificIPs.Name = "rbSpecificIPs";
            this.rbSpecificIPs.Size = new System.Drawing.Size(198, 33);
            this.rbSpecificIPs.TabIndex = 1;
            this.rbSpecificIPs.Text = "Specific IPs in a netwatch";
            this.rbSpecificIPs.UseVisualStyleBackColor = true;
            // 
            // grpBoxIPs
            // 
            this.grpBoxIPs.BackColor = System.Drawing.Color.White;
            this.grpBoxIPs.Controls.Add(this.tableLayoutPanel4);
            this.grpBoxIPs.Location = new System.Drawing.Point(3, 253);
            this.grpBoxIPs.Name = "grpBoxIPs";
            this.grpBoxIPs.Size = new System.Drawing.Size(429, 164);
            this.grpBoxIPs.TabIndex = 8;
            this.grpBoxIPs.TabStop = false;
            this.grpBoxIPs.Text = "IPs";
            this.grpBoxIPs.Visible = false;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.txtSearchIPs, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.chkListBoxNetwatchIPs, 0, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(423, 145);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // txtSearchIPs
            // 
            this.txtSearchIPs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearchIPs.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSearchIPs.Location = new System.Drawing.Point(3, 3);
            this.txtSearchIPs.Name = "txtSearchIPs";
            this.txtSearchIPs.Size = new System.Drawing.Size(417, 26);
            this.txtSearchIPs.TabIndex = 0;
            // 
            // chkListBoxNetwatchIPs
            // 
            this.chkListBoxNetwatchIPs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkListBoxNetwatchIPs.FormattingEnabled = true;
            this.chkListBoxNetwatchIPs.Location = new System.Drawing.Point(3, 33);
            this.chkListBoxNetwatchIPs.Name = "chkListBoxNetwatchIPs";
            this.chkListBoxNetwatchIPs.Size = new System.Drawing.Size(417, 109);
            this.chkListBoxNetwatchIPs.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.White;
            this.groupBox2.Controls.Add(this.tableLayoutPanel3);
            this.groupBox2.Location = new System.Drawing.Point(3, 423);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(429, 112);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Netwatch Status:";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.chkListBoxNetwatchStatus, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 93F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(423, 93);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // chkListBoxNetwatchStatus
            // 
            this.chkListBoxNetwatchStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkListBoxNetwatchStatus.FormattingEnabled = true;
            this.chkListBoxNetwatchStatus.Location = new System.Drawing.Point(3, 3);
            this.chkListBoxNetwatchStatus.Name = "chkListBoxNetwatchStatus";
            this.chkListBoxNetwatchStatus.Size = new System.Drawing.Size(417, 87);
            this.chkListBoxNetwatchStatus.TabIndex = 1;
            // 
            // TriggerEventNetwatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "TriggerEventNetwatch";
            this.Size = new System.Drawing.Size(436, 581);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.grpBoxIPs.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.RadioButton rbAll_IPs;
        private System.Windows.Forms.RadioButton rbSpecificIPs;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox textSearchNetwatch;
        private System.Windows.Forms.CheckedListBox chkListBoxNetwatch;
        private System.Windows.Forms.GroupBox grpBoxIPs;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TextBox txtSearchIPs;
        private System.Windows.Forms.CheckedListBox chkListBoxNetwatchIPs;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.CheckedListBox chkListBoxNetwatchStatus;
    }
}
