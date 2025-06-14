using CustomerAndServerMaintenanceTracking.AppLogs;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class SettingsUsers : Form
    {
        private OverlayForm overlayForm;
        private UserRoleRepository _userRoleRepository;
        private UserAccountRepository _userAccountRepository;
        private ServiceLogRepository _serviceLogRepositoryForSettings;

        public SettingsUsers()
        {
            InitializeComponent();
            InitializeRepositories();

            #region Roles
            if (_userRoleRepository != null)
            {
                InitializeRolesGrid();
                LoadRolesData();
            }
            else
            {
                if (tabControl3.TabPages.Contains(tabPage5))
                {
                    tabPage5.Enabled = false;
                    tabPage5.Text = "Roles (Error)";
                }
            }
            #endregion

            #region Users
            if (_userAccountRepository != null)
            {
                InitializeActiveUsersGrid();
                InitializeInactiveUsersGrid();
                LoadActiveUsersData();
                LoadInactiveUsersData();
            }
            else
            {
                if (tabControl3.TabPages.Contains(tabPage4))
                {
                    tabPage4.Enabled = false;
                    tabPage4.Text = "Users (Error)";
                }
            }
            #endregion

            #region Event Handlers
            // Roles
            this.btnAddRole.Click += new System.EventHandler(this.btnAddRole_Click);
            this.txtSearchAccountRoles.TextChanged += new System.EventHandler(this.txtSearchAccountRoles_TextChanged);
            this.dataGridView1.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewRoles_CellPainting);
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewRoles_CellClick);

            // Users
            this.btnAddUser.Click += new System.EventHandler(this.btnAddUser_Click);
            this.txtSearchUserListActive.TextChanged += new System.EventHandler(this.txtSearchUserListActive_TextChanged);
            this.txtSearchUserListInactive.TextChanged += new System.EventHandler(this.txtSearchUserListInactive_TextChanged);

            this.dataGridViewUsersActive.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewActiveUsers_CellPainting);
            this.dataGridViewUsersActive.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewActiveUsers_CellClick);
            this.dataGridViewUsersActive.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewUsersActive_CellContentClick);
            this.dataGridViewUsersActive.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridViewUsers_CellFormatting);

            this.dataGridViewUsersInactive.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewInactiveUsers_CellPainting);
            this.dataGridViewUsersInactive.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewInactiveUsers_CellClick);
            this.dataGridViewUsersInactive.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewUsersInactive_CellContentClick);
            this.dataGridViewUsersInactive.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridViewUsers_CellFormatting);
            #endregion
        }

        private void InitializeRepositories()
        {
            try
            {
                _serviceLogRepositoryForSettings = new ServiceLogRepository();
                _userRoleRepository = new UserRoleRepository();
                _userAccountRepository = new UserAccountRepository();
            }
            catch (ConfigurationErrorsException configEx)
            {
                MessageBox.Show($"A critical configuration error occurred: {configEx.Message}\nPlease check your App.config file.\nThe application might not function correctly.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during initialization: {ex.Message}\nSome functionality may be affected.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        #region Overlay
        private void ShowOverlay()
        {
            if (overlayForm == null || overlayForm.IsDisposed)
            {
                overlayForm = new OverlayForm();
            }
            Form formToCover = this.MdiParent ?? this;
            overlayForm.Owner = formToCover;
            overlayForm.StartPosition = FormStartPosition.Manual;
            Point location = formToCover.PointToScreen(Point.Empty);
            overlayForm.Bounds = new Rectangle(location, formToCover.ClientSize);
            overlayForm.Show();
            overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                overlayForm.Close();
                overlayForm = null;
            }
        }
        #endregion

        #region Roles
        private void InitializeRolesGrid()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.AutoGenerateColumns = false;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoleId", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoleName", DataPropertyName = "RoleName", HeaderText = "Role Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 40 });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoleDescription", DataPropertyName = "Description", HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 40 });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoleDateCreated", DataPropertyName = "DateCreated", HeaderText = "Date Created", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });

            DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn { Name = "RoleActionColumn", HeaderText = "Action", ReadOnly = true, Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dataGridView1.Columns.Add(actionColumn);

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
        }

        private void LoadRolesData()
        {
            try
            {
                if (_userRoleRepository == null) return;
                var roles = _userRoleRepository.GetRoles() ?? new List<UserRole>();

                string searchTerm = txtSearchAccountRoles.Text.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    roles = roles.Where(r =>
                                    (r.RoleName != null && r.RoleName.ToLowerInvariant().Contains(searchTerm)) ||
                                    (r.Description != null && r.Description.ToLowerInvariant().Contains(searchTerm)))
                                    .ToList();
                }

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = roles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user roles: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddRole_Click(object sender, EventArgs e)
        {
            using (AddUserRole addUserRoleForm = new AddUserRole())
            {
                addUserRoleForm.RoleSaved += AddUserRoleForm_RoleSaved;
                addUserRoleForm.StartPosition = FormStartPosition.CenterScreen;
                ShowOverlay();
                addUserRoleForm.ShowDialog(this);
                CloseOverlay();
                addUserRoleForm.RoleSaved -= AddUserRoleForm_RoleSaved;
            }
        }

        private void AddUserRoleForm_RoleSaved(object sender, EventArgs e)
        {
            LoadRolesData();
        }

        private void txtSearchAccountRoles_TextChanged(object sender, EventArgs e)
        {
            LoadRolesData();
        }

        private void dataGridViewRoles_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Columns["RoleActionColumn"] != null && e.ColumnIndex == dataGridView1.Columns["RoleActionColumn"].Index)
            {
                e.PaintBackground(e.ClipBounds, true);
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                if (buttonWidth < 10) buttonWidth = 10;
                int buttonHeight = e.CellBounds.Height - 4;
                if (buttonHeight < 5) buttonHeight = 5;

                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", dataGridView1.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", dataGridView1.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }

        private void dataGridViewRoles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dataGridView1.Columns["RoleActionColumn"] == null || e.ColumnIndex != dataGridView1.Columns["RoleActionColumn"].Index)
                return;

            if (dataGridView1.Rows[e.RowIndex].DataBoundItem is UserRole selectedRoleData)
            {
                Rectangle cellRect = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridView1.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;

                if (relativeX <= buttonWidth + 2) // Edit
                {
                    UserRole roleWithPermissions = _userRoleRepository.GetRoleByIdWithPermissions(selectedRoleData.Id);
                    if (roleWithPermissions == null)
                    {
                        MessageBox.Show("Could not retrieve role details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (AddUserRole editUserRoleForm = new AddUserRole(roleWithPermissions))
                    {
                        editUserRoleForm.RoleSaved += AddUserRoleForm_RoleSaved;
                        editUserRoleForm.StartPosition = FormStartPosition.CenterParent;
                        ShowOverlay();
                        editUserRoleForm.ShowDialog(this);
                        CloseOverlay();
                        editUserRoleForm.RoleSaved -= AddUserRoleForm_RoleSaved;
                    }
                }
                else // Delete
                {
                    if (MessageBox.Show($"Delete the role '{selectedRoleData.RoleName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            if (_userRoleRepository.DeleteRole(selectedRoleData.Id))
                            {
                                MessageBox.Show("Role deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadRolesData();
                            }
                            else
                            {
                                MessageBox.Show("Could not delete role.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            MessageBox.Show(ex.Message, "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting role: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        #endregion

        #region Users
        private void dataGridViewUsers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv != null && dgv.Columns[e.ColumnIndex].Name == "UserRoleName" && e.RowIndex >= 0)
            {
                if (dgv.Rows[e.RowIndex].DataBoundItem is UserAccount userAccount)
                {
                    e.Value = userAccount.Role?.RoleName ?? "N/A";
                    e.FormattingApplied = true;
                }
            }
        }

        #region Active Users
        private void InitializeActiveUsersGrid()
        {
            if (dataGridViewUsersActive == null) return;
            AddUserGridColumns(dataGridViewUsersActive, isActiveGrid: true);
        }

        private void LoadActiveUsersData()
        {
            if (_userAccountRepository == null || dataGridViewUsersActive == null) return;
            try
            {
                var activeUsers = _userAccountRepository.GetUserAccountsWithRoles().Where(u => u.IsActive).ToList();
                string searchTerm = txtSearchUserListActive.Text.Trim().ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    activeUsers = activeUsers.Where(u =>
                        (u.FullName?.ToLowerInvariant().Contains(searchTerm) == true) ||
                        (u.Username?.ToLowerInvariant().Contains(searchTerm) == true) ||
                        (u.Email?.ToLowerInvariant().Contains(searchTerm) == true) ||
                        (u.Role?.RoleName?.ToLowerInvariant().Contains(searchTerm) == true)
                    ).ToList();
                }
                dataGridViewUsersActive.DataSource = null;
                dataGridViewUsersActive.DataSource = activeUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading active users: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearchUserListActive_TextChanged(object sender, EventArgs e)
        {
            LoadActiveUsersData();
        }

        private void dataGridViewActiveUsers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            PaintUserActionButtons(sender as DataGridView, e, "UserActionColumn");
        }

        private void dataGridViewActiveUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleUserActionCellClick(dataGridViewUsersActive, e);
        }

        private void dataGridViewUsersActive_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewUsersActive.Columns[e.ColumnIndex].Name != "UserIsActiveCol" || e.RowIndex < 0) return;

            dataGridViewUsersActive.CommitEdit(DataGridViewDataErrorContexts.Commit);

            if (dataGridViewUsersActive.Rows[e.RowIndex].DataBoundItem is UserAccount selectedUser && !selectedUser.IsActive)
            {
                using (DeactivationReasonForm reasonForm = new DeactivationReasonForm(selectedUser.Username))
                {
                    ShowOverlay();
                    DialogResult dialogResult = reasonForm.ShowDialog(this);
                    CloseOverlay();

                    if (dialogResult == DialogResult.OK)
                    {
                        selectedUser.DeactivationReason = reasonForm.Reason;
                        _serviceLogRepositoryForSettings?.WriteLog(new ServiceLogEntry
                        {
                            ServiceName = "SettingsForm.UserManagement",
                            LogLevel = "INFO",
                            Message = $"User '{selectedUser.Username}' deactivated. Reason: {reasonForm.Reason}"
                        });

                        _userAccountRepository.UpdateUserAccount(selectedUser);
                        LoadActiveUsersData();
                        LoadInactiveUsersData();
                    }
                    else
                    {
                        LoadActiveUsersData(); // Revert checkbox
                    }
                }
            }
        }
        #endregion

        #region Inactive Users
        private void InitializeInactiveUsersGrid()
        {
            if (dataGridViewUsersInactive == null) return;
            AddUserGridColumns(dataGridViewUsersInactive, isActiveGrid: false);
        }

        private void LoadInactiveUsersData()
        {
            if (_userAccountRepository == null || dataGridViewUsersInactive == null) return;
            try
            {
                var inactiveUsers = _userAccountRepository.GetUserAccountsWithRoles().Where(u => !u.IsActive).ToList();
                string searchTerm = txtSearchUserListInactive.Text.Trim().ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    inactiveUsers = inactiveUsers.Where(u =>
                       (u.FullName?.ToLowerInvariant().Contains(searchTerm) == true) ||
                       (u.Username?.ToLowerInvariant().Contains(searchTerm) == true) ||
                       (u.Email?.ToLowerInvariant().Contains(searchTerm) == true) ||
                       (u.Role?.RoleName?.ToLowerInvariant().Contains(searchTerm) == true)
                   ).ToList();
                }
                dataGridViewUsersInactive.DataSource = null;
                dataGridViewUsersInactive.DataSource = inactiveUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inactive users: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearchUserListInactive_TextChanged(object sender, EventArgs e)
        {
            LoadInactiveUsersData();
        }

        private void dataGridViewInactiveUsers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            PaintUserActionButtons(sender as DataGridView, e, "UserActionColumn");
        }

        private void dataGridViewInactiveUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleUserActionCellClick(dataGridViewUsersInactive, e);
        }

        private void dataGridViewUsersInactive_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewUsersInactive.Columns[e.ColumnIndex].Name != "UserIsActiveCol" || e.RowIndex < 0) return;

            dataGridViewUsersInactive.CommitEdit(DataGridViewDataErrorContexts.Commit);

            if (dataGridViewUsersInactive.Rows[e.RowIndex].DataBoundItem is UserAccount selectedUser && selectedUser.IsActive)
            {
                if (MessageBox.Show($"Activate user '{selectedUser.Username}'?", "Confirm Activation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    selectedUser.DeactivationReason = null; // Clear reason
                    _userAccountRepository.UpdateUserAccount(selectedUser);
                    LoadActiveUsersData();
                    LoadInactiveUsersData();
                }
                else
                {
                    LoadInactiveUsersData(); // Revert checkbox
                }
            }
        }
        #endregion

        #region Common User Methods
        private void AddUserGridColumns(DataGridView dgv, bool isActiveGrid)
        {
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserFullName", DataPropertyName = "FullName", HeaderText = "Full Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserUsername", DataPropertyName = "Username", HeaderText = "Username", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserRoleName", HeaderText = "Role", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });

            if (!isActiveGrid)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserDeactivationReason", DataPropertyName = "DeactivationReason", HeaderText = "Deactivation Reason", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
            }

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserEmail", DataPropertyName = "Email", HeaderText = "Email", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserPhoneNumber", DataPropertyName = "PhoneNumber", HeaderText = "Phone", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserDateCreated", DataPropertyName = "DateCreated", HeaderText = "Date Created", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserLastLogin", DataPropertyName = "LastLoginDate", HeaderText = "Last Login", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn { Name = "UserIsActiveCol", DataPropertyName = "IsActive", HeaderText = isActiveGrid ? "Active" : "Activate", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserActionColumn", HeaderText = "Action", ReadOnly = true, Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

            dgv.AllowUserToAddRows = false;
        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            using (AddUserAccount addUserAccountForm = new AddUserAccount())
            {
                addUserAccountForm.UserAccountSaved += AddUserAccountForm_UserAccountSaved;
                ShowOverlay();
                addUserAccountForm.StartPosition = FormStartPosition.CenterScreen;
                addUserAccountForm.ShowDialog(this);
                CloseOverlay();
                addUserAccountForm.UserAccountSaved -= AddUserAccountForm_UserAccountSaved;
            }
        }

        private void AddUserAccountForm_UserAccountSaved(object sender, EventArgs e)
        {
            LoadActiveUsersData();
            LoadInactiveUsersData();
        }

        private void PaintUserActionButtons(DataGridView dgv, DataGridViewCellPaintingEventArgs e, string actionColumnName)
        {
            if (dgv != null && e.RowIndex >= 0 && dgv.Columns[actionColumnName] != null && e.ColumnIndex == dgv.Columns[actionColumnName].Index)
            {
                e.PaintBackground(e.ClipBounds, true);
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                if (buttonWidth < 10) buttonWidth = 10;
                int buttonHeight = e.CellBounds.Height - 4;
                if (buttonHeight < 5) buttonHeight = 5;

                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", dgv.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", dgv.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                e.Handled = true;
            }
        }

        private void HandleUserActionCellClick(DataGridView dgv, DataGridViewCellEventArgs e)
        {
            if (dgv == null || e.RowIndex < 0 || dgv.Columns["UserActionColumn"] == null || e.ColumnIndex != dgv.Columns["UserActionColumn"].Index)
                return;

            if (dgv.Rows[e.RowIndex].DataBoundItem is UserAccount selectedUser)
            {
                Rectangle cellRect = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dgv.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;

                if (relativeX <= buttonWidth + 2) // Edit
                {
                    UserAccount userToEdit = _userAccountRepository.GetUserAccountById(selectedUser.Id);
                    if (userToEdit == null)
                    {
                        MessageBox.Show("Could not retrieve user for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    using (AddUserAccount editUserForm = new AddUserAccount(userToEdit))
                    {
                        editUserForm.UserAccountSaved += AddUserAccountForm_UserAccountSaved;
                        editUserForm.StartPosition = FormStartPosition.CenterScreen;
                        ShowOverlay();
                        editUserForm.ShowDialog(this);
                        CloseOverlay();
                        editUserForm.UserAccountSaved -= AddUserAccountForm_UserAccountSaved;
                    }
                }
                else // Delete
                {
                    if (MessageBox.Show($"Delete user '{selectedUser.Username}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            if (_userAccountRepository.DeleteUserAccount(selectedUser.Id))
                            {
                                MessageBox.Show("User deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadActiveUsersData();
                                LoadInactiveUsersData();
                            }
                            else
                            {
                                MessageBox.Show("Could not delete user.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        #endregion
        #endregion
    }
}
