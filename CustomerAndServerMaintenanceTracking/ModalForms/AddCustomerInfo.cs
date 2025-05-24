using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddCustomerInfo : Form
    {
        private OverlayForm overlayForm;


        public AddCustomerInfo()
        {
            InitializeComponent();
        }


        // This event handler should be wired to your Cancel button.
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
