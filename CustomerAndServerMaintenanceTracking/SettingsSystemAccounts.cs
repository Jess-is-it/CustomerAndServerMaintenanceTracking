using CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class SettingsSystemAccounts : Form
    {
        private EmailSettingsRepository _emailSettingsRepository;

        public SettingsSystemAccounts()
        {
            InitializeComponent();

            #region Repository Initialization
            try
            {
                _emailSettingsRepository = new EmailSettingsRepository();
            }
            catch (ConfigurationErrorsException configEx)
            {
                MessageBox.Show($"A critical configuration error occurred: {configEx.Message}\nPlease check your App.config file.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Disable the form or specific tabs if initialization fails
                this.tabPageEmail.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while initializing repositories: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.tabPageEmail.Enabled = false;
            }
            #endregion

            #region UI and Event Initialization
            if (_emailSettingsRepository != null)
            {
                InitializeEmailSettingsGrid();
                LoadEmailSettingsGrid();

                // Wire up event handlers
                this.btnAddEmail.Click += new System.EventHandler(this.btnAddEmail_Click);
                this.dgvEmail.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dgvEmail_CellPainting);
                this.dgvEmail.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvEmail_CellClick);
            }
            #endregion
        }


        #region Email Management
        private void InitializeEmailSettingsGrid()
        {
            dgvEmail.Columns.Clear();
            dgvEmail.AutoGenerateColumns = false;

            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsDefault", DataPropertyName = "IsDefault", HeaderText = "Default", Width = 60 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SettingName", DataPropertyName = "SettingName", HeaderText = "Setting Name", Width = 150 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SmtpServer", DataPropertyName = "SmtpServer", HeaderText = "SMTP Server", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SmtpPort", DataPropertyName = "SmtpPort", HeaderText = "Port", Width = 50 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SenderEmail", DataPropertyName = "SenderEmail", HeaderText = "Sender Email", Width = 200 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", ReadOnly = true, Width = 200 });

            dgvEmail.AllowUserToAddRows = false;
            dgvEmail.ReadOnly = true;
            dgvEmail.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEmail.MultiSelect = false;
        }

        private void LoadEmailSettingsGrid()
        {
            try
            {
                var settingsList = _emailSettingsRepository.GetAllEmailSettings();
                dgvEmail.DataSource = null;
                if (settingsList != null)
                {
                    dgvEmail.DataSource = settingsList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load email settings: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddEmail_Click(object sender, EventArgs e)
        {
            using (var addForm = new AddEmail())
            {
                addForm.StartPosition = FormStartPosition.CenterParent;
                if (addForm.ShowDialog(this) == DialogResult.OK)
                {
                    LoadEmailSettingsGrid();
                }
            }
        }

        private void dgvEmail_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvEmail.Columns["Action"].Index)
                return;

            e.PaintBackground(e.ClipBounds, true);

            Font actionFont = new Font(dgvEmail.Font.FontFamily, 8f);
            try
            {
                int padding = 4;
                int smallButtonWidth = 50;
                int largeButtonWidth = e.CellBounds.Width - (smallButtonWidth * 2) - (padding * 4);
                int topOffset = e.CellBounds.Top + 2;
                int buttonHeight = e.CellBounds.Height - 4;

                var setting = dgvEmail.Rows[e.RowIndex].DataBoundItem as EmailSettings;
                if (setting == null) return;

                string defaultActionText = setting.IsDefault ? "Default" : "Set as Default";
                Rectangle textRect = new Rectangle(e.CellBounds.X + padding, topOffset, largeButtonWidth, buttonHeight);
                TextRenderer.DrawText(e.Graphics, defaultActionText, actionFont, textRect, Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

                Rectangle editButtonRect = new Rectangle(textRect.Right + padding, topOffset, smallButtonWidth, buttonHeight);
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", actionFont, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                Rectangle deleteButtonRect = new Rectangle(editButtonRect.Right + padding, topOffset, smallButtonWidth, buttonHeight);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", actionFont, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
            }
            finally
            {
                actionFont?.Dispose();
            }
            e.Handled = true;
        }

        private void dgvEmail_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvEmail.Columns["Action"].Index)
                return;

            var setting = dgvEmail.Rows[e.RowIndex].DataBoundItem as EmailSettings;
            if (setting == null) return;

            Rectangle cellBounds = dgvEmail.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            Point mousePosition = dgvEmail.PointToClient(Cursor.Position);
            Point clickPointInCell = new Point(mousePosition.X - cellBounds.Left, mousePosition.Y - cellBounds.Top);

            int padding = 4;
            int smallButtonWidth = 50;
            int largeButtonWidth = cellBounds.Width - (smallButtonWidth * 2) - (padding * 4);
            int topMargin = 2;
            int buttonHeight = cellBounds.Height - (topMargin * 2);

            Rectangle defaultActionRect = new Rectangle(padding, topMargin, largeButtonWidth, buttonHeight);
            Rectangle editButtonRect = new Rectangle(defaultActionRect.Right + padding, topMargin, smallButtonWidth, buttonHeight);
            Rectangle deleteButtonRect = new Rectangle(editButtonRect.Right + padding, topMargin, smallButtonWidth, buttonHeight);

            if (defaultActionRect.Contains(clickPointInCell))
            {
                if (setting.IsDefault) return;
                try
                {
                    _emailSettingsRepository.SetDefaultEmailSetting(setting.Id);
                    LoadEmailSettingsGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to set default: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (editButtonRect.Contains(clickPointInCell))
            {
                using (var editForm = new AddEmail(setting))
                {
                    editForm.StartPosition = FormStartPosition.CenterParent;
                    if (editForm.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadEmailSettingsGrid();
                    }
                }
            }
            else if (deleteButtonRect.Contains(clickPointInCell))
            {
                if (setting.IsDefault)
                {
                    MessageBox.Show($"Cannot delete the default setting '{setting.SettingName}'. Please set another as default first.", "Cannot Delete Default", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show($"Are you sure you want to delete '{setting.SettingName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        _emailSettingsRepository.DeleteEmailSetting(setting.Id);
                        LoadEmailSettingsGrid();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion
    }
}
