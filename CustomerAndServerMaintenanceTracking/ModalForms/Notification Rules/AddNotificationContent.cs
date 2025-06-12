using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Services;
using Newtonsoft.Json;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class AddNotificationContent : Form
    {
        private readonly int _ruleId;
        private readonly string _ruleName; // For title
        private List<string> _selectedNotificationTypes = new List<string>();
        private readonly string _sourceFeature;
        private readonly string _sourceEntityName; // e.g., Name of the Netwatch config

        private NotificationRuleRepository _notificationRuleRepo;
        // private NotificationTemplateProvider _templateProvider; // Not needed if static

        // Fields to hold current content being edited
        private string _currentSmsContent;
        private string _currentEmailSubject;
        private string _currentEmailBody;
        private string _currentTelegramContent;
        private string _currentFbMessengerMessageContent;

        public AddNotificationContent(int ruleId, string ruleName, List<string> selectedNotificationTypes, string sourceFeature, string sourceEntityName)
        {
            InitializeComponent();

            _ruleId = ruleId;
            _ruleName = ruleName;
            _selectedNotificationTypes = selectedNotificationTypes ?? new List<string>();
            _sourceFeature = sourceFeature;
            _sourceEntityName = sourceEntityName;

            _notificationRuleRepo = new NotificationRuleRepository();

            this.Text = $"Edit Content for: {_ruleName}";
            if (lblTitle != null) lblTitle.Text = $"Edit Content for: {_ruleName}";

            // Ensure button names match your designer. Assuming "btnSaveContent" and "btnCancel"
            Button saveButton = this.Controls.Find("btnSaveContent", true).FirstOrDefault() as Button;
            if (saveButton == null) // Fallback if still named btnsaveRecipients
            {
                saveButton = this.btnsaveRecipients;
                if (saveButton != null) saveButton.Text = "Save Content";
            }
            if (saveButton != null)
            {
                saveButton.Click -= BtnSaveContent_Click; // Prevent multiple subscriptions
                saveButton.Click += BtnSaveContent_Click;
            }

            if (this.btnCancel != null)
            {
                this.btnCancel.Click -= BtnCancel_Click; // Prevent multiple
                this.btnCancel.Click += BtnCancel_Click; // Use named handler
            }
            if (this.btnInserTokens != null)
            {
                this.btnInserTokens.Click -= btnInserTokens_Click; // Prevent multiple
                this.btnInserTokens.Click += btnInserTokens_Click;
            }
        }

        private void AddNotificationContent_Load(object sender, EventArgs e)
        {
            PopulateAvailableTokens();
            UpdateContentTabVisibility(); // Show/hide SMS, Email, etc. tabs
            LoadExistingOrSetDefaultContent();
        }

        private void PopulateAvailableTokens()
        {
            if (listBoxToken == null) return;
            listBoxToken.Items.Clear();
            List<string> tokens = NotificationTemplateProvider.GetAvailableTokens(_sourceFeature, _sourceEntityName);
            foreach (string token in tokens)
            {
                listBoxToken.Items.Add(token);
            }
            if (listBoxToken.Items.Count > 0) listBoxToken.SelectedIndex = 0;

        }

        private void UpdateContentTabVisibility()
        {
            if (NotifContentTabs == null)
            {
                Console.WriteLine("CRITICAL ERROR: NotifContentTabs control is null in AddNotificationContent.");
                return;
            }
            if (_selectedNotificationTypes == null)
            {
                _selectedNotificationTypes = new List<string>();
            }

            Console.WriteLine($"AddNotificationContent - UpdateContentTabVisibility - Selected Types: {string.Join(", ", _selectedNotificationTypes)}");

            TabPage previouslySelectedSubTab = NotifContentTabs.SelectedTab;
            NotifContentTabs.TabPages.Clear();

            // Add back tabs based on _selectedNotificationTypes, in a defined order
            if (_selectedNotificationTypes.Contains("Email"))
            {
                if (tabPageEmail != null) NotifContentTabs.TabPages.Add(tabPageEmail);
                else Console.WriteLine("Error: AddNotificationContent.tabPageEmail member is null.");
            }
            if (_selectedNotificationTypes.Contains("SMS"))
            {
                if (tabPageSMS != null) NotifContentTabs.TabPages.Add(tabPageSMS);
                else Console.WriteLine("Error: AddNotificationContent.tabPageSMS member is null.");
            }
            if (_selectedNotificationTypes.Contains("Telegram"))
            {
                if (tabPageTelegram != null) NotifContentTabs.TabPages.Add(tabPageTelegram);
                else Console.WriteLine("Error: AddNotificationContent.tabPageTelegram member is null.");
            }
            if (_selectedNotificationTypes.Contains("FacebookMessenger"))
            {
                if (tabPageFbMessenger != null) NotifContentTabs.TabPages.Add(tabPageFbMessenger);
                else Console.WriteLine("Error: AddNotificationContent.tabPageFbMessenger member is null.");
            }

            Console.WriteLine($"AddNotificationContent - NotifContentTabs Page Count AFTER Add: {NotifContentTabs.TabPages.Count}");

            if (NotifContentTabs.TabPages.Count > 0)
            {
                if (previouslySelectedSubTab != null && NotifContentTabs.TabPages.Contains(previouslySelectedSubTab))
                {
                    NotifContentTabs.SelectedTab = previouslySelectedSubTab;
                }
                else
                {
                    NotifContentTabs.SelectedIndex = 0;
                }
            }
            else
            {
                Button saveButton = Controls.Find("btnSaveContent", true).FirstOrDefault() as Button ?? btnsaveRecipients;
                if (saveButton != null) saveButton.Enabled = false;
            }
        }

        private void LoadExistingOrSetDefaultContent()
        {
            NotificationRule rule = _notificationRuleRepo.GetRuleById(_ruleId);
            NotificationContentData contentData = null;

            if (rule != null && !string.IsNullOrWhiteSpace(rule.ContentDetailsJson) && rule.ContentDetailsJson != "{}")
            {
                try
                {
                    contentData = JsonConvert.DeserializeObject<NotificationContentData>(rule.ContentDetailsJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing ContentDetailsJson for rule ID {_ruleId}: {ex.Message}");
                }
            }
            // Corrected: Ensure contentData is not null for safe access
            if (contentData == null)
            {
                contentData = new NotificationContentData();
            }


            if (NotifContentTabs.TabPages.Contains(tabPageEmail))
            {
                _currentEmailSubject = contentData.EmailSubject ?? NotificationTemplateProvider.GetDefaultEmailSubject(_sourceFeature, _sourceEntityName);
                _currentEmailBody = contentData.EmailBody ?? NotificationTemplateProvider.GetDefaultEmailBody(_sourceFeature, _sourceEntityName);
                if (txtEmailSubject != null) txtEmailSubject.Text = _currentEmailSubject;
                if (rtbEmailBody != null) rtbEmailBody.Text = _currentEmailBody;
            }

            if (NotifContentTabs.TabPages.Contains(tabPageSMS))
            {
                _currentSmsContent = contentData.SmsContent ?? NotificationTemplateProvider.GetDefaultSmsContent(_sourceFeature, _sourceEntityName);
                if (txtSMSMessage != null) txtSMSMessage.Text = _currentSmsContent;
            }

            if (NotifContentTabs.TabPages.Contains(tabPageTelegram))
            {
                _currentTelegramContent = contentData.TelegramContent ?? NotificationTemplateProvider.GetDefaultTelegramContent(_sourceFeature, _sourceEntityName);
                if (txtTelegramMessage != null) txtTelegramMessage.Text = _currentTelegramContent;
            }

            if (NotifContentTabs.TabPages.Contains(tabPageFbMessenger))
            {
                _currentFbMessengerMessageContent = contentData.FbMessengerContent ?? NotificationTemplateProvider.GetDefaultFbMessengerContent(_sourceFeature, _sourceEntityName);
                if (txtFBMsgMessage != null) txtFBMsgMessage.Text = _currentFbMessengerMessageContent;
            }
        }

        private Control GetActiveMessageControl()
        {
            if (NotifContentTabs.SelectedTab == tabPageEmail)
            {
                if (txtEmailSubject.Focused) return txtEmailSubject;
                if (rtbEmailBody.Focused) return rtbEmailBody;
                return rtbEmailBody;
            }
            if (NotifContentTabs.SelectedTab == tabPageSMS)
            {
                if (txtSMSMessage != null && txtSMSMessage.Focused) return txtSMSMessage;
                return txtSMSMessage;
            }
            if (NotifContentTabs.SelectedTab == tabPageTelegram)
            {
                if (txtTelegramMessage != null && txtTelegramMessage.Focused) return txtTelegramMessage;
                return txtTelegramMessage;
            }
            if (NotifContentTabs.SelectedTab == tabPageFbMessenger)
            {
                if (txtFBMsgMessage != null && txtFBMsgMessage.Focused) return txtFBMsgMessage;
                return txtFBMsgMessage;
            }
            return null;
        }

        private void btnInserTokens_Click(object sender, EventArgs e)
        {
            if (listBoxToken.SelectedItem == null)
            {
                MessageBox.Show("Please select a token from the list to insert.", "No Token Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string selectedToken = listBoxToken.SelectedItem.ToString();
            Control activeMessageControl = GetActiveMessageControl();

            if (activeMessageControl is TextBoxBase activeTextBox)
            {
                int selectionStart = activeTextBox.SelectionStart;
                activeTextBox.Text = activeTextBox.Text.Insert(selectionStart, selectedToken);
                activeTextBox.SelectionStart = selectionStart + selectedToken.Length;
                activeTextBox.Focus();
            }
            else
            {
                MessageBox.Show("Please select a message field (e.g., Subject or Body) where the token should be inserted.", "No Message Field Active", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnSaveContent_Click(object sender, EventArgs e)
        {
            NotificationContentData contentToSave = new NotificationContentData();
            bool anyContentProvidedForSelectedTypes = false;

            if (_selectedNotificationTypes.Contains("Email") && NotifContentTabs.TabPages.Contains(tabPageEmail))
            {
                contentToSave.EmailSubject = txtEmailSubject.Text.Trim(); // Store current value
                contentToSave.EmailBody = rtbEmailBody.Text.Trim();   // Store current value
                if (string.IsNullOrWhiteSpace(contentToSave.EmailSubject) || string.IsNullOrWhiteSpace(contentToSave.EmailBody))
                {
                    MessageBox.Show("Email Subject and Body cannot be empty if Email notification is selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                anyContentProvidedForSelectedTypes = true;
            }

            if (_selectedNotificationTypes.Contains("SMS") && NotifContentTabs.TabPages.Contains(tabPageSMS))
            {
                contentToSave.SmsContent = txtSMSMessage.Text.Trim(); // Store current value
                if (string.IsNullOrWhiteSpace(contentToSave.SmsContent))
                {
                    MessageBox.Show("SMS Message cannot be empty if SMS notification is selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                anyContentProvidedForSelectedTypes = true;
            }

            if (_selectedNotificationTypes.Contains("Telegram") && NotifContentTabs.TabPages.Contains(tabPageTelegram))
            {
                contentToSave.TelegramContent = txtTelegramMessage.Text.Trim(); // Store current value
                if (string.IsNullOrWhiteSpace(contentToSave.TelegramContent))
                {
                    MessageBox.Show("Telegram Message cannot be empty if Telegram notification is selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                anyContentProvidedForSelectedTypes = true;
            }

            if (_selectedNotificationTypes.Contains("FacebookMessenger") && NotifContentTabs.TabPages.Contains(tabPageFbMessenger))
            {
                contentToSave.FbMessengerContent = txtFBMsgMessage.Text.Trim(); // Store current value
                if (string.IsNullOrWhiteSpace(contentToSave.FbMessengerContent))
                {
                    MessageBox.Show("Facebook Messenger Message cannot be empty if selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                anyContentProvidedForSelectedTypes = true;
            }

            if (!anyContentProvidedForSelectedTypes && _selectedNotificationTypes.Any())
            {
                MessageBox.Show("Please provide content for at least one of your selected notification channels.", "Content Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_selectedNotificationTypes.Any())
            {
                MessageBox.Show("No notification types were selected for this rule. Content cannot be saved.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string contentJson = JsonConvert.SerializeObject(contentToSave);
                _notificationRuleRepo.UpdateNotificationRuleContent(_ruleId, contentJson);
                _notificationRuleRepo.ResetRuleSchedule(_ruleId);
                MessageBox.Show("Notification content saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving notification content: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
