using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class AddNotificationSchedule : Form
    {
        private readonly int _ruleId;
        private readonly string _ruleName; // For display purposes in the title

        private NotificationRuleRepository _notificationRuleRepo;
        private NotificationScheduleData _currentScheduleData;

        public AddNotificationSchedule(int ruleId, string ruleName)
        {
            InitializeComponent();
            _ruleId = ruleId;
            _ruleName = ruleName;

            _notificationRuleRepo = new NotificationRuleRepository();
            _currentScheduleData = new NotificationScheduleData();

            this.Text = $"Edit Schedule for: {_ruleName}";
            if (this.label1 != null) this.label1.Text = $"Schedule for Rule: {_ruleName}"; // Assuming label1 is your title Label

            // The ComboBox cmbNotificationRule is NOT in your current designer, so no logic needed for it.

            if (btnAddSchedule != null)
            {
                btnAddSchedule.Text = "Save Schedule"; // Or "Update Schedule"
                btnAddSchedule.Click -= BtnSaveSchedule_Click; // Prevent multiple subscriptions
                btnAddSchedule.Click += BtnSaveSchedule_Click;
            }
            if (btnCancel != null)
            {
                btnCancel.Click -= BtnCancel_Click; // Prevent multiple subscriptions
                btnCancel.Click += BtnCancel_Click;
            }

            if (chkAll != null)
            {
                chkAll.CheckedChanged -= ChkAll_CheckedChanged; // Prevent multiple
                chkAll.CheckedChanged += ChkAll_CheckedChanged;
            }
            // Wire up individual day checkboxes if you want to uncheck 'chkAll' when one is manually unchecked
            WireIndividualDayCheckBoxEvents();
        }

        private void AddNotificationSchedule_Load(object sender, EventArgs e)
        {
            LoadExistingOrDefaultSchedule();
        }

        private void WireIndividualDayCheckBoxEvents()
        {
            CheckBox[] dayCheckBoxes = { chkMon, chkTue, chkWed, chkThur, chkFri, chkSat, chkSun };
            foreach (var chkDay in dayCheckBoxes)
            {
                if (chkDay != null)
                {
                    chkDay.CheckedChanged -= DayCheckBox_CheckedChanged; // Prevent multiple
                    chkDay.CheckedChanged += DayCheckBox_CheckedChanged;
                }
            }
        }

        private void DayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAll != null && chkAll.Focused) return; // If chkAll caused this, ignore

            // If any individual day is unchecked, uncheck "All"
            if (sender is CheckBox changedDayChk && !changedDayChk.Checked)
            {
                if (chkAll != null)
                {
                    chkAll.CheckedChanged -= ChkAll_CheckedChanged; // Prevent loop
                    chkAll.Checked = false;
                    chkAll.CheckedChanged += ChkAll_CheckedChanged;
                }
            }
            else // Check if all days are now checked
            {
                UpdateChkAllState();
            }
        }

        private void UpdateChkAllState()
        {
            if (chkAll == null) return;

            bool allDaysChecked = true;
            CheckBox[] dayCheckBoxes = { chkMon, chkTue, chkWed, chkThur, chkFri, chkSat, chkSun };
            foreach (var chkDay in dayCheckBoxes)
            {
                if (chkDay != null && !chkDay.Checked)
                {
                    allDaysChecked = false;
                    break;
                }
                else if (chkDay == null) // If any day checkbox is missing, 'All' cannot be true
                {
                    allDaysChecked = false;
                    break;
                }
            }

            chkAll.CheckedChanged -= ChkAll_CheckedChanged; // Prevent loop
            chkAll.Checked = allDaysChecked;
            chkAll.CheckedChanged += ChkAll_CheckedChanged;
        }


        private void LoadExistingOrDefaultSchedule()
        {
            NotificationRule rule = _notificationRuleRepo.GetRuleById(_ruleId);
            if (rule != null && !string.IsNullOrWhiteSpace(rule.ScheduleDetailsJson))
            {
                try
                {
                    _currentScheduleData = JsonConvert.DeserializeObject<NotificationScheduleData>(rule.ScheduleDetailsJson);
                    if (_currentScheduleData == null) _currentScheduleData = new NotificationScheduleData(); // Ensure not null
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing ScheduleDetailsJson for rule ID {_ruleId}: {ex.Message}");
                    _currentScheduleData = new NotificationScheduleData();
                }
            }
            else
            {
                _currentScheduleData = new NotificationScheduleData();
                _currentScheduleData.StartDateTime = DateTime.Now;
                _currentScheduleData.IntervalSeconds = null;
                _currentScheduleData.TriggerTrueDurationSeconds = 0;
            }

            if (_currentScheduleData.StartDateTime < datePicker.MinDate) // Ensure value is within DateTimePicker range
                datePicker.Value = datePicker.MinDate;
            else if (_currentScheduleData.StartDateTime > datePicker.MaxDate)
                datePicker.Value = datePicker.MaxDate;
            else
                datePicker.Value = _currentScheduleData.StartDateTime.Date;

            timePicker.Value = _currentScheduleData.StartDateTime;

            txtInterval.Text = _currentScheduleData.IntervalSeconds?.ToString() ?? "";
            txtTriggerConTrueDuration.Text = _currentScheduleData.TriggerTrueDurationSeconds.ToString();

            if (_currentScheduleData.DaysOfWeek == null) // Corrected logic
            {
                _currentScheduleData.DaysOfWeek = new List<DayOfWeek>();
            }

            chkMon.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Monday);
            chkTue.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Tuesday);
            chkWed.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Wednesday);
            chkThur.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Thursday);
            chkFri.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Friday);
            chkSat.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Saturday);
            chkSun.Checked = _currentScheduleData.DaysOfWeek.Contains(DayOfWeek.Sunday);

            UpdateChkAllState(); // Update chkAll based on individual day checks
        }

        private void ChkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAll == null || !chkAll.Focused) return;

            bool isChecked = chkAll.Checked;

            Action<CheckBox> setCheck = (chk) => {
                if (chk != null)
                {
                    chk.CheckedChanged -= DayCheckBox_CheckedChanged;
                    chk.Checked = isChecked;
                    chk.CheckedChanged += DayCheckBox_CheckedChanged;
                }
            };

            setCheck(chkMon);
            setCheck(chkTue);
            setCheck(chkWed);
            setCheck(chkThur);
            setCheck(chkFri);
            setCheck(chkSat);
            setCheck(chkSun);
        }

        private void BtnSaveSchedule_Click(object sender, EventArgs e)
        {
            int? interval = null;
            if (!string.IsNullOrWhiteSpace(txtInterval.Text))
            {
                if (!int.TryParse(txtInterval.Text, out int parsedInterval) || parsedInterval < 0)
                {
                    MessageBox.Show("Interval must be a valid positive number (seconds) or empty for no recurrence.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInterval.Focus();
                    return;
                }
                interval = parsedInterval == 0 ? (int?)null : parsedInterval; // Treat 0 as no interval
            }

            if (!int.TryParse(txtTriggerConTrueDuration.Text, out int triggerDuration) || triggerDuration < 0)
            {
                MessageBox.Show("Trigger condition true duration must be a valid non-negative number (seconds).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTriggerConTrueDuration.Focus();
                return;
            }

            _currentScheduleData.StartDateTime = datePicker.Value.Date + timePicker.Value.TimeOfDay;
            _currentScheduleData.IntervalSeconds = interval;
            _currentScheduleData.TriggerTrueDurationSeconds = triggerDuration;

            _currentScheduleData.DaysOfWeek.Clear();
            if (chkMon.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Monday);
            if (chkTue.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Tuesday);
            if (chkWed.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Wednesday);
            if (chkThur.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Thursday);
            if (chkFri.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Friday);
            if (chkSat.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Saturday);
            if (chkSun.Checked) _currentScheduleData.DaysOfWeek.Add(DayOfWeek.Sunday);

            try
            {
                string scheduleJson = JsonConvert.SerializeObject(_currentScheduleData);
                _notificationRuleRepo.UpdateNotificationRuleSchedule(_ruleId, scheduleJson);
                _notificationRuleRepo.ResetRuleSchedule(_ruleId);
                MessageBox.Show("Schedule saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving schedule: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
