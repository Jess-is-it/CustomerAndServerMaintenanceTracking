using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms;
using CustomerAndServerMaintenanceTracking.ModalForms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking
{
    public partial class SettingsMikrotikRouter : Form
    {
        private OverlayForm overlayForm;

        public SettingsMikrotikRouter()
        {
            InitializeComponent();
            InitializeRouterTab();
            LoadRouters();

            #region Event Handlers
            this.txtSearchRouter.TextChanged += new System.EventHandler(this.txtSearchRouter_TextChanged);
            this.btnAddRouter.Click += new System.EventHandler(this.btnAddRouter_Click);
            this.dataGridViewRouters.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewRouters_CellPainting);
            this.dataGridViewRouters.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewRouters_CellClick);
            #endregion
        }

        #region Overlay
        private void ShowOverlay()
        {
            if (overlayForm == null || overlayForm.IsDisposed)
            {
                overlayForm = new OverlayForm();
            }
            Form formToCover = this.MdiParent ?? this;
            overlayForm.Owner = formToCover;
            overlayForm.StartPosition = FormStartPosition.Manual;
            Point location = formToCover.PointToScreen(Point.Empty);
            overlayForm.Bounds = new Rectangle(location, formToCover.ClientSize);
            overlayForm.Show();
            overlayForm.BringToFront();
        }

        private void CloseOverlay()
        {
            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                overlayForm.Close();
                overlayForm = null;
            }
        }
        #endregion

        #region Router Management
        private void InitializeRouterTab()
        {
            dataGridViewRouters.Columns.Clear();
            dataGridViewRouters.AutoGenerateColumns = false;

            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Width = 50, Visible = false });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RouterName", HeaderText = "Router Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HostIPAddress", HeaderText = "Host IP Address", Width = 120 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ApiPort", HeaderText = "API Port", Width = 60 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", Width = 120 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Password", HeaderText = "Password", Width = 120 });
            dataGridViewRouters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", Name = "Action", ReadOnly = true, Width = 120 });
        }
        private void LoadRouters()
        {
            try
            {
                MikrotikRouterRepository repo = new MikrotikRouterRepository();
                var routers = repo.GetRouters();
                string search = txtSearchRouter.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(search) && routers != null)
                {
                    routers = routers.Where(r => r.RouterName != null && r.RouterName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                dataGridViewRouters.DataSource = null;
                dataGridViewRouters.DataSource = routers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading routers: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            addRouterForm.StartPosition = FormStartPosition.CenterParent;
            ShowOverlay();
            addRouterForm.ShowDialog(this);
            CloseOverlay();
        }
        private void dataGridViewRouters_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewRouters.Columns["Action"] != null && e.ColumnIndex == dataGridViewRouters.Columns["Action"].Index)
            {
                e.PaintBackground(e.ClipBounds, true);
                int buttonWidth = (e.CellBounds.Width - 6) / 2;
                if (buttonWidth < 10) buttonWidth = 10;
                int buttonHeight = e.CellBounds.Height - 4;
                if (buttonHeight < 5) buttonHeight = 5;

                Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", this.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }
        private void dataGridViewRouters_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridViewRouters.Rows[e.RowIndex].DataBoundItem is MikrotikRouter selectedRouter)
            {
                if (dataGridViewRouters.Columns["Action"] != null && e.ColumnIndex == dataGridViewRouters.Columns["Action"].Index)
                {
                    Rectangle cellRect = dataGridViewRouters.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    Point clickPoint = dataGridViewRouters.PointToClient(Cursor.Position);
                    int relativeX = clickPoint.X - cellRect.X;
                    int buttonWidth = (cellRect.Width - 6) / 2;
                    if (buttonWidth < 10) buttonWidth = (cellRect.Width - 2) / 2;

                    if (relativeX <= buttonWidth + 2)
                    {
                        EditRouter editRouterForm = new EditRouter(selectedRouter);
                        editRouterForm.RouterSaved += (s, ea) => LoadRouters();
                        editRouterForm.StartPosition = FormStartPosition.CenterParent;
                        ShowOverlay();
                        editRouterForm.ShowDialog(this);
                        CloseOverlay();
                    }
                    else
                    {
                        if (MessageBox.Show($"Are you sure you want to delete the router '{selectedRouter.RouterName}'?\nThis will also archive any customers associated with this router.", "Confirm Delete Router", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            try
                            {
                                CustomerRepository customerRepo = new CustomerRepository();
                                int customersArchived = customerRepo.ArchiveCustomersByRouterId(selectedRouter.Id);

                                if (customersArchived > 0)
                                {
                                    MessageBox.Show($"{customersArchived} customer(s) associated with router '{selectedRouter.RouterName}' have been archived.", "Customers Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                MikrotikRouterRepository routerRepo = new MikrotikRouterRepository();
                                routerRepo.DeleteRouter(selectedRouter.Id);

                                MessageBox.Show($"Router '{selectedRouter.RouterName}' deleted successfully.", "Router Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                LoadRouters();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error deleting router or archiving customers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                LoadRouters();
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}