using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using CustomerAndServerMaintenanceTracking.AppLogs;
using CustomerAndServerMaintenanceTracking.CustomCells;
using CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules;
using CustomerAndServerMaintenanceTracking.Models.ViewModels;
using Newtonsoft.Json;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking
{

    public partial class NotificationManagerForm : Form, IRefreshableForm
    {
        private OverlayForm _overlayForm;

        private DataGridViewCheckBoxColumn _deleteCheckBoxColumn;
        private bool _isDeleteModeActive = false;
        private ToolStripButton toolStripBtnDeleteCheckedNotifications;

        private NotificationRuleRepository _notificationRuleRepo;
        private List<NotificationRuleDisplayViewModel> _ruleDisplayList;
        private BindingList<NotificationRuleDisplayViewModel> _bindingListRules;

        // Add these repository fields
        private ServiceLogRepository _logRepository;
        private TagRepository _tagRepository;
        private NetwatchConfigRepository _netwatchConfigRepository;
        private readonly NotificationHistoryRepository _historyLogRepository;

        public void RefreshDataViews()
        {
            if (_isDeleteModeActive)
            {
                // For debugging, you can see this message in your Output window.
                Console.WriteLine("NotificationManagerForm: Auto-refresh skipped because Delete Mode is active.");
                return; // Exit the method immediately, preventing the data reload.
            }

            // This method is required by IRefreshableForm. It will be called by the Dashboard's timer.
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                // Use BeginInvoke to ensure the call is safely made on the UI thread
                this.BeginInvoke((MethodInvoker)delegate
                {
                    Console.WriteLine("NotificationManagerForm: Refresh triggered.");
                    LoadNotificationRules();
                });
            }
        }
        private void ShowOverlay()
        {
            if (_overlayForm == null || _overlayForm.IsDisposed)
            {
                _overlayForm = new OverlayForm { Owner = this.MdiParent ?? this }; // Cover MDI parent or self
            }
            _overlayForm.StartPosition = FormStartPosition.Manual;
            Form formToCover = this.MdiParent ?? this;
            Point location = formToCover.PointToScreen(Point.Empty);
            _overlayForm.Bounds = new Rectangle(location, formToCover.ClientSize);
            _overlayForm.Show();
            _overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            _overlayForm?.Close();
            _overlayForm = null;
        }


        public NotificationManagerForm()
        {
            InitializeComponent();

            _logRepository = new ServiceLogRepository(); // Initialize
            _tagRepository = new TagRepository();       // Initialize

            _notificationRuleRepo = new NotificationRuleRepository();
            _historyLogRepository = new NotificationHistoryRepository();
            _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, _tagRepository);

            _ruleDisplayList = new List<NotificationRuleDisplayViewModel>();
            _bindingListRules = new BindingList<NotificationRuleDisplayViewModel>();

            Button newRuleBtn = this.Controls.Find("btnNewNotificationRule", true).FirstOrDefault() as Button;
            if (newRuleBtn != null)
            {
                newRuleBtn.Click += BtnNewNotificationRule_Click;
            }
            // If not, try to add "New Rule" to the ToolStripDropDownButton named "ToolStripMenuItem"
            // This assumes "ToolStripMenuItem" is the Name property of your ToolStripDropDownButton in the designer.
            else if (this.ToolStripMenuItem is ToolStripDropDownButton moreButton) // Directly reference by its name
            {
                // Check if it already exists to prevent duplicates if constructor is called multiple times (unlikely for forms)
                if (!moreButton.DropDownItems.OfType<ToolStripMenuItem>().Any(item => item.Name == "newRuleContextMenuItem"))
                {
                    ToolStripMenuItem newRuleMenuItem = new ToolStripMenuItem("New Notification Rule");
                    newRuleMenuItem.Name = "newRuleContextMenuItem"; // Give it a name for future reference
                    newRuleMenuItem.Click += BtnNewNotificationRule_Click;
                    moreButton.DropDownItems.Insert(0, newRuleMenuItem); // Add at the top
                }
            }

            _deleteCheckBoxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "DeleteChkCol",
                HeaderText = "Select", // Or an empty header
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Frozen = true // Keep it visible when scrolling horizontally
            };

            _deleteCheckBoxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "DeleteChkCol",
                HeaderText = "Select",
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Frozen = true
            };

            if (this.deleteToolStripMenuItem != null)
            {
                this.deleteToolStripMenuItem.Click -= deleteToolStripMenuItem_Click;
                this.deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            }

            // This section should now correctly refer to the designer-generated control
            if (this.toolStripBtnDeleteCheckedNotifications != null)
            {
                this.toolStripBtnDeleteCheckedNotifications.Click -= toolStripBtnDeleteCheckedNotifications_Click;
                this.toolStripBtnDeleteCheckedNotifications.Click += toolStripBtnDeleteCheckedNotifications_Click;
                this.toolStripBtnDeleteCheckedNotifications.Visible = false; // Ensure it's initially hidden
            }
            else
            {
                // This else block should ideally not be hit if you added the button in the designer
                Console.WriteLine("Error: toolStripBtnDeleteCheckedNotifications not found in designer.");
            }


            // Wire up search
            if (txtSearchRules != null)
            {
                txtSearchRules.TextChanged += TxtSearchRules_TextChanged;
            }

        }

        private void NotificationManagerForm_Load(object sender, EventArgs e)
        {
            SetupNotificationListColumns();
            LoadNotificationRules();
        }

        private void SetupNotificationListColumns()
        {
            if (dgvNotificationList == null) return;
            dgvNotificationList.AutoGenerateColumns = false;
            dgvNotificationList.Columns.Clear();
            dgvNotificationList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // Explicitly set for width control

            dgvNotificationList.RowTemplate.Height = 30;

            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "RuleIdCol", DataPropertyName = "RuleId", HeaderText = "ID", Visible = false, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "NameCol", DataPropertyName = "Name", HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25 });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "TriggerCol", DataPropertyName = "TriggerConditionSummary", HeaderText = "Trigger", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "TypeCol", DataPropertyName = "NotificationTypesSummary", HeaderText = "Channels", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "MessageCol", DataPropertyName = "MessageSummary", HeaderText = "Message", Width = 150, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecipientsCol", DataPropertyName = "RecipientsSummary", HeaderText = "Recipients", Width = 150, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScheduleCol", DataPropertyName = "ScheduleSummary", HeaderText = "Schedule", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "RunCountCol", DataPropertyName = "RunCount", HeaderText = "Run #", Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "NextRunCol", DataPropertyName = "NextRun", HeaderText = "Next Run", Width = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });

            // REMOVED: ToggleStatusActionCol
            // DataGridViewButtonColumn toggleStatusColumn = new DataGridViewButtonColumn 
            //     { Name = "ToggleStatusActionCol", HeaderText = "Status", Width = 70, UseColumnTextForButtonValue = false };
            // dgvNotificationList.Columns.Add(toggleStatusColumn);

            // MODIFIED: MultiActionCol will now contain Start/Stop and Logs (custom painted)
            dgvNotificationList.Columns.Add(new DataGridViewTextBoxColumn { Name = "MultiActionCol", HeaderText = "Actions", Width = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.None }); // Width for two buttons

            dgvNotificationList.AllowUserToAddRows = false;
            dgvNotificationList.ReadOnly = true;
            //dgvNotificationList.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
     
            dgvNotificationList.CellFormatting -= DgvNotificationList_CellFormatting;
            dgvNotificationList.CellPainting -= dgvNotificationList_CellPainting;
            dgvNotificationList.CellClick -= dgvNotificationList_CellClick; // Also remove the new one just in case

            // Now, add back the ones we need, using CellClick instead of CellContentClick
            dgvNotificationList.CellClick += dgvNotificationList_CellClick;
            dgvNotificationList.CellFormatting += DgvNotificationList_CellFormatting;
            dgvNotificationList.CellPainting += dgvNotificationList_CellPainting;
        }

        private void dgvNotificationList_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {// Ensure we are in the correct column and row
            if (e.RowIndex < 0 || e.ColumnIndex != dgvNotificationList.Columns["MultiActionCol"].Index)
                return;

            // Paint the background of the cell
            e.PaintBackground(e.ClipBounds, true);

            var grid = (DataGridView)sender;
            if (!(grid.Rows[e.RowIndex].DataBoundItem is NotificationRuleDisplayViewModel ruleVM))
                return;

            // Define the rectangles for our two buttons
            int padding = 3;
            int buttonWidth = (e.CellBounds.Width - (padding * 3)) / 2;
            int buttonHeight = e.CellBounds.Height - (padding * 2);
            int topOffset = e.CellBounds.Top + padding;

            Rectangle startStopButtonRect = new Rectangle(e.CellBounds.Left + padding, topOffset, buttonWidth, buttonHeight);
            Rectangle logsButtonRect = new Rectangle(startStopButtonRect.Right + padding, topOffset, buttonWidth, buttonHeight);

            // --- Button 1: The Start/Stop Button ---
            string startStopText = ruleVM.IsEnabled ? "Stop" : "Start";
            System.Drawing.Color backColor = ruleVM.IsEnabled ? System.Drawing.Color.Salmon : System.Drawing.Color.LightGreen;

            // Manually fill the button's background with the chosen color
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, startStopButtonRect);
            }

            // ** THE FIX IS HERE **
            // Draw the button border and text using the correct 7-argument overload.
            // We add TextFormatFlags to center the text.
            ButtonRenderer.DrawButton(e.Graphics, startStopButtonRect, startStopText, grid.Font,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                false,
                System.Windows.Forms.VisualStyles.PushButtonState.Normal);


            // --- Button 2: The standard "Logs" Button ---
            // ** THE FIX IS HERE AS WELL **
            // Use the same correct overload for the "Logs" button.
            ButtonRenderer.DrawButton(e.Graphics, logsButtonRect, "Logs", grid.Font,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                false,
                System.Windows.Forms.VisualStyles.PushButtonState.Normal);

            // Tell the grid that we've handled painting this cell
            e.Handled = true;
        }

        private void LoadNotificationRules()
        {
            // --- STEP 1: Remember which rule IDs are currently checked for deletion ---
            HashSet<int> checkedRuleIds = new HashSet<int>();
            if (_isDeleteModeActive && _bindingListRules != null)
            {
                // Get the IDs of all items where the user has checked the box
                checkedRuleIds = new HashSet<int>(_bindingListRules.Where(r => r.IsSelectedForDeletion).Select(r => r.RuleId));
            }

            try
            {
                // --- STEP 2: Fetch the latest data from the database as before ---
                List<NotificationRule> rulesFromRepo = _notificationRuleRepo.GetNotificationRulesForManagerDisplay();

                // --- STEP 3: Create a NEW display list, but PRESERVE the checked state ---
                _ruleDisplayList = new List<NotificationRuleDisplayViewModel>(); // Clear and rebuild the master display list

                foreach (var rule in rulesFromRepo)
                {
                    var displayModel = new NotificationRuleDisplayViewModel
                    {
                        RuleId = rule.Id,
                        Name = rule.RuleName,
                        IsEnabled = rule.IsEnabled,
                        RunCount = rule.RunCount,
                        NextRun = rule.NextRunTime,
                        SourceFeature = rule.SourceFeature,
                        SourceEntityId = rule.SourceEntityId,

                        // Pass the raw JSON to the ViewModel for the IsFullyConfigured property to use
                        NotificationChannelsJson = rule.NotificationChannelsJson,
                        ContentDetailsJson = rule.ContentDetailsJson,
                        RecipientDetailsJson = rule.RecipientDetailsJson,
                        ScheduleDetailsJson = rule.ScheduleDetailsJson,

                        // --- KEY CHANGE: Set the IsSelectedForDeletion property based on our remembered IDs ---
                        IsSelectedForDeletion = checkedRuleIds.Contains(rule.Id)
                    };

                    // Populate the summary strings for display
                    List<string> channels = new List<string>();
                    try { if (!string.IsNullOrWhiteSpace(rule.NotificationChannelsJson)) channels = JsonConvert.DeserializeObject<List<string>>(rule.NotificationChannelsJson) ?? new List<string>(); } catch { }

                    displayModel.NotificationTypesSummary = channels.Any() ? string.Join(", ", channels) : "N/A";
                    displayModel.TriggerConditionSummary = FormatTriggerSummaryForDisplay(rule.SourceFeature, rule.TriggerDetailsJson, rule.SourceEntityId, rule.RuleName);
                    displayModel.MessageSummary = FormatContentSummaryForDisplay(rule.ContentDetailsJson, channels);
                    displayModel.RecipientsSummary = FormatRecipientSummaryForDisplay(rule.RecipientDetailsJson);
                    displayModel.ScheduleSummary = FormatScheduleSummaryForDisplay(rule.ScheduleDetailsJson);

                    _ruleDisplayList.Add(displayModel);
                }

                // --- STEP 4: Apply filters and bind the new, state-aware list to the grid ---
                ApplyFiltersAndBindGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading notification rules: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string FormatTriggerSummaryForDisplay(string sourceFeature, string triggerJson, int? sourceEntityId, string ruleNameForContext)
        {
            if (string.IsNullOrWhiteSpace(triggerJson) || triggerJson == "{}") return "Not Set";
            if (sourceFeature == "Netwatch")
            {
                try
                {
                    var details = JsonConvert.DeserializeObject<NetwatchTriggerDetails>(triggerJson);
                    if (details == null || (details.SelectedNetwatchConfigIds == null || !details.SelectedNetwatchConfigIds.Any()) || (details.SelectedNetwatchStatuses == null || !details.SelectedNetwatchStatuses.Any())) return "Netwatch: Incomplete";

                    string netwatchName = "Netwatch";
                    if (details.SelectedNetwatchConfigIds.Count == 1 && sourceEntityId.HasValue && details.SelectedNetwatchConfigIds.First() == sourceEntityId.Value)
                    {
                        // If you have a way to get NetwatchConfig name by ID here, you can use it.
                        // For now, use the ruleNameForContext as a hint or a generic name.
                        // netwatchName = _netwatchConfigRepo.GetNetwatchConfigById(sourceEntityId.Value)?.NetwatchName ?? ruleNameForContext.Replace("Alert for ", "");
                        // Simplified for now as _netwatchConfigRepo might not be directly available or efficient here.
                        netwatchName = ruleNameForContext.Replace("Alert for ", "").Replace("Add Notification: ", "");
                    }
                    else if (details.SelectedNetwatchConfigIds != null && details.SelectedNetwatchConfigIds.Any())
                    {
                        netwatchName = $"{details.SelectedNetwatchConfigIds.Count} Config(s)";
                    }

                    string ipSummary = details.TriggerOnAllIPs ? "All IPs" : $"{(details.SelectedSpecificIPs?.Count ?? 0)} Specific IP(s)";
                    return $"Netwatch: {netwatchName} ({ipSummary}) -> {string.Join(", ", details.SelectedNetwatchStatuses)}";
                }
                catch { return "Netwatch: Invalid Trigger Data"; }
            }
            return "Trigger Not Set";
        }
        private string FormatContentSummaryForDisplay(string contentJson, List<string> channels)
        {
            // If there are no channels selected for the rule, there can be no content.
            if (channels == null || !channels.Any())
            {
                return "Not Set";
            }

            // If the JSON is empty, null, or just an empty object "{}", content is not set.
            if (string.IsNullOrWhiteSpace(contentJson) || contentJson == "{}")
            {
                return "Not Set";
            }

            try
            {
                var content = JsonConvert.DeserializeObject<NotificationContentData>(contentJson);
                if (content == null) return "Not Set";

                // Check if content exists for at least one of the rule's selected channels.
                bool actualContentExists = false;
                if (channels.Contains("Email") && (!string.IsNullOrWhiteSpace(content.EmailSubject) || !string.IsNullOrWhiteSpace(content.EmailBody)))
                    actualContentExists = true;

                if (!actualContentExists && channels.Contains("SMS") && !string.IsNullOrWhiteSpace(content.SmsContent))
                    actualContentExists = true;

                if (!actualContentExists && channels.Contains("Telegram") && !string.IsNullOrWhiteSpace(content.TelegramContent))
                    actualContentExists = true;

                if (!actualContentExists && channels.Contains("FacebookMessenger") && !string.IsNullOrWhiteSpace(content.FbMessengerContent))
                    actualContentExists = true;

                // Return "View/Edit Message" if content exists, otherwise "Not Set".
                return actualContentExists ? "View/Edit Message" : "Not Set";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error formatting content summary: {ex.Message}");
                return "Invalid Data"; // Or "Not Set"
            }
        }
        private string FormatRecipientSummaryForDisplay(string recipientsJson)
        {
            if (string.IsNullOrWhiteSpace(recipientsJson) || recipientsJson == "{}") return "Not Set";
            try
            {
                var recipients = JsonConvert.DeserializeObject<NotificationRecipientData>(recipientsJson); // Assumes NotificationRecipientData is in SharedLibrary.Models
                if (recipients == null) return "Invalid Recipient Data";
                List<string> parts = new List<string>();
                if (recipients.RoleIds != null && recipients.RoleIds.Any()) parts.Add($"{recipients.RoleIds.Count} Role(s)");
                if (recipients.UserAccountIds != null && recipients.UserAccountIds.Any()) parts.Add($"{recipients.UserAccountIds.Count} User(s)");
                if (recipients.CustomerIdentifiers != null && recipients.CustomerIdentifiers.Any()) parts.Add($"{recipients.CustomerIdentifiers.Count} Customer(s)");
                if (recipients.TagIds != null && recipients.TagIds.Any()) parts.Add($"{recipients.TagIds.Count} Tag(s)");
                if (recipients.AdditionalEmails != null && recipients.AdditionalEmails.Any()) parts.Add($"{recipients.AdditionalEmails.Count} Email(s)");
                if (recipients.AdditionalPhones != null && recipients.AdditionalPhones.Any()) parts.Add($"{recipients.AdditionalPhones.Count} Phone(s)");
                return parts.Any() ? string.Join(", ", parts) : "No Recipients Set";
            }
            catch { return "Invalid Recipient Data"; }
        }

        private string FormatScheduleSummaryForDisplay(string scheduleJson)
        {
            if (string.IsNullOrWhiteSpace(scheduleJson) || scheduleJson == "{}") return "Not Set / One Time";
            try
            {
                var schedule = JsonConvert.DeserializeObject<NotificationScheduleData>(scheduleJson); // Assumes NotificationScheduleData is in SharedLibrary.Models
                if (schedule == null) return "Not Set";

                string summary = $"Starts: {schedule.StartDateTime:g}";
                if (schedule.IsRecurring && schedule.IntervalSeconds.HasValue && schedule.IntervalSeconds > 0)
                {
                    summary += $", Every {TimeSpan.FromSeconds(schedule.IntervalSeconds.Value).TotalMinutes:N0}min";
                    if (schedule.DaysOfWeek != null && schedule.DaysOfWeek.Any() && schedule.DaysOfWeek.Count < 7)
                    {
                        summary += $" on {string.Join(",", schedule.DaysOfWeek.Select(d => d.ToString().Substring(0, 3)))}";
                    }
                    else if (schedule.DaysOfWeek != null && schedule.DaysOfWeek.Count == 7)
                    {
                        summary += " (Daily)";
                    }
                }
                else
                {
                    summary += " (One Time)";
                }
                if (schedule.TriggerTrueDurationSeconds > 0)
                {
                    summary += $", After Cond. True for {schedule.TriggerTrueDurationSeconds}s";
                }
                return summary;
            }
            catch { return "Invalid Schedule Data"; }
        }

        private void ApplyFiltersAndBindGrid()
        {
            if (dgvNotificationList == null) return;

            IEnumerable<NotificationRuleDisplayViewModel> currentList = _ruleDisplayList;
            string searchText = txtSearchRules.Text.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                currentList = currentList.Where(r =>
                    (r.Name?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.TriggerConditionSummary?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.NotificationTypesSummary?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.RecipientsSummary?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.ScheduleSummary?.ToLowerInvariant().Contains(searchText) ?? false)
                );
            }
            _bindingListRules = new BindingList<NotificationRuleDisplayViewModel>(currentList.ToList());
            dgvNotificationList.DataSource = _bindingListRules;
        }

        private void TxtSearchRules_TextChanged(object sender, EventArgs e)
        {
            ApplyFiltersAndBindGrid();
        }

        private void BtnNewNotificationRule_Click(object sender, EventArgs e)
        {
            // This now launches the SIMPLIFIED AddNotificationRule form.
            // The sourceEntityName and sourceEntityId might be null or context-specific if
            // launching from a general "New Rule" button vs. from NetwatchList.
            // For a general new rule, these might not apply directly.
            // We assume a general source or allow it to be more generic initially.
            using (AddNotificationRule newRuleForm = new AddNotificationRule(null, 0, "General")) // Pass generic source
            {
                if (newRuleForm.ShowDialog(this) == DialogResult.OK)
                {
                    LoadNotificationRules(); // Refresh the list after a new rule is initially saved
                }
            }
        }

        private void DgvNotificationList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvNotificationList.Rows.Count <= e.RowIndex || dgvNotificationList.Rows[e.RowIndex].IsNewRow)
                return;

            var grid = (DataGridView)sender;
            if (!(grid.Rows[e.RowIndex].DataBoundItem is NotificationRuleDisplayViewModel rule))
                return;

            string colName = grid.Columns[e.ColumnIndex].Name;

            // Apply default styles first
            e.CellStyle.Font = grid.DefaultCellStyle.Font;
            e.CellStyle.ForeColor = grid.DefaultCellStyle.ForeColor;
            e.CellStyle.BackColor = (e.RowIndex % 2 == 0) ? System.Drawing.SystemColors.Window : System.Drawing.SystemColors.ControlLight;
            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;


            if (colName == "NameCol")
            {
                // No special formatting needed here anymore, default styles will be used.
            }
            else if (colName == "MessageCol")
            {
                // Check if content is set
                if (string.IsNullOrWhiteSpace(rule.ContentDetailsJson) || rule.ContentDetailsJson == "{}")
                {
                    e.Value = "Enter Message >>";
                    e.CellStyle.ForeColor = System.Drawing.Color.Red; // Color red if not set
                }
                else
                {
                    // If content is set, use the summary text with the default color
                    e.Value = string.IsNullOrWhiteSpace(rule.MessageSummary) || rule.MessageSummary == "Not Set"
                            ? "View/Edit Message"
                            : rule.MessageSummary;
                }
                e.FormattingApplied = true;
            }
            else if (colName == "RecipientsCol")
            {
                // Check if recipients are set
                if (string.IsNullOrWhiteSpace(rule.RecipientDetailsJson) || rule.RecipientDetailsJson == "{}")
                {
                    e.Value = "Choose Recipient >>";
                    e.CellStyle.ForeColor = System.Drawing.Color.Red; // Color red if not set
                }
                else
                {
                    e.Value = string.IsNullOrWhiteSpace(rule.RecipientsSummary) || rule.RecipientsSummary == "Not Set"
                            ? "View/Edit Recipients"
                            : rule.RecipientsSummary;
                }
                e.FormattingApplied = true;
            }
            else if (colName == "ScheduleCol")
            {
                // Check if schedule is set
                if (string.IsNullOrWhiteSpace(rule.ScheduleDetailsJson) || rule.ScheduleDetailsJson == "{}")
                {
                    e.Value = "Set Schedule >>";
                    e.CellStyle.ForeColor = System.Drawing.Color.Red; // Color red if not set
                }
                else
                {
                    e.Value = string.IsNullOrWhiteSpace(rule.ScheduleSummary) || rule.ScheduleSummary == "Not Set / One Time"
                            ? "View/Edit Schedule"
                            : rule.ScheduleSummary;
                }
                e.FormattingApplied = true;
            }
            else if (colName == "ToggleStatusActionCol")
            {
                e.Value = rule.IsEnabled ? "Stop" : "Start";
                e.CellStyle.BackColor = rule.IsEnabled ? System.Drawing.Color.Salmon : System.Drawing.Color.LightGreen;
                e.CellStyle.ForeColor = System.Drawing.Color.Black;
                e.FormattingApplied = true;
            }
        }

        private void dgvNotificationList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var grid = (DataGridView)sender;
            if (!(grid.Rows[e.RowIndex].DataBoundItem is NotificationRuleDisplayViewModel selectedDisplayRule))
                return;

            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "MultiActionCol")
            {
                Rectangle cellBounds = grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point mousePos = grid.PointToClient(Cursor.Position);

                if (!cellBounds.Contains(mousePos)) return;

                Point relativeMousePos = new Point(mousePos.X - cellBounds.Left, mousePos.Y - cellBounds.Top);

                int totalPadding = 9; int buttonSpacing = 3; int numButtons = 2;
                int availableWidth = cellBounds.Width - totalPadding;
                int buttonWidth = availableWidth / numButtons;
                int topOffsetForHitTest = 3; int leftOffsetForHitTest = 3;
                int buttonHeightForHitTest = cellBounds.Height - 6;

                Rectangle startStopButtonRect = new Rectangle(leftOffsetForHitTest, topOffsetForHitTest, buttonWidth, buttonHeightForHitTest);
                Rectangle logsButtonRect = new Rectangle(leftOffsetForHitTest + buttonWidth + buttonSpacing, topOffsetForHitTest, buttonWidth, buttonHeightForHitTest);

                if (startStopButtonRect.Contains(relativeMousePos))
                {
                    bool newStatus = !selectedDisplayRule.IsEnabled;
                    try
                    {
                        if (_notificationRuleRepo.UpdateNotificationRuleStatus(selectedDisplayRule.RuleId, newStatus))
                        {
                            // --- START OF NEW CODE: Logging the action ---
                            string logMessage = newStatus ? "Rule ENABLED by user." : "Rule DISABLED by user.";
                            _historyLogRepository.WriteLog(new NotificationHistoryLog
                            {
                                RuleId = selectedDisplayRule.RuleId,
                                LogLevel = "INFO",
                                Message = logMessage
                            });
                            // --- END OF NEW CODE ---
                            LoadNotificationRules();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update rule status.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (logsButtonRect.Contains(relativeMousePos))
                {
                    NotificationHistoryLogs logsForm = new NotificationHistoryLogs(selectedDisplayRule.RuleId, selectedDisplayRule.Name);
                    logsForm.ShowDialog(this);
                }
            }
            else if (colName == "NameCol" || colName == "TriggerCol" || colName == "TypeCol" ||
                     colName == "MessageCol" || colName == "RecipientsCol" || colName == "ScheduleCol")
            {
                // This part remains the same
                NotificationRule fullRule = _notificationRuleRepo.GetRuleById(selectedDisplayRule.RuleId);
                if (fullRule == null) { MessageBox.Show("Could not retrieve rule details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

                if (fullRule.IsEnabled)
                {
                    MessageBox.Show("Please stop this rule before making changes.", "Rule is Active", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool refreshGridAfterModal = false;
                using (Form editForm = GetEditFormForColumn(colName, fullRule, selectedDisplayRule))
                {
                    if (editForm != null)
                    {
                        editForm.StartPosition = FormStartPosition.CenterParent;
                        ShowOverlay();
                        if (editForm.ShowDialog(this) == DialogResult.OK)
                        {
                            refreshGridAfterModal = true;
                        }
                        CloseOverlay();
                    }
                }

                if (refreshGridAfterModal)
                {
                    LoadNotificationRules();
                }
            }
        }
        private Form GetEditFormForColumn(string colName, NotificationRule fullRule, NotificationRuleDisplayViewModel displayRule)
        {
            List<string> notificationChannels = new List<string>();
            try
            {
                if (!string.IsNullOrWhiteSpace(fullRule.NotificationChannelsJson))
                    notificationChannels = JsonConvert.DeserializeObject<List<string>>(fullRule.NotificationChannelsJson) ?? new List<string>();
            }
            catch { }

            switch (colName)
            {
                case "NameCol":
                case "TriggerCol":
                case "TypeCol":
                    return new AddNotificationRule(displayRule.RuleId);
                case "MessageCol":
                    string sourceEntityName = DetermineSourceEntityName(fullRule, displayRule.Name);
                    return new AddNotificationContent(displayRule.RuleId, displayRule.Name, notificationChannels, fullRule.SourceFeature, sourceEntityName);
                case "RecipientsCol":
                    return new AddNotificationRecipients(displayRule.RuleId, displayRule.Name);
                case "ScheduleCol":
                    return new AddNotificationSchedule(displayRule.RuleId, displayRule.Name);
                default:
                    return null;
            }
        }
        private string DetermineSourceEntityName(NotificationRule fullRule, string defaultName)
        {
            string entityName = defaultName;
            if (fullRule.SourceFeature == "Netwatch" && fullRule.SourceEntityId.HasValue && _netwatchConfigRepository != null)
            {
                var netwatchConf = _netwatchConfigRepository.GetNetwatchConfigWithDetails(fullRule.SourceEntityId.Value);
                if (netwatchConf != null) entityName = netwatchConf.NetwatchName;
            }
            // Add else if for other source features if needed
            return entityName;
        }

        private void ToggleDeleteMode(bool activate)
        {
            _isDeleteModeActive = activate;
            if (_isDeleteModeActive)
            {
                deleteToolStripMenuItem.Text = "Cancel Deletion";
                if (!dgvNotificationList.Columns.Contains(_deleteCheckBoxColumn))
                {
                    dgvNotificationList.Columns.Insert(0, _deleteCheckBoxColumn); // Add as first column
                }
                _deleteCheckBoxColumn.Visible = true;
                if (toolStripBtnDeleteCheckedNotifications != null) toolStripBtnDeleteCheckedNotifications.Visible = true;
                dgvNotificationList.ReadOnly = false; // Allow checkbox interaction
                foreach (DataGridViewColumn col in dgvNotificationList.Columns)
                {
                    if (col != _deleteCheckBoxColumn) col.ReadOnly = true;
                }
            }
            else
            {
                deleteToolStripMenuItem.Text = "Delete";
                foreach (var item in _bindingListRules)
                {
                    item.IsSelectedForDeletion = false;
                }

                if (dgvNotificationList.Columns.Contains(_deleteCheckBoxColumn))
                {
                    // Clear checks before hiding/removing
                    foreach (DataGridViewRow row in dgvNotificationList.Rows)
                    {
                        if (row.Cells["DeleteChkCol"] is DataGridViewCheckBoxCell chkCell)
                        {
                            chkCell.Value = false;
                        }
                    }
                    _deleteCheckBoxColumn.Visible = false; // Just hide it, simpler than remove/add
                }
                if (toolStripBtnDeleteCheckedNotifications != null) toolStripBtnDeleteCheckedNotifications.Visible = false;
                dgvNotificationList.ReadOnly = true; // Set grid back to read-only
            }
            dgvNotificationList.Invalidate(); // Refresh grid display
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleDeleteMode(!_isDeleteModeActive);
        }

        private void toolStripBtnDeleteCheckedNotifications_Click(object sender, EventArgs e)
        {
            dgvNotificationList.EndEdit();

            List<int> ruleIdsToDelete = new List<int>();
            if (dgvNotificationList.Columns.Contains("DeleteChkCol"))
            {
                foreach (DataGridViewRow row in dgvNotificationList.Rows)
                {
                    // Ensure the cell and its value are not null before trying to access/convert
                    DataGridViewCheckBoxCell chkCell = row.Cells["DeleteChkCol"] as DataGridViewCheckBoxCell;
                    if (chkCell != null && chkCell.Value != null && Convert.ToBoolean(chkCell.Value) == true)
                    {
                        if (row.DataBoundItem is NotificationRuleDisplayViewModel ruleVM)
                        {
                            ruleIdsToDelete.Add(ruleVM.RuleId);
                        }
                    }
                }
            }

            if (!ruleIdsToDelete.Any())
            {
                MessageBox.Show("No notification rules selected for deletion.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete {ruleIdsToDelete.Count} selected notification rule(s)?",
                                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                int successCount = 0;
                int failCount = 0;
                List<string> failedRuleNames = new List<string>();

                foreach (int ruleId in ruleIdsToDelete)
                {
                    try
                    {
                        if (_notificationRuleRepo.DeleteNotificationRule(ruleId))
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            // Optionally, try to get rule name for better error reporting if needed
                            // var ruleName = _ruleDisplayList.FirstOrDefault(r => r.RuleId == ruleId)?.Name ?? $"ID {ruleId}";
                            // failedRuleNames.Add(ruleName);
                            Console.WriteLine($"Failed to delete rule ID: {ruleId} (not found or DB error).");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        // var ruleName = _ruleDisplayList.FirstOrDefault(r => r.RuleId == ruleId)?.Name ?? $"ID {ruleId}";
                        // failedRuleNames.Add(ruleName);
                        Console.WriteLine($"Error deleting rule ID {ruleId}: {ex.Message}");
                    }
                }

                string message = $"{successCount} rule(s) deleted successfully.";
                if (failCount > 0)
                {
                    message += $"\n{failCount} rule(s) failed to delete.";
                    // if (failedRuleNames.Any()) message += $"\nFailed for: {string.Join(", ", failedRuleNames)}";
                }

                MessageBox.Show(message,
                                "Deletion Complete", MessageBoxButtons.OK,
                                failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                LoadNotificationRules();
                ToggleDeleteMode(false);
            }
        }
    }
}
