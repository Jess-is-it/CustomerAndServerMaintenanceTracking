using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms
{
    public partial class AddUserAccount : Form
    {
        public event EventHandler UserAccountSaved;
        private UserRoleRepository _roleRepository;
        private UserAccountRepository _userAccountRepository;
        private UserAccount _editingUserAccount;

        public AddUserAccount()
        {
            InitializeComponent();
            _roleRepository = new UserRoleRepository();
            _userAccountRepository = new UserAccountRepository();
            SetupFormText(isEditMode: false);
            LoadRolesIntoComboBox();
            WireEvents();
        }

        public AddUserAccount(UserAccount userToEdit) : this()
        {
            _editingUserAccount = userToEdit ?? throw new ArgumentNullException(nameof(userToEdit));
            SetupFormText(isEditMode: true);
            LoadUserDataForEdit();
        }

        private void WireEvents()
        {
            this.btnAddUser.Click -= this.btnSaveUserAccount_Click;
            this.btnCancel.Click -= this.btnCancel_Click;

            this.btnAddUser.Click += new System.EventHandler(this.btnSaveUserAccount_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        }

        private void SetupFormText(bool isEditMode)
        {
            if (isEditMode)
            {
                this.Text = "Edit User Account";
                this.label1.Text = "Edit User Account";
                this.btnAddUser.Text = "Update User";
                // txtPassword.PlaceholderText = "Enter new password (optional)"; // REMOVED
                // You might want a label or tooltip to indicate password is for change only
            }
            else
            {
                this.Text = "Add User Account";
                this.label1.Text = "Add User Account";
                this.btnAddUser.Text = "Add User";
            }
        }

        private void LoadRolesIntoComboBox()
        {
            try
            {
                List<UserRole> roles = _roleRepository.GetRoles();
                cmbAccountRole.DataSource = roles;
                cmbAccountRole.DisplayMember = "RoleName";
                cmbAccountRole.ValueMember = "Id";
                if (roles.Any())
                {
                    cmbAccountRole.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading roles: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUserDataForEdit()
        {
            if (_editingUserAccount == null) return;

            txtFullName.Text = _editingUserAccount.FullName;
            txtUserName.Text = _editingUserAccount.Username;
            txtUserName.ReadOnly = true;
            txtEmail.Text = _editingUserAccount.Email;
            txtPhoneNumber.Text = _editingUserAccount.PhoneNumber;

            if (cmbAccountRole.Items.Count > 0 && _editingUserAccount.Role != null)
            {
                cmbAccountRole.SelectedValue = _editingUserAccount.RoleId;
            }
            else if (cmbAccountRole.Items.Count > 0)
            {
                cmbAccountRole.SelectedIndex = -1; // Or some default
            }
            // Password fields are intentionally left blank.
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnSaveUserAccount_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUserName.Focus();
                return;
            }
            if (cmbAccountRole.SelectedValue == null)
            {
                MessageBox.Show("Please select an account role.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbAccountRole.Focus();
                return;
            }

            string plainPassword = txtPassword.Text;
            // Assuming you have a txtConfirmPassword in your designer. If not, you should add one.
            // string confirmPassword = txtConfirmPassword.Text;

            if (_editingUserAccount == null) // ADD Mode
            {
                if (string.IsNullOrWhiteSpace(plainPassword))
                {
                    MessageBox.Show("Password cannot be empty for new users.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }
                // if (plainPassword != confirmPassword)
                // {
                //     MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //     // txtConfirmPassword.Focus();
                //     return;
                // }
            }
            // else if (!string.IsNullOrWhiteSpace(plainPassword) && plainPassword != confirmPassword) // EDIT Mode - if password IS provided, it must match confirm
            // {
            //     MessageBox.Show("New passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //     // txtConfirmPassword.Focus();
            //     return;
            // }

            try
            {
                bool success = false;
                if (_editingUserAccount == null) // ADD Mode
                {
                    UserAccount newUser = new UserAccount
                    {
                        FullName = txtFullName.Text.Trim(),
                        Username = txtUserName.Text.Trim(),
                        // PasswordHash will be set by the repository
                        Email = txtEmail.Text.Trim(),
                        PhoneNumber = txtPhoneNumber.Text.Trim(),
                        RoleId = (int)cmbAccountRole.SelectedValue,
                        IsActive = true,
                        DateCreated = DateTime.Now
                    };

                    // Pass the plain password to the repository method
                    int result = _userAccountRepository.AddUserAccount(newUser, plainPassword);
                    if (result > 0)
                    {
                        MessageBox.Show("User account added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        success = true;
                    }
                    else if (result == -1) // Username exists
                    {
                        MessageBox.Show("This username already exists. Please choose a different username.", "Duplicate Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtUserName.Focus();
                        txtUserName.SelectAll();
                    }
                    else
                    {
                        MessageBox.Show("Failed to add user account.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else // EDIT Mode
                {
                    _editingUserAccount.FullName = txtFullName.Text.Trim();
                    _editingUserAccount.Email = txtEmail.Text.Trim();
                    _editingUserAccount.PhoneNumber = txtPhoneNumber.Text.Trim();
                    _editingUserAccount.RoleId = (int)cmbAccountRole.SelectedValue;
                    // _editingUserAccount.IsActive = chkIsActive.Checked; // If you add a checkbox

                    string passwordToUpdate = null;
                    if (!string.IsNullOrWhiteSpace(plainPassword)) // Only if new password entered
                    {
                        passwordToUpdate = plainPassword;
                    }

                    if (_userAccountRepository.UpdateUserAccount(_editingUserAccount, passwordToUpdate))
                    {
                        MessageBox.Show("User account updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        success = true;
                    }
                    else
                    {
                        MessageBox.Show("Failed to update user account. The username might conflict if changed (though not editable here), or an error occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (success)
                {
                    UserAccountSaved?.Invoke(this, EventArgs.Empty);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
