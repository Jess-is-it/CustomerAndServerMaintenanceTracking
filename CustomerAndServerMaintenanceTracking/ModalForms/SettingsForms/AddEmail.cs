using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms
{
    public partial class AddEmail : Form
    {
        private readonly EmailSettingsRepository _emailSettingsRepository;
        private readonly EmailSettings _editingSetting; // Will be null in "Add" mode

        // Constructor for "Add" mode
        public AddEmail()
        {
            InitializeComponent();
            _emailSettingsRepository = new EmailSettingsRepository();
            this.Text = "Add New Email Configuration";
            label1.Text = "Add New Email Configuration";
            btnSave.Text = "Save";
        }

        // NEW: Constructor for "Edit" mode
        public AddEmail(EmailSettings settingToEdit)
        {
            InitializeComponent();
            _emailSettingsRepository = new EmailSettingsRepository();
            _editingSetting = settingToEdit; // Store the setting being edited

            // --- Configure form for editing ---
            this.Text = "Edit Email Configuration";
            label1.Text = $"Edit: {settingToEdit.SettingName}";
            btnSave.Text = "Update";

            // Load existing data into controls
            txtSettingName.Text = _editingSetting.SettingName;
            txtServer.Text = _editingSetting.SmtpServer;
            txtPort.Text = _editingSetting.SmtpPort.ToString();
            chkEnableSsl.Checked = _editingSetting.EnableSsl;
            txtSenderEmail.Text = _editingSetting.SenderEmail;
            txtDisplayName.Text = _editingSetting.SenderDisplayName;
            txtUser.Text = _editingSetting.SmtpUsername;
            txtPassword.Text = _editingSetting.SmtpPassword;
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            // --- VALIDATION (same for both modes) ---
            if (string.IsNullOrWhiteSpace(txtSettingName.Text))
            {
                MessageBox.Show("Setting Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSettingName.Focus();
                return;
            }
            if (!int.TryParse(txtPort.Text, out int port) || port <= 0)
            {
                MessageBox.Show("Please enter a valid SMTP Port number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPort.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                MessageBox.Show("SMTP Server cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServer.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSenderEmail.Text))
            {
                MessageBox.Show("Sender Email ('From') cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSenderEmail.Focus();
                return;
            }

            try
            {
                // --- DECIDE whether to ADD or UPDATE ---
                if (_editingSetting == null) // ADD Mode
                {
                    var newSetting = new EmailSettings
                    {
                        SettingName = txtSettingName.Text.Trim(),
                        SmtpServer = txtServer.Text.Trim(),
                        SmtpPort = port,
                        EnableSsl = chkEnableSsl.Checked,
                        SmtpUsername = txtUser.Text.Trim(),
                        SmtpPassword = txtPassword.Text,
                        SenderEmail = txtSenderEmail.Text.Trim(),
                        SenderDisplayName = txtDisplayName.Text.Trim(),
                        IsDefault = false
                    };
                    _emailSettingsRepository.AddEmailSetting(newSetting);
                    MessageBox.Show("New email setting saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else // EDIT Mode
                {
                    // Update the object that was passed to the constructor
                    _editingSetting.SettingName = txtSettingName.Text.Trim();
                    _editingSetting.SmtpServer = txtServer.Text.Trim();
                    _editingSetting.SmtpPort = port;
                    _editingSetting.EnableSsl = chkEnableSsl.Checked;
                    _editingSetting.SmtpUsername = txtUser.Text.Trim();
                    _editingSetting.SmtpPassword = txtPassword.Text;
                    _editingSetting.SenderEmail = txtSenderEmail.Text.Trim();
                    _editingSetting.SenderDisplayName = txtDisplayName.Text.Trim();

                    _emailSettingsRepository.UpdateEmailSetting(_editingSetting);
                    MessageBox.Show("Email setting updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UQ_EmailSettings_SettingName"))
                {
                    MessageBox.Show($"An email setting with the name '{txtSettingName.Text.Trim()}' already exists. Please use a unique name.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtSettingName.Focus();
                }
                else
                {
                    MessageBox.Show($"An error occurred while saving: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
