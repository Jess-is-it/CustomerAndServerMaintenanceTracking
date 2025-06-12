using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class SelectRecipientAddEmailOrPhone : Form
    {
        private bool _isEmailMode;
        public List<string> FinalContacts { get; private set; }

        public SelectRecipientAddEmailOrPhone(bool isEmail, List<string> existingContacts = null)
        {
            InitializeComponent();
            _isEmailMode = isEmail;
            FinalContacts = new List<string>();

            if (_isEmailMode)
            {
                this.labelTitle.Text = "Manage Email Addresses";
                this.groupBox1.Text = "Enter Email Addresses (comma, semicolon, or space separated)";
                this.btnInserEmailorPhone.Text = "Save Emails";
            }
            else
            {
                this.labelTitle.Text = "Manage Phone Numbers";
                this.groupBox1.Text = "Enter Phone Numbers (11 digits, comma/semicolon/space separated)";
                this.btnInserEmailorPhone.Text = "Save Phones";
            }

            if (existingContacts != null && existingContacts.Any())
            {
                txtEmailorPhone.Text = string.Join(", ", existingContacts);
            }

            btnInserEmailorPhone.Click += BtnInserEmailorPhone_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        }

        private void BtnInserEmailorPhone_Click(object sender, EventArgs e)
        {
            string inputText = txtEmailorPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputText))
            {
                FinalContacts.Clear();
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            var rawEntries = inputText.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(c => c.Trim())
                                   .Where(c => !string.IsNullOrWhiteSpace(c))
                                   .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            List<string> validatedEntries = new List<string>();
            List<string> invalidEntries = new List<string>();

            foreach (string entry in rawEntries)
            {
                if (_isEmailMode)
                {
                    if (Regex.IsMatch(entry, @"^[^@\s]+@[^@\s]+\.[^@\s\.]+$") && entry.Contains(".")) validatedEntries.Add(entry);
                    else invalidEntries.Add(entry);
                }
                else
                {
                    string numericEntry = new string(entry.Where(char.IsDigit).ToArray());
                    if (numericEntry.Length == 11) validatedEntries.Add(numericEntry);
                    else invalidEntries.Add(entry);
                }
            }

            if (invalidEntries.Any())
            {
                string entryType = _isEmailMode ? "email addresses" : "phone numbers";
                string message = $"The following {entryType} are invalid and were not included:\n\n" + string.Join("\n", invalidEntries);
                if (validatedEntries.Any()) message += $"\n\nValid entries will be saved.";
                else
                {
                    message += $"\n\nPlease correct them. No valid entries were found to save.";
                    MessageBox.Show(message, "Validation Issues", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(message, "Validation Issues", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            FinalContacts = validatedEntries;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public List<string> GetContacts() { return FinalContacts; }
    }
}
