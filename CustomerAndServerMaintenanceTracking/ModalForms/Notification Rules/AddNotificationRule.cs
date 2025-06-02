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

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class AddNotificationRule : Form
    {
        private readonly int _netwatchId;
        private readonly string _netwatchName;

        public AddNotificationRule(int netwatchId, string netwatchName)
        {
            InitializeComponent();
            _netwatchId = netwatchId;
            _netwatchName = netwatchName;

            // STEP 2: Set the title label to "Add Notification: [NetwatchName]" in blue
            lblTitle.Text = $"Add Notification: {_netwatchName}";
            lblTitle.ForeColor = Color.DodgerBlue;

            // STEP 3: Determine the feature context (always "Netwatch" here)
            // and load the TriggerEventNetwatch control into panelTriggerConDisplay.
            //LoadTriggerEventControl();
        }

        /// <summary>
        /// STEP 3 IMPLEMENTATION:
        /// Creates and docks the TriggerEventNetwatch control into panelTriggerConDisplay.
        /// This control handles everything related to selecting Netwatch trigger conditions.
        /// </summary>

    }
}


