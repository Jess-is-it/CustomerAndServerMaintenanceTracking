namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    partial class NetwatchDetailedStatusForm
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblNetwatchName = new System.Windows.Forms.Label();
            this.dgvDetailedStatus = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetailedStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblNetwatchName, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dgvDetailedStatus, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(16);
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(457, 544);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lblNetwatchName
            // 
            this.lblNetwatchName.AutoSize = true;
            this.lblNetwatchName.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNetwatchName.Location = new System.Drawing.Point(19, 16);
            this.lblNetwatchName.Name = "lblNetwatchName";
            this.lblNetwatchName.Size = new System.Drawing.Size(70, 25);
            this.lblNetwatchName.TabIndex = 0;
            this.lblNetwatchName.Text = "label1";
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
            this.dgvDetailedStatus.Size = new System.Drawing.Size(419, 466);
            this.dgvDetailedStatus.TabIndex = 1;
            // 
            // NetwatchDetailedStatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(457, 544);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "NetwatchDetailedStatusForm";
            this.Text = "NetwatchDetailedStatusForm";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetailedStatus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblNetwatchName;
        private System.Windows.Forms.DataGridView dgvDetailedStatus;
    }
}