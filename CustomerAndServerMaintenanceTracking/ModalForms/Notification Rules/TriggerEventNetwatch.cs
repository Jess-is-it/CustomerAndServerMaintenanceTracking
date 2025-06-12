using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Windows.Controls;
using Newtonsoft.Json;


namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class TriggerEventNetwatch : System.Windows.Forms.UserControl
    {
        private readonly int _netwatchId;
        private NetwatchConfigRepository _netwatchConfigRepository;
        private TagRepository _tagRepository;
        private ServiceLogRepository _logRepository;
        private List<NetwatchConfigDisplay> _allNetwatchConfigs;

        public TriggerEventNetwatch(int netwatchId) // netwatchId here is the NetwatchConfig.Id
        {
            InitializeComponent();
            this._netwatchId = netwatchId;

            _logRepository = new ServiceLogRepository();
            _tagRepository = new TagRepository();
            _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, _tagRepository);

            // Wire up event handlers - WE WILL MOVE chkListBoxNetwatch.ItemCheck to InitializeControls
            this.rbAll_IPs.CheckedChanged += new System.EventHandler(this.rbSpecification_CheckedChanged);
            this.rbSpecificIPs.CheckedChanged += new System.EventHandler(this.rbSpecification_CheckedChanged);
            // this.chkListBoxNetwatch.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chkListBoxNetwatch_ItemCheck); // REMOVE FROM HERE
            this.textSearchNetwatch.TextChanged += new System.EventHandler(this.textSearchNetwatch_TextChanged);
            this.txtSearchIPs.TextChanged += new System.EventHandler(this.txtSearchIPs_TextChanged);

            InitializeControls();
        }

        private void InitializeControls()
        {
            LoadNetwatchConfigurations();
            PopulateStatusOptions();
            UpdateIpSelectionGroupVisibility();

            this.chkListBoxNetwatch.ItemCheck -= chkListBoxNetwatch_ItemCheck;
            this.chkListBoxNetwatch.ItemCheck += chkListBoxNetwatch_ItemCheck;
        }

        private void LoadNetwatchConfigurations(string searchTerm = "")
        {
            if (chkListBoxNetwatch == null) return;

            // --- TEMPORARILY UNSUBSCRIBE ---
            this.chkListBoxNetwatch.ItemCheck -= chkListBoxNetwatch_ItemCheck;

            _allNetwatchConfigs = _netwatchConfigRepository.GetNetwatchConfigsForDisplay();

            chkListBoxNetwatch.Items.Clear();

            IEnumerable<NetwatchConfigDisplay> configsToDisplay = _allNetwatchConfigs;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                configsToDisplay = _allNetwatchConfigs.Where(nc =>
                    nc.NetwatchName.ToLowerInvariant().Contains(searchTerm.ToLowerInvariant()) ||
                    (nc.TargetSourceName != null && nc.TargetSourceName.ToLowerInvariant().Contains(searchTerm.ToLowerInvariant()))
                );
            }

            bool itemWasCheckedProgrammatically = false;
            if (this._netwatchId > 0)
            {
                var specificConfig = _allNetwatchConfigs.FirstOrDefault(c => c.Id == this._netwatchId);
                if (specificConfig != null)
                {
                    chkListBoxNetwatch.Items.Add(specificConfig, true); // This line was triggering the early ItemCheck
                    itemWasCheckedProgrammatically = true;
                    // chkListBoxNetwatch.Enabled = false; 
                    // textSearchNetwatch.Enabled = false;
                }
                else
                {
                    chkListBoxNetwatch.Items.Add($"Netwatch Config ID {this._netwatchId} not found.", false);
                }
            }
            else
            {
                foreach (var config in configsToDisplay)
                {
                    chkListBoxNetwatch.Items.Add(config);
                }
            }

            chkListBoxNetwatch.DisplayMember = "NetwatchName"; // Set these after populating
            chkListBoxNetwatch.ValueMember = "Id";

            // --- RESUBSCRIBE ---
            this.chkListBoxNetwatch.ItemCheck += chkListBoxNetwatch_ItemCheck;

            // --- MANUALLY TRIGGER IP LOAD IF NEEDED AFTER PROGRAMMATIC CHECK ---
            if (itemWasCheckedProgrammatically && rbSpecificIPs.Checked)
            {
                // Since ItemCheck was off, we need to manually call LoadIPsForSelectedNetwatchConfigs
                // if an item was checked programmatically and specific IPs should be shown.
                LoadIPsForSelectedNetwatchConfigs();
            }
            else if (rbSpecificIPs.Checked && chkListBoxNetwatch.CheckedItems.Count > 0)
            {
                // Also covers the case where search filters and a previously checked item remains
                LoadIPsForSelectedNetwatchConfigs();
            }
        }

        private void PopulateStatusOptions()
        {
            if (chkListBoxNetwatchStatus == null) return;

            chkListBoxNetwatchStatus.Items.Clear();
            // Add common Netwatch statuses. You might want to fetch these from a predefined list or enum.
            chkListBoxNetwatchStatus.Items.Add("All Up", false);
            chkListBoxNetwatchStatus.Items.Add("Partial Up", false);
            chkListBoxNetwatchStatus.Items.Add("All Down", false);
            chkListBoxNetwatchStatus.Items.Add("Timeout", false); // Specific timeout status
            chkListBoxNetwatchStatus.Items.Add("No IP", false);
            chkListBoxNetwatchStatus.Items.Add("Error", false); // General error status
                                                                // Add any other relevant statuses
        }
        private void rbSpecification_CheckedChanged(object sender, EventArgs e)
        {
            UpdateIpSelectionGroupVisibility();
        }

        private void UpdateIpSelectionGroupVisibility()
        {
            if (grpBoxIPs != null)
            {
                grpBoxIPs.Visible = rbSpecificIPs.Checked;
                if (rbSpecificIPs.Checked)
                {
                    // If "Specific IPs" is selected, load the IPs for the currently checked Netwatch config(s).
                    LoadIPsForSelectedNetwatchConfigs();
                }
                else
                {
                    // Clear IP list if "All IPs" is selected
                    if (chkListBoxNetwatchIPs != null) chkListBoxNetwatchIPs.Items.Clear();
                    if (txtSearchIPs != null) txtSearchIPs.Clear();
                }
            }
        }

        private void LoadIPsForSelectedNetwatchConfigs(string ipSearchTerm = "")
        {
            if (chkListBoxNetwatchIPs == null || !rbSpecificIPs.Checked)
            {
                if (chkListBoxNetwatchIPs != null) chkListBoxNetwatchIPs.Items.Clear();
                return;
            }

            chkListBoxNetwatchIPs.Items.Clear();
            List<IndividualIpStatus> uniqueIpDetails = new List<IndividualIpStatus>();
            HashSet<string> addedIps = new HashSet<string>(); // To track unique IPs

            // Iterate through checked Netwatch configurations
            foreach (var item in chkListBoxNetwatch.CheckedItems)
            {
                if (item is NetwatchConfigDisplay selectedConfig)
                {
                    List<IndividualIpStatus> statuses = _netwatchConfigRepository.GetDetailedIpStatuses(selectedConfig.Id);
                    if (statuses != null)
                    {
                        foreach (var status in statuses)
                        {
                            // Add to list only if IP is not null and not already added
                            if (!string.IsNullOrWhiteSpace(status.IpAddress) && addedIps.Add(status.IpAddress))
                            {
                                // Apply search term here before adding if needed
                                if (string.IsNullOrWhiteSpace(ipSearchTerm) ||
                                    (status.IpAddress != null && status.IpAddress.ToLowerInvariant().Contains(ipSearchTerm.ToLowerInvariant())) ||
                                    (status.EntityName != null && status.EntityName.ToLowerInvariant().Contains(ipSearchTerm.ToLowerInvariant())))
                                {
                                    uniqueIpDetails.Add(status);
                                }
                            }
                        }
                    }
                }
            }

            // Sort before adding to listbox for consistent order
            uniqueIpDetails = uniqueIpDetails.OrderBy(ip => ip.EntityName).ThenBy(ip => ip.IpAddress).ToList();

            foreach (var ipDetail in uniqueIpDetails)
            {
                // Display format: "EntityName (IPAddress)" or just "IPAddress" if EntityName is N/A or same as IP
                string displayText = string.IsNullOrWhiteSpace(ipDetail.EntityName) || ipDetail.EntityName.Equals("N/A", StringComparison.OrdinalIgnoreCase) || ipDetail.EntityName == ipDetail.IpAddress
                                   ? ipDetail.IpAddress
                                   : $"{ipDetail.EntityName} ({ipDetail.IpAddress})";
                chkListBoxNetwatchIPs.Items.Add(new ComboboxItem { Text = displayText, Value = ipDetail.IpAddress });
            }
            if (chkListBoxNetwatchIPs.Items.Count > 0) chkListBoxNetwatchIPs.DisplayMember = "Text";

        }

        private void chkListBoxNetwatch_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (rbSpecificIPs.Checked)
                    {
                        LoadIPsForSelectedNetwatchConfigs(txtSearchIPs.Text);
                    }
                });
            }
            else
            {
                // Handle is not created yet. This scenario should be less likely now
                // due to unsubscribing during programmatic changes.
                // If it still occurs, you might queue the action or perform it synchronously
                // if you are sure about the context (but that defeats the purpose of BeginInvoke).
                // For now, logging it might be best if it ever happens.
                Console.WriteLine("chkListBoxNetwatch_ItemCheck: Handle not created, BeginInvoke skipped.");
                // Potentially, if rbSpecificIPs.Checked, directly call:
                // if (rbSpecificIPs.Checked)
                // {
                //     LoadIPsForSelectedNetwatchConfigs(txtSearchIPs.Text);
                // }
                // However, be cautious with direct UI updates from here if the state is unstable.
            }
        }

        private void textSearchNetwatch_TextChanged(object sender, EventArgs e)
        {
            LoadNetwatchConfigurations(textSearchNetwatch.Text);
        }

        private void txtSearchIPs_TextChanged(object sender, EventArgs e)
        {
            if (rbSpecificIPs.Checked)
            {
                LoadIPsForSelectedNetwatchConfigs(txtSearchIPs.Text);
            }
        }

        // Helper class for CheckedListBox items if you need to store Value separately from Text
        private class ComboboxItem
        {
            public string Text { get; set; }
            public object Value { get; set; }
            public override string ToString()
            {
                return Text;
            }
        }

        // Method to get selected Netwatch Config IDs
        public List<int> GetSelectedNetwatchConfigIds()
        {
            return chkListBoxNetwatch.CheckedItems.OfType<NetwatchConfigDisplay>().Select(cfg => cfg.Id).ToList();
        }

        // Method to get selected IP Addresses (only if rbSpecificIPs is checked)
        public List<string> GetSelectedIpAddresses()
        {
            if (!rbSpecificIPs.Checked)
            {
                return new List<string>(); // Or null, or an indicator for "all IPs"
            }
            return chkListBoxNetwatchIPs.CheckedItems.OfType<ComboboxItem>().Select(item => item.Value.ToString()).ToList();
        }

        // Method to get selected statuses
        public List<string> GetSelectedStatuses()
        {
            return chkListBoxNetwatchStatus.CheckedItems.OfType<string>().ToList();
        }

        public bool IsAllIpsSelected()
        {
            return rbAll_IPs.Checked;
        }

        public void LoadStateFromJson(string triggerDetailsJson)
        {
            if (string.IsNullOrWhiteSpace(triggerDetailsJson) || triggerDetailsJson == "{}")
            {
                // Set to default state if no valid JSON
                rbAll_IPs.Checked = true; // Or your preferred default
                                          // Clear list boxes
                foreach (int i in chkListBoxNetwatch.CheckedIndices) chkListBoxNetwatch.SetItemChecked(i, false);
                chkListBoxNetwatchIPs.Items.Clear();
                foreach (int i in chkListBoxNetwatchStatus.CheckedIndices) chkListBoxNetwatchStatus.SetItemChecked(i, false);
                return;
            }

            try
            {
                var details = JsonConvert.DeserializeObject<NetwatchTriggerDetails>(triggerDetailsJson);
                if (details == null) return;

                // Load Netwatch Config selections
                // This requires chkListBoxNetwatch to be populated first by LoadNetwatchConfigurations()
                // Ensure LoadNetwatchConfigurations() is called before LoadStateFromJson() if it isn't already.
                for (int i = 0; i < chkListBoxNetwatch.Items.Count; i++)
                {
                    if (chkListBoxNetwatch.Items[i] is NetwatchConfigDisplay item)
                    {
                        bool shouldBeChecked = details.SelectedNetwatchConfigIds?.Contains(item.Id) ?? false;
                        if (chkListBoxNetwatch.GetItemChecked(i) != shouldBeChecked) // Only change if different to avoid event loops
                        {
                            chkListBoxNetwatch.SetItemChecked(i, shouldBeChecked);
                        }
                    }
                }

                rbAll_IPs.Checked = details.TriggerOnAllIPs;
                rbSpecificIPs.Checked = !details.TriggerOnAllIPs; // This will trigger visibility update for grpBoxIPs

                if (rbSpecificIPs.Checked)
                {
                    // LoadIPsForSelectedNetwatchConfigs() should have been called due to rbSpecificIPs.Checked change.
                    // Now, check the specific IPs from the saved state.
                    // This assumes chkListBoxNetwatchIPs contains ComboboxItem with Value as IP string.
                    for (int i = 0; i < chkListBoxNetwatchIPs.Items.Count; i++)
                    {
                        if (chkListBoxNetwatchIPs.Items[i] is ComboboxItem ipItem) // Using the helper class
                        {
                            bool shouldBeChecked = details.SelectedSpecificIPs?.Contains(ipItem.Value.ToString()) ?? false;
                            if (chkListBoxNetwatchIPs.GetItemChecked(i) != shouldBeChecked)
                            {
                                chkListBoxNetwatchIPs.SetItemChecked(i, shouldBeChecked);
                            }
                        }
                    }
                }

                // Load Status selections
                for (int i = 0; i < chkListBoxNetwatchStatus.Items.Count; i++)
                {
                    string statusItem = chkListBoxNetwatchStatus.Items[i].ToString();
                    bool shouldBeChecked = details.SelectedNetwatchStatuses?.Contains(statusItem) ?? false;
                    if (chkListBoxNetwatchStatus.GetItemChecked(i) != shouldBeChecked)
                    {
                        chkListBoxNetwatchStatus.SetItemChecked(i, shouldBeChecked);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading trigger state into TriggerEventNetwatch: {ex.Message}");
                MessageBox.Show("Could not fully load saved trigger conditions. Please review.", "Load Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



    }
}

