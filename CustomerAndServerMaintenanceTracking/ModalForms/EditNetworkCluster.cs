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
    public partial class EditNetworkCluster : Form
    {

        private NetworkCluster currentCluster;
        public event EventHandler ClusterUpdated;

        public EditNetworkCluster(NetworkCluster cluster)
        {
            InitializeComponent();
            currentCluster = cluster;

            // Pre-fill fields with the existing cluster data
            txtClusterName.Text = currentCluster.ClusterName;
            txtClusterDescription.Text = currentCluster.ClusterDescription;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnUpdateCluster_Click(object sender, EventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtClusterName.Text))
            {
                MessageBox.Show("Please enter a cluster name.");
                return;
            }

            // Update the cluster object
            currentCluster.ClusterName = txtClusterName.Text.Trim();
            currentCluster.ClusterDescription = txtClusterDescription.Text.Trim();

            // Call repository to update in DB
            NetworkClusterRepository repo = new NetworkClusterRepository();
            repo.UpdateCluster(currentCluster);

            // Fire event to let parent form refresh
            ClusterUpdated?.Invoke(this, EventArgs.Empty);

            this.Close();
        }
    }
}
