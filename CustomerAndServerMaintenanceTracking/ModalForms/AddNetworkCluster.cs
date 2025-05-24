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
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class AddNetworkCluster : Form
    {

        public event EventHandler ClusterAdded;
        public AddNetworkCluster()
        {
            InitializeComponent();
        }

        private void btnAddCluster_Click(object sender, EventArgs e)
        {
            // Validate input.
            if (string.IsNullOrWhiteSpace(txtClusterName.Text))
            {
                MessageBox.Show("Please enter a cluster name.");
                return;
            }

            NetworkCluster newCluster = new NetworkCluster
            {
                ClusterName = txtClusterName.Text.Trim(),
                ClusterDescription = txtClusterDescription.Text.Trim()
            };

            NetworkClusterRepository repo = new NetworkClusterRepository();
            repo.AddCluster(newCluster);

            ClusterAdded?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
