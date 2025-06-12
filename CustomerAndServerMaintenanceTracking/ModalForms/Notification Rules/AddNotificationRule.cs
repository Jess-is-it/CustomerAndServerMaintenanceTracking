using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Configuration;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using Newtonsoft.Json;
using CustomerAndServerMaintenanceTracking.Models;
using CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules;
using System.Data.SqlClient;
using CustomerAndServerMaintenanceTracking.Services;
namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class AddNotificationRule : Form
    {
        // Context from the calling form
        private string _sourceFeature;
        private string _sourceEntityName;
        private int _sourceEntityId;

        // Repositories
        private NetwatchConfigRepository _netwatchConfigRepository;
        private ServiceLogRepository _logRepository;
        private TagRepository _tagRepository;
        private NotificationRuleRepository _notificationRuleRepo; // << DECLARED HERE

        // Data storage for the rule being built
        private string _notificationRuleNameOrDescription;
        private List<string> _selectedNotificationTypes = new List<string>();

        // Trigger Condition Data
        private List<int> _selectedNetwatchConfigIds = new List<int>();
        private bool _triggerOnAllIPs;
        private List<string> _selectedSpecificIPs = new List<string>();
        private List<string> _selectedNetwatchStatuses = new List<string>();

        private OverlayForm _overlayForm;
        private NotificationRule _editingRule = null; // To store the rule being edited

        // Constructor for ADD mode (from NetwatchList or other source)
        public AddNotificationRule(string sourceEntityName, int sourceEntityId, string sourceFeature)
        {
            InitializeComponent();

            _sourceEntityName = sourceEntityName;
            _sourceEntityId = sourceEntityId;
            _sourceFeature = sourceFeature;

            // Initialize Repositories
            _logRepository = new ServiceLogRepository();
            _tagRepository = new TagRepository(); // Assuming it has a parameterless constructor
            _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, _tagRepository);
            _notificationRuleRepo = new NotificationRuleRepository(_logRepository); // << INITIALIZED HERE

            SetupFormBasedOnContext(isEditMode: false);
            InitializeButtonEventHandlers();
        }

        // NEW Constructor for EDIT mode (from NotificationManagerForm)
        public AddNotificationRule(int ruleIdToEdit)
        {
            InitializeComponent();

            _logRepository = new ServiceLogRepository();
            _tagRepository = new TagRepository();
            _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, _tagRepository);
            _notificationRuleRepo = new NotificationRuleRepository(_logRepository); // << INITIALIZED HERE

            InitializeButtonEventHandlers();

            _editingRule = _notificationRuleRepo.GetRuleById(ruleIdToEdit);

            if (_editingRule == null)
            {
                MessageBox.Show("Error: Could not load the notification rule for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Close form if rule not found, after it's shown, to avoid issues
                this.Load += (s, e) => {
                    if (_editingRule == null) this.Close();
                };
                return;
            }

            // Populate form based on _editingRule
            _sourceFeature = _editingRule.SourceFeature;
            _sourceEntityId = _editingRule.SourceEntityId ?? 0;
            _sourceEntityName = _editingRule.RuleName; // Or fetch original if different

            SetupFormBasedOnContext(isEditMode: true); // Pass edit mode flag
        }


        private void AddNotificationRule_Load(object sender, EventArgs e)
        {
            if (_editingRule == null && this.Modal && this.DialogResult == DialogResult.None && !this.IsDisposed)
            {
                // This handles the case where loading in EDIT mode failed in constructor.
                // We check Modal and DialogResult to ensure we only close if it was meant to be shown.
                // And IsDisposed to prevent issues if Load is called after Dispose.
            }
        }

        private void InitializeButtonEventHandlers()
        {
            if (btnSaveNotification != null)
            {
                btnSaveNotification.Click -= btnSaveInitialNotificationRule_Click;
                btnSaveNotification.Click += btnSaveInitialNotificationRule_Click;
            }
            if (btnCancelDetailsandType != null)
            {
                btnCancelDetailsandType.Click -= btnCancelForm_Click;
                btnCancelDetailsandType.Click += btnCancelForm_Click;
            }
        }

        private void SetupFormBasedOnContext(bool isEditMode)
        {
            if (isEditMode && _editingRule != null)
            {
                // Editing an existing rule
                if (lblTitle != null) lblTitle.Text = $"Edit Rule: {_editingRule.RuleName}";
                if (btnSaveNotification != null) btnSaveNotification.Text = "Update Rule";
                if (txtNotificationName != null) txtNotificationName.Text = _editingRule.RuleName;

                _selectedNotificationTypes.Clear(); // Ensure it's clear before populating
                if (!string.IsNullOrWhiteSpace(_editingRule.NotificationChannelsJson))
                {
                    try
                    {
                        var channels = JsonConvert.DeserializeObject<List<string>>(_editingRule.NotificationChannelsJson);
                        if (channels != null) _selectedNotificationTypes.AddRange(channels);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing NotificationChannelsJson for RuleId {_editingRule.Id} during edit: {ex.Message}");
                        // Optionally inform the user or default to no channels selected
                    }
                }
                // Set checkboxes based on loaded _selectedNotificationTypes
                chkEmailNotifType.Checked = _selectedNotificationTypes.Contains("Email");
                chkSMSNotifType.Checked = _selectedNotificationTypes.Contains("SMS");
                chkTelegramNotifType.Checked = _selectedNotificationTypes.Contains("Telegram");
                chkFbMessengerNotifType.Checked = _selectedNotificationTypes.Contains("FacebookMessenger");
            }
            else // Adding a new rule
            {
                if (lblTitle != null) lblTitle.Text = $"New Notification Rule for: {_sourceEntityName}";
                if (btnSaveNotification != null) btnSaveNotification.Text = "Save Rule";
                if (txtNotificationName != null) txtNotificationName.Text = $"Alert for {(_sourceEntityName ?? "Selected Item")}";

                // Set default notification types for a new rule if desired
                _selectedNotificationTypes.Clear();
                chkEmailNotifType.Checked = true; // Example: Default to Email
                                                  // _selectedNotificationTypes.Add("Email"); // Reflect this in the list immediately if needed elsewhere before save
            }

            // Load the Trigger Condition UserControl
            if (panelTriggerConDisplay != null)
            {
                panelTriggerConDisplay.Controls.Clear();
                if (_sourceFeature == "Netwatch")
                {
                    // The TriggerEventNetwatch UC needs the NetwatchConfigID (_sourceEntityId for new, _editingRule.SourceEntityId for edit)
                    // to know which Netwatch config it should load details for (e.g., for IP selection)
                    int contextNetwatchConfigId = isEditMode ? (_editingRule?.SourceEntityId ?? 0) : _sourceEntityId;

                    TriggerEventNetwatch netwatchTriggerUc = new TriggerEventNetwatch(contextNetwatchConfigId);
                    netwatchTriggerUc.Dock = DockStyle.Fill;

                    // --- THIS IS THE CORRECT PLACE TO LOAD STATE FOR EDIT MODE ---
                    if (isEditMode && _editingRule != null && !string.IsNullOrWhiteSpace(_editingRule.TriggerDetailsJson))
                    {
                        Console.WriteLine($"Attempting to load TriggerDetailsJson for RuleId {_editingRule.Id} into TriggerEventNetwatch UC."); // Debug log
                        netwatchTriggerUc.LoadStateFromJson(_editingRule.TriggerDetailsJson);
                    }
                    // -------------------------------------------------------------

                    panelTriggerConDisplay.Controls.Add(netwatchTriggerUc);
                }
                // else if (_sourceFeature == "AnotherFeature") { /* Load other trigger UCs */ }
            }
            else
            {
                Console.WriteLine("Error: panelTriggerConDisplay not found on AddNotificationRule form during SetupFormBasedOnContext.");
            }
        }

        private void btnSaveInitialNotificationRule_Click(object sender, EventArgs e)
        {
            _notificationRuleNameOrDescription = txtNotificationName.Text.Trim();
            _selectedNotificationTypes.Clear();
            if (chkEmailNotifType.Checked) _selectedNotificationTypes.Add("Email");
            if (chkSMSNotifType.Checked) _selectedNotificationTypes.Add("SMS");
            if (chkTelegramNotifType.Checked) _selectedNotificationTypes.Add("Telegram");
            if (chkFbMessengerNotifType.Checked) _selectedNotificationTypes.Add("FacebookMessenger");

            if (string.IsNullOrWhiteSpace(_notificationRuleNameOrDescription))
            {
                MessageBox.Show("Please provide a name or description for this notification rule.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNotificationName.Focus();
                return;
            }
            if (!_selectedNotificationTypes.Any())
            {
                MessageBox.Show("Please select at least one notification type (Email, SMS, etc.).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string triggerDetailsJson = "{}"; // Default empty JSON object
            if (panelTriggerConDisplay.Controls.Count > 0 &&
                panelTriggerConDisplay.Controls[0] is TriggerEventNetwatch netwatchTriggerUc)
            {
                _selectedNetwatchConfigIds = netwatchTriggerUc.GetSelectedNetwatchConfigIds();
                _triggerOnAllIPs = netwatchTriggerUc.IsAllIpsSelected();
                _selectedSpecificIPs = netwatchTriggerUc.GetSelectedIpAddresses();
                _selectedNetwatchStatuses = netwatchTriggerUc.GetSelectedStatuses();

                if (_sourceFeature == "Netwatch")
                {
                    if ((_selectedNetwatchConfigIds == null || !_selectedNetwatchConfigIds.Any()) && _sourceEntityId <= 0) // if launched generically for netwatch, at least one config must be chosen in UC
                    {
                        MessageBox.Show("Please select at least one Netwatch configuration to monitor from the trigger settings.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!_selectedNetwatchConfigIds.Any() && _sourceEntityId > 0) // If launched for a specific config, ensure it's implicitly part of the trigger.
                    {
                        _selectedNetwatchConfigIds.Add(_sourceEntityId); // Add the context NetwatchConfigId
                    }
                    if (!_triggerOnAllIPs && (_selectedSpecificIPs == null || !_selectedSpecificIPs.Any()))
                    {
                        MessageBox.Show("You've selected to monitor specific IPs, but no IPs are selected. Please select at least one IP or choose 'All IPs'.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (_selectedNetwatchStatuses == null || !_selectedNetwatchStatuses.Any())
                    {
                        MessageBox.Show("Please select at least one Netwatch status to trigger the notification.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    NetwatchTriggerDetails trigger = new NetwatchTriggerDetails
                    {
                        SelectedNetwatchConfigIds = _selectedNetwatchConfigIds,
                        TriggerOnAllIPs = _triggerOnAllIPs,
                        SelectedSpecificIPs = _selectedSpecificIPs,
                        SelectedNetwatchStatuses = _selectedNetwatchStatuses
                    };
                    triggerDetailsJson = JsonConvert.SerializeObject(trigger);
                }
            }
            else if (_sourceFeature == "Netwatch") // If it's a Netwatch rule but UC is missing
            {
                MessageBox.Show("Trigger condition control not found. Cannot save Netwatch rule.", "Internal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string channelsJson = JsonConvert.SerializeObject(_selectedNotificationTypes);
            bool success = false;

            try
            {
                if (_editingRule == null) // ADD Mode
                {
                    NotificationRule initialRule = new NotificationRule
                    {
                        RuleName = _notificationRuleNameOrDescription,
                        Description = _notificationRuleNameOrDescription,
                        NotificationChannelsJson = channelsJson,
                        SourceFeature = _sourceFeature,
                        SourceEntityId = _sourceEntityId, // This ID is from the context it was launched (e.g. specific NetwatchConfigId)
                        TriggerDetailsJson = triggerDetailsJson,
                        IsEnabled = true,
                        DateCreated = DateTime.Now,
                        RunCount = 0,
                        // ContentDetailsJson, RecipientDetailsJson, ScheduleDetailsJson will be null initially
                    };
                    int newRuleId = _notificationRuleRepo.AddInitialRule(initialRule);
                    if (newRuleId > 0)
                    {
                        MessageBox.Show($"Initial Notification Rule '{initialRule.RuleName}' saved with ID: {newRuleId}!\nYou can now manage its Content, Recipients, and Schedule from the Notification Manager.", "Rule Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        success = true;
                    }
                    else
                    {
                        MessageBox.Show("Failed to save new notification rule.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else // EDIT Mode
                {
                    _editingRule.RuleName = _notificationRuleNameOrDescription;
                    _editingRule.Description = _notificationRuleNameOrDescription;
                    _editingRule.NotificationChannelsJson = channelsJson;
                    // SourceFeature and SourceEntityId generally shouldn't change for an existing rule's context
                    // If they can, ensure UI allows this and it's handled here.
                    _editingRule.TriggerDetailsJson = triggerDetailsJson;
                    _editingRule.LastModified = DateTime.Now;

                    if (_notificationRuleRepo.UpdateInitialRuleDetails(_editingRule))
                    {
                        _notificationRuleRepo.ResetRuleSchedule(_editingRule.Id);
                        MessageBox.Show($"Notification Rule '{_editingRule.RuleName}' details updated!", "Rule Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        success = true;
                    }
                    else
                    {
                        MessageBox.Show("Failed to update notification rule details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (success)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving notification rule: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelForm_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

    }
}


      
