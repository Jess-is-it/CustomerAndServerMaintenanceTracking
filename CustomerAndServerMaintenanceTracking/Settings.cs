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
using CustomerAndServerMaintenanceTracking.ModalForms;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class Settings: Form
    {
        private OverlayForm overlayForm;
        public Settings()
        {
            InitializeComponent();
            InitializeRouterTab();
            LoadRouters();
        }
        private void InitializeRouterTab()
        {
            // Assuming you have a TabPage for Router settings,
            // and on that TabPage, a DataGridView named dataGridViewRouters,
            // a TextBox named txtSearchRouter, and a Button named btnAddRouter.

            dataGridViewRouters.Columns.Clear();
            dataGridViewRouters.AutoGenerateColumns = false;

            // (1) ID column
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.DataPropertyName = "Id";
            idColumn.HeaderText = "ID";
            idColumn.Width = 50;
            dataGridViewRouters.Columns.Add(idColumn);

            // (2) Router Name column
            DataGridViewTextBoxColumn routerNameColumn = new DataGridViewTextBoxColumn();
            routerNameColumn.DataPropertyName = "RouterName";
            routerNameColumn.HeaderText = "Router Name";
            routerNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewRouters.Columns.Add(routerNameColumn);

            // (3) Host IP Address column
            DataGridViewTextBoxColumn hostIPColumn = new DataGridViewTextBoxColumn();
            hostIPColumn.DataPropertyName = "HostIPAddress";
            hostIPColumn.HeaderText = "Host IP Address";
            hostIPColumn.Width = 120;

            // (New) API Port column
            DataGridViewTextBoxColumn apiPortColumn = new DataGridViewTextBoxColumn();
            apiPortColumn.DataPropertyName = "ApiPort"; // this must match the property in MikrotikRouter
            apiPortColumn.HeaderText = "API Port";
            apiPortColumn.Width = 60; // adjust width as needed
            dataGridViewRouters.Columns.Add(apiPortColumn);


            // (4) Username column
            DataGridViewTextBoxColumn usernameColumn = new DataGridViewTextBoxColumn();
            usernameColumn.DataPropertyName = "Username";
            usernameColumn.HeaderText = "Username";
            usernameColumn.Width = 120;
            dataGridViewRouters.Columns.Add(usernameColumn);

            // (5) Password column
            DataGridViewTextBoxColumn passwordColumn = new DataGridViewTextBoxColumn();
            passwordColumn.DataPropertyName = "Password";
            passwordColumn.HeaderText = "Password";
            passwordColumn.Width = 120;
            dataGridViewRouters.Columns.Add(passwordColumn);

            // (6) Action column (unbound) for Edit/Delete buttons
            DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn();
            actionColumn.HeaderText = "Action";
            actionColumn.Name = "Action";
            actionColumn.ReadOnly = true;
            dataGridViewRouters.Columns.Add(actionColumn);
        }

        private void LoadRouters()
        {
            try
            {
                MikrotikRouterRepository repo = new MikrotikRouterRepository();
                var routers = repo.GetRouters();
                string search = txtSearchRouter.Text.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    routers = routers.Where(r => r.RouterName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                dataGridViewRouters.DataSource = null;
                dataGridViewRouters.DataSource = routers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading routers: " + ex.Message);
            }
        }

        private void txtSearchRouter_TextChanged(object sender, EventArgs e)
        {
            LoadRouters();
        }

        private void btnAddRouter_Click(object sender, EventArgs e)
        {
            AddRouter addRouterForm = new AddRouter();
            addRouterForm.RouterSaved += (s, ea) => LoadRouters();
            addRouterForm.StartPosition = FormStartPosition.CenterScreen;
            Overlay();
            addRouterForm.ShowDialog();
            overlayForm.Close();
        }

        private void Overlay()
        {
            overlayForm = new OverlayForm();
            overlayForm.StartPosition = FormStartPosition.Manual;
            // Set the overlay to cover the entire Dashboard form.
            // If Dashboard is your main form, "this" refers to it.
            overlayForm.Bounds = this.Bounds;
            overlayForm.Show();
        }

        private void dataGridViewRouters_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex == dataGridViewRouters.Columns["Action"].Index && e.RowIndex >= 0)
            {
                e.PaintBackground(e.ClipBounds, true);
                // Calculate button dimensions.
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                int buttonHeight = e.CellBounds.Height - 4;
                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                // Draw the "Edit" button.
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                // Draw the "Delete" button.
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }

        private void dataGridViewRouters_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0)
                return;

            // Get the selected router object.
            MikrotikRouter selectedRouter = (MikrotikRouter)dataGridViewRouters.Rows[e.RowIndex].DataBoundItem;
            string columnName = dataGridViewRouters.Columns[e.ColumnIndex].Name;

            if (columnName == "Action")
            {
                // Get the bounds of the Action cell.
                Rectangle cellRect = dataGridViewRouters.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridViewRouters.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;

                if (relativeX < buttonWidth + 2)
                {
                    // Edit button clicked: open EditRouter form.
                    EditRouter editRouterForm = new EditRouter(selectedRouter);
                    editRouterForm.RouterSaved += (s, ea) => LoadRouters();
                    editRouterForm.StartPosition = FormStartPosition.CenterScreen;
                    editRouterForm.ShowDialog();
                }
                else
                {
                    // Delete button clicked: confirm and delete.
                    if (MessageBox.Show("Are you sure you want to delete this router?", "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        MikrotikRouterRepository repo = new MikrotikRouterRepository();
                        repo.DeleteRouter(selectedRouter.Id);
                        LoadRouters();
                    }
                }
            }
        }

        private void btnSavePPPoeInterval_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtPPPoeInterval.Text.Trim(), out int interval))
            {
                SyncSettingsRepository repo = new SyncSettingsRepository();
                repo.SaveInterval("PPPoeInterval", interval);
                MessageBox.Show("PPPoe Accounts sync interval saved to the database.");

                // Update the running timer's interval in the main form.
                Dashboard mainForm = Application.OpenForms["Dashboard"] as Dashboard;
                if (mainForm != null)
                {
                    mainForm.UpdatePPPoeTimerInterval();
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid number for the interval.");
            }
        }

 

        private void Settings_Load(object sender, EventArgs e)
        {
            SyncSettingsRepository repo = new SyncSettingsRepository();

            // Retrieve the sync intervals from the database
            int pppoeInterval = repo.GetInterval("PPPoeInterval");
            int activeInterval = repo.GetInterval("PPPActiveInterval");

            // If no value is stored (or it's 0), set default values (e.g., 60000 ms)
            if (pppoeInterval <= 0)
                pppoeInterval = 60000;
            if (activeInterval <= 0)
                activeInterval = 60000;

            // Display the intervals in the text boxes
            txtPPPoeInterval.Text = pppoeInterval.ToString();
        }
    }
}
