using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Models.ViewModels;
using Newtonsoft.Json;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using System.Text.RegularExpressions;

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class AddNotificationRecipients : Form
    {
        // Repositories
        private readonly NotificationRuleRepository _notificationRuleRepo;
        private readonly UserRoleRepository _userRoleRepo;
        private readonly UserAccountRepository _userAccountRepo;
        private readonly CustomerRepository _customerRepo;
        private readonly TagRepository _tagRepository;

        // The single source of truth for items displayed in the grid
        private readonly BindingList<NotificationRecipientDisplayItem> _displayItemsSource = new BindingList<NotificationRecipientDisplayItem>();

        private readonly int _ruleId;
        private readonly string _ruleName;
        private OverlayForm _overlayForm;

        public AddNotificationRecipients(int ruleId, string ruleName)
        {
            InitializeComponent();
            _ruleId = ruleId;
            _ruleName = ruleName;

            _notificationRuleRepo = new NotificationRuleRepository();
            _userRoleRepo = new UserRoleRepository();
            _userAccountRepo = new UserAccountRepository();
            _customerRepo = new CustomerRepository();
            _tagRepository = new TagRepository();

            this.Text = $"Edit Recipients for: {_ruleName}";
            if (lblTitle != null) lblTitle.Text = $"Recipients for: {_ruleName}";
            if (btnsaveRecipients != null) btnsaveRecipients.Text = "Save Recipients";
        }

        private void AddNotificationRecipients_Load(object sender, EventArgs e)
        {
            InitializeControlsAndLoadData();
        }

        private void InitializeControlsAndLoadData()
        {
            // Set up grid first
            SetupDgvRecipientsColumns();

            // Load any previously saved recipients
            LoadExistingRecipients();

            // Wire up all events last, after initial state is set
            WireUpEvents(true);
        }

        private void WireUpEvents(bool subscribe)
        {
            // Temporarily detach all handlers to prevent unwanted event cascades
            chkUserRolesUserType.CheckedChanged -= RecipientType_CheckedChanged;
            chkSpecificUsersUserType.CheckedChanged -= RecipientType_CheckedChanged;
            chkSpecificCustomers.CheckedChanged -= RecipientType_CheckedChanged;
            chkTags.CheckedChanged -= RecipientType_CheckedChanged;
            chkAddEmailUserType.CheckedChanged -= RecipientType_CheckedChanged;
            chkAddPhoneUserType.CheckedChanged -= RecipientType_CheckedChanged;
            textSearchRecipients.TextChanged -= FilterAndBindGrid;
            dgvUsers.CurrentCellDirtyStateChanged -= DgvUsers_CurrentCellDirtyStateChanged;
            if (btnsaveRecipients != null) btnsaveRecipients.Click -= BtnsaveRecipients_Click;
            if (btnCancel != null) btnCancel.Click -= (s, e) => this.Close();

            if (subscribe)
            {
                chkUserRolesUserType.CheckedChanged += RecipientType_CheckedChanged;
                chkSpecificUsersUserType.CheckedChanged += RecipientType_CheckedChanged;
                chkSpecificCustomers.CheckedChanged += RecipientType_CheckedChanged;
                chkTags.CheckedChanged += RecipientType_CheckedChanged;
                chkAddEmailUserType.CheckedChanged += RecipientType_CheckedChanged;
                chkAddPhoneUserType.CheckedChanged += RecipientType_CheckedChanged;
                textSearchRecipients.TextChanged += FilterAndBindGrid;
                dgvUsers.CurrentCellDirtyStateChanged += DgvUsers_CurrentCellDirtyStateChanged;
                if (btnsaveRecipients != null) btnsaveRecipients.Click += BtnsaveRecipients_Click;
                if (btnCancel != null) btnCancel.Click += (s, e) => this.Close();
            }
        }

        private void DgvUsers_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvUsers.IsCurrentCellDirty)
            {
                dgvUsers.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void LoadExistingRecipients()
        {
            _displayItemsSource.Clear();
            var rule = _notificationRuleRepo.GetRuleById(_ruleId);
            if (rule != null && !string.IsNullOrWhiteSpace(rule.RecipientDetailsJson))
            {
                try
                {
                    var recipientData = JsonConvert.DeserializeObject<NotificationRecipientData>(rule.RecipientDetailsJson);
                    if (recipientData != null)
                    {
                        // Load saved recipients and mark them as selected
                        recipientData.RoleIds?.ForEach(id => { var r = _userRoleRepo.GetRoleByIdWithPermissions(id); if (r != null) AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Role", Identifier = r.RoleName, Details = r.Description, OriginalSource = id, SourceListKey = "Role" }); });
                        recipientData.UserAccountIds?.ForEach(id => { var u = _userAccountRepo.GetUserAccountById(id); if (u != null) AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "User", Identifier = u.Username, Details = u.FullName, OriginalSource = id, SourceListKey = "User" }); });
                        recipientData.CustomerIdentifiers?.ForEach(idStr => { Customer c = null; if (int.TryParse(idStr, out int id)) c = _customerRepo.GetCustomerById(id); AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Customer", Identifier = c?.AccountName ?? $"ID: {idStr}", Details = c?.Location, OriginalSource = idStr, SourceListKey = "Customer" }); });
                        recipientData.TagIds?.ForEach(id => { var t = _tagRepository.GetTagById(id); if (t != null) AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Tag", Identifier = t.TagName, Details = t.TagDescription, OriginalSource = id, SourceListKey = "Tag" }); });
                        recipientData.AdditionalEmails?.ForEach(email => AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Email", Identifier = email, Details = "Manual Entry", OriginalSource = email, SourceListKey = "Email" }));
                        recipientData.AdditionalPhones?.ForEach(phone => AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Phone", Identifier = phone, Details = "Manual Entry", OriginalSource = phone, SourceListKey = "Phone" }));
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Error deserializing RecipientDetailsJson: {ex.Message}"); }
            }
            FilterAndBindGrid();
            UpdateCheckboxesFromGridState();
        }

        private void SetupDgvRecipientsColumns()
        {
            dgvUsers.Columns.Clear();
            dgvUsers.AutoGenerateColumns = false;
            dgvUsers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvUsers.RowTemplate.Height = 30;

            dgvUsers.Columns.Add(new DataGridViewCheckBoxColumn { Name = "SelectCol", DataPropertyName = "IsSelected", HeaderText = "Select", Width = 60 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecipientTypeCol", DataPropertyName = "RecipientType", HeaderText = "Type", Width = 80 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdentifierCol", DataPropertyName = "Identifier", HeaderText = "Identifier/Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "DetailsCol", DataPropertyName = "Details", HeaderText = "Details", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 70, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });

            dgvUsers.ReadOnly = false;
            foreach (DataGridViewColumn col in dgvUsers.Columns) { if (col.Name != "SelectCol") col.ReadOnly = true; }
        }

        private void RecipientType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCheckBox = sender as CheckBox;
            if (activeCheckBox == null) return;

            WireUpEvents(false); // Disable events to prevent messy repeated firings

            bool isEmailOrPhone = activeCheckBox == chkAddEmailUserType || activeCheckBox == chkAddPhoneUserType;

            if (activeCheckBox.Checked)
            {
                // This block handles checking a previously unchecked box.
                bool isModalLauncher = activeCheckBox == chkSpecificCustomers || activeCheckBox == chkTags || isEmailOrPhone;
                if (isModalLauncher)
                {
                    ShowOverlay();
                    OpenModal(activeCheckBox);
                    CloseOverlay();
                }
                else if (activeCheckBox == chkUserRolesUserType)
                {
                    _userRoleRepo.GetRoles().ForEach(role => AddItemToDisplaySource(new NotificationRecipientDisplayItem { RecipientType = "Role", Identifier = role.RoleName, Details = role.Description, OriginalSource = role.Id, SourceListKey = "Role" }));
                }
                else if (activeCheckBox == chkSpecificUsersUserType)
                {
                    _userAccountRepo.GetUserAccountsWithRoles().Where(u => u.IsActive).ToList().ForEach(user => AddItemToDisplaySource(new NotificationRecipientDisplayItem { RecipientType = "User", Identifier = user.Username, Details = user.FullName, OriginalSource = user.Id, SourceListKey = "User" }));
                }
            }
            else // This block handles unchecking a box.
            {
                if (isEmailOrPhone)
                {
                    // DESIRED BEHAVIOR: If the user unchecks an Email or Phone box,
                    // treat it as an EDIT action. Re-check the box and open the modal.
                    activeCheckBox.Checked = true; // Force the box to stay checked visually.
                    ShowOverlay();
                    OpenModal(activeCheckBox); // Open the modal to allow editing.
                    CloseOverlay();
                }
                else
                {
                    // Original behavior for other checkboxes (Customers, Tags, etc.)
                    RemoveItemsFromDisplaySource(GetSourceKeyFromCheckbox(activeCheckBox));
                }
            }

            FilterAndBindGrid();
            UpdateCheckboxesFromGridState(); // Sync all checkboxes after the operation
            WireUpEvents(true);
        }

        private void OpenModal(CheckBox checkBox)
        {
            if (checkBox == chkSpecificCustomers)
            {
                var currentIds = _displayItemsSource.Where(i => i.SourceListKey == "Customer").Select(i => Convert.ToInt32(i.OriginalSource)).ToList();
                using (var modal = new SelectRecipientSpecificCustomers(currentIds))
                {
                    modal.StartPosition = FormStartPosition.CenterScreen;
                    if (modal.ShowDialog(this) == DialogResult.OK) { RemoveItemsFromDisplaySource("Customer"); modal.SelectedCustomerIds.ForEach(id => { var c = _customerRepo.GetCustomerById(id); if (c != null) AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Customer", Identifier = c.AccountName, Details = c.Location, OriginalSource = id.ToString(), SourceListKey = "Customer" }); }); } }
            }
            else if (checkBox == chkTags)
            {
                var currentIds = _displayItemsSource.Where(i => i.SourceListKey == "Tag").Select(i => Convert.ToInt32(i.OriginalSource)).ToList();
                using (var modal = new SelectRecipientTags(currentIds))
                { 
                    modal.StartPosition = FormStartPosition.CenterScreen;
                    if (modal.ShowDialog(this) == DialogResult.OK) { RemoveItemsFromDisplaySource("Tag"); modal.SelectedTagIds.ForEach(id => { var t = _tagRepository.GetTagById(id); if (t != null) AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Tag", Identifier = t.TagName, Details = t.TagDescription, OriginalSource = id, SourceListKey = "Tag" }); }); } }
            }
            else if (checkBox == chkAddEmailUserType)
            {
                var currentContacts = _displayItemsSource.Where(i => i.SourceListKey == "Email").Select(i => i.OriginalSource.ToString()).ToList();
                using (var modal = new SelectRecipientAddEmailOrPhone(true, currentContacts))
                {
                    modal.StartPosition = FormStartPosition.CenterScreen;
                    if (modal.ShowDialog(this) == DialogResult.OK) { RemoveItemsFromDisplaySource("Email"); modal.GetContacts().ForEach(c => AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Email", Identifier = c, Details = "Manual Entry", OriginalSource = c, SourceListKey = "Email" })); } }
            }
            else if (checkBox == chkAddPhoneUserType)
            {
                var currentContacts = _displayItemsSource.Where(i => i.SourceListKey == "Phone").Select(i => i.OriginalSource.ToString()).ToList();
                using (var modal = new SelectRecipientAddEmailOrPhone(false, currentContacts))
                {
                    modal.StartPosition = FormStartPosition.CenterScreen;
                    if (modal.ShowDialog(this) == DialogResult.OK) { RemoveItemsFromDisplaySource("Phone"); modal.GetContacts().ForEach(c => AddItemToDisplaySource(new NotificationRecipientDisplayItem { IsSelected = true, RecipientType = "Phone", Identifier = c, Details = "Manual Entry", OriginalSource = c, SourceListKey = "Phone" })); } }
            }
        }

        #region Helper and Save Methods
        private void AddItemToDisplaySource(NotificationRecipientDisplayItem newItem)
        {
            bool exists = _displayItemsSource.Any(item => item.SourceListKey == newItem.SourceListKey && item.OriginalSource.Equals(newItem.OriginalSource));
            if (!exists) _displayItemsSource.Add(newItem);
        }
        private void RemoveItemsFromDisplaySource(string sourceListKey)
        {
            if (string.IsNullOrEmpty(sourceListKey)) return;
            var itemsToRemove = _displayItemsSource.Where(item => item.SourceListKey == sourceListKey).ToList();
            foreach (var item in itemsToRemove) _displayItemsSource.Remove(item);
        }
        private void FilterAndBindGrid(object sender = null, EventArgs e = null)
        {
            IEnumerable<NotificationRecipientDisplayItem> filteredList = _displayItemsSource;
            string searchText = textSearchRecipients.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredList = filteredList.Where(item =>
                    (item.RecipientType?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (item.Identifier?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (item.Details?.ToLowerInvariant().Contains(searchText) ?? false)
                );
            }
            dgvUsers.DataSource = null;
            if (filteredList.Any()) dgvUsers.DataSource = filteredList.ToList();
        }
        private void UpdateCheckboxesFromGridState()
        {
            WireUpEvents(false);
            chkUserRolesUserType.Checked = _displayItemsSource.Any(i => i.RecipientType == "Role");
            chkSpecificUsersUserType.Checked = _displayItemsSource.Any(i => i.RecipientType == "User");
            chkSpecificCustomers.Checked = _displayItemsSource.Any(i => i.RecipientType == "Customer");
            chkTags.Checked = _displayItemsSource.Any(i => i.RecipientType == "Tag");
            chkAddEmailUserType.Checked = _displayItemsSource.Any(i => i.RecipientType == "Email");
            chkAddPhoneUserType.Checked = _displayItemsSource.Any(i => i.RecipientType == "Phone");
            WireUpEvents(true);
        }
        private string GetSourceKeyFromCheckbox(CheckBox chk)
        {
            if (chk == chkUserRolesUserType) return "Role";
            if (chk == chkSpecificUsersUserType) return "User";
            if (chk == chkSpecificCustomers) return "Customer";
            if (chk == chkTags) return "Tag";
            if (chk == chkAddEmailUserType) return "Email";
            if (chk == chkAddPhoneUserType) return "Phone";
            return string.Empty;
        }

        private void BtnsaveRecipients_Click(object sender, EventArgs e)
        {
            var dataToSave = new NotificationRecipientData();
            var selectedItems = _displayItemsSource.Where(item => item.IsSelected).ToList();

            if (!selectedItems.Any())
            {
                MessageBox.Show("Please select at least one recipient by checking the box next to their name.", "No Recipients Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dataToSave.RoleIds = selectedItems.Where(i => i.SourceListKey == "Role").Select(i => (int)i.OriginalSource).ToList();
            dataToSave.UserAccountIds = selectedItems.Where(i => i.SourceListKey == "User").Select(i => (int)i.OriginalSource).ToList();
            dataToSave.CustomerIdentifiers = selectedItems.Where(i => i.SourceListKey == "Customer").Select(i => i.OriginalSource.ToString()).ToList();
            dataToSave.TagIds = selectedItems.Where(i => i.SourceListKey == "Tag").Select(i => (int)i.OriginalSource).ToList();
            dataToSave.AdditionalEmails = selectedItems.Where(i => i.SourceListKey == "Email").Select(i => i.OriginalSource.ToString()).ToList();
            dataToSave.AdditionalPhones = selectedItems.Where(i => i.SourceListKey == "Phone").Select(i => i.OriginalSource.ToString()).ToList();

            try
            {
                string recipientsJson = JsonConvert.SerializeObject(dataToSave);
                _notificationRuleRepo.UpdateNotificationRuleRecipients(_ruleId, recipientsJson);
                _notificationRuleRepo.ResetRuleSchedule(_ruleId);
                MessageBox.Show("Recipients saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Error saving recipients: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnCancel_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
        private void ShowOverlay() { if (_overlayForm == null || _overlayForm.IsDisposed) _overlayForm = new OverlayForm { Owner = this }; _overlayForm.Show(); }
        private void CloseOverlay() { _overlayForm?.Close(); _overlayForm = null; }
        private System.Windows.Forms.Label lblGridInstructions => this.Controls.Find("lblGridInstructions", true).FirstOrDefault() as Label;
        #endregion
    }
}
