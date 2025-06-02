using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms
{
    public partial class DeactivationReasonForm : Form
    {
        public string Reason { get; private set; }
        private string _username;

        public DeactivationReasonForm(string username)
        {
            InitializeComponent();
            _username = username;
            this.Text = $"Deactivate User '{_username}'";
            // Assuming lblPrompt is your Label for the message
            if (lblPrompt != null)
            {
                lblPrompt.Text = $"Please provide a reason for deactivating user '{_username}':";
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Reason cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReason.Focus();
                return;
            }
            Reason = txtReason.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
