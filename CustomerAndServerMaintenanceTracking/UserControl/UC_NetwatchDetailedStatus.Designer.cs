namespace CustomerAndServerMaintenanceTracking.UserControl
{
    partial class UC_NetwatchDetailedStatus
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
            this.lblNetwatchNameTitle = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvDetailedStatus = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetailedStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // lblNetwatchNameTitle
            // 
            this.lblNetwatchNameTitle.AutoSize = true;
            this.lblNetwatchNameTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNetwatchNameTitle.Location = new System.Drawing.Point(19, 16);
            this.lblNetwatchNameTitle.Name = "lblNetwatchNameTitle";
            this.lblNetwatchNameTitle.Size = new System.Drawing.Size(70, 25);
            this.lblNetwatchNameTitle.TabIndex = 0;
            this.lblNetwatchNameTitle.Text = "label1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblNetwatchNameTitle, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dgvDetailedStatus, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(16);
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(395, 508);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // dgvDetailedStatus
            // 
            this.dgvDetailedStatus.AllowUserToAddRows = false;
            this.dgvDetailedStatus.AllowUserToDeleteRows = false;
            this.dgvDetailedStatus.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDetailedStatus.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDetailedStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDetailedStatus.Location = new System.Drawing.Point(19, 59);
            this.dgvDetailedStatus.Name = "dgvDetailedStatus";
            this.dgvDetailedStatus.ReadOnly = true;
            this.dgvDetailedStatus.Size = new System.Drawing.Size(357, 430);
            this.dgvDetailedStatus.TabIndex = 1;
            // 
            // UC_NetwatchDetailedStatus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UC_NetwatchDetailedStatus";
            this.Size = new System.Drawing.Size(395, 508);
            this.Load += new System.EventHandler(this.UC_NetwatchDetailedStatus_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetailedStatus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblNetwatchNameTitle;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridView dgvDetailedStatus;
    }
}
