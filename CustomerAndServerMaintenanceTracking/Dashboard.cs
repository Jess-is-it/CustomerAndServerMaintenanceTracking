using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class Dashboard: Form
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void customerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if the form is already open; if so, focus it.
            foreach (Form child in this.MdiChildren)
            {
                if (child is Customers)
                {
                    child.Activate();
                    return;
                }
            }

            // Create a new instance of the Customers form and set its MdiParent.
            Customers customersForm = new Customers();
            customersForm.MdiParent = this;
            customersForm.Show();
        }
    }
}
