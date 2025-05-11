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

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddPingDeviceIP : Form
    {
        // Event to notify that a router has been saved.
        public event EventHandler RouterSaved;

        public AddPingDeviceIP()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddRouter_Click(object sender, EventArgs e)
        {

        }
    }
}
