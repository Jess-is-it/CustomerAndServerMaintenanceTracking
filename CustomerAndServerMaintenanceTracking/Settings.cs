using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using CustomerAndServerMaintenanceTracking.ModalForms;
using CustomerAndServerMaintenanceTracking.AppLogs; // For ServiceLogs form
using System.ServiceProcess; // For ServiceController
using System.Configuration;
using CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms;    // For ConfigurationManager

namespace CustomerAndServerMaintenanceTracking
{
    public partial class Settings : Form
    {
        private OverlayForm overlayForm;
        private System.Windows.Forms.Timer _serviceStatusRefreshTimer;

        // These MUST match the actual registered Windows Service names
        private const string ActualNetwatchPingerServiceName = "NetwatchPingerService";
        private const string ActualPPPoESyncServiceName = "PPPoESyncService";

        private readonly string _targetMachineName;
        private UserRoleRepository _userRoleRepository;
        private UserAccountRepository _userAccountRepository;
        private ServiceLogRepository _serviceLogRepositoryForSettings;
        private EmailSettingsRepository _emailSettingsRepository;

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

        public Settings()
        {
            InitializeComponent();
            InitializeRouterTab();
            LoadRouters();


            try
            {
                _serviceLogRepositoryForSettings = new ServiceLogRepository();
            }
            catch (Exception ex)
            {
                // Handle case where even the logger can't be initialized (e.g., config error for DB connection)
                MessageBox.Show($"Critical error initializing logger for Settings form: {ex.Message}\nSome logging will be unavailable.", "Logging Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                // Depending on how critical logging is, you might disable functionality or just proceed without logging.
            }

            #region EMAIL

            _emailSettingsRepository = new EmailSettingsRepository();
            InitializeEmailSettingsGrid();
            LoadEmailSettingsGrid();

            this.btnAddEmail.Click += new System.EventHandler(this.btnAddEmail_Click);
            #endregion
            #region ROLES
            // User Role Repository (already there)
            try
            {
                        _userRoleRepository = new UserRoleRepository();
                    }
                    catch (ConfigurationErrorsException configEx)
                    {
                        MessageBox.Show($"A critical configuration error occurred: {configEx.Message}\nPlease check your App.config file for the database connection string 'CustomerAndServerMaintenanceTracking'.\nThe application might not function correctly.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while initializing roles repository: {ex.Message}\nRoles functionality may be affected.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    // Initialize Roles Tab
                    if (_userRoleRepository != null)
                    {
                        InitializeRolesGrid();
                        LoadRolesData();
                    }
                    else
                    {
                        if (tabControl1.TabPages.Contains(tabPageUsers) && tabControl3.TabPages.Contains(tabPage5))
                        {
                            tabPage5.Enabled = false;
                            tabPage5.Text = "Roles (Error)";
                        }
                    }

                    // Wire up events for Roles tab (ensure these use the correct control names from your designer)
                    if (this.btnAddRole != null)
                    {
                        this.btnAddRole.Click -= this.btnAddRole_Click;
                        this.btnAddRole.Click += new System.EventHandler(this.btnAddRole_Click);
                    }
                    if (this.txtSearchAccountRoles != null)
                    {
                        this.txtSearchAccountRoles.TextChanged -= this.txtSearchAccountRoles_TextChanged;
                        this.txtSearchAccountRoles.TextChanged += new System.EventHandler(this.txtSearchAccountRoles_TextChanged);
                    }
                    if (this.dataGridView1 != null) // This is dataGridViewRoles
                    {
                        this.dataGridView1.CellPainting -= this.dataGridViewRoles_CellPainting;
                        this.dataGridView1.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewRoles_CellPainting);
                        this.dataGridView1.CellClick -= this.dataGridViewRoles_CellClick;
                        this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewRoles_CellClick);
                    }
            #endregion

            #region USERS

            // *** NEW: User Account Repository ***
            try
            {
                _userAccountRepository = new UserAccountRepository();
            }
            catch (ConfigurationErrorsException configEx) // Catch specific exception for connection string
            {
                MessageBox.Show($"A critical configuration error occurred initializing User Accounts: {configEx.Message}\nPlease check your App.config file.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Consider disabling user tab or showing error there
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while initializing user accounts repository: {ex.Message}\nUser accounts functionality may be affected.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // Initialize Users Tab
            if (_userAccountRepository != null)
            {
                InitializeActiveUsersGrid(); // Renamed
                InitializeInactiveUsersGrid(); // New method
                LoadActiveUsersData();       // Renamed
                LoadInactiveUsersData();     // New method
            }
            else
            {
                if (tabControl1.TabPages.Contains(tabPageUsers) && tabControl3.TabPages.Contains(tabPage4))
                {
                    tabPage4.Enabled = false; // tabPage4 is the "Users" sub-tab containing tabControlUserActiveInActive
                    tabPage4.Text = "Users (Error)";
                }
            }

            // Wire up events for Users tab
            if (this.btnAddUser != null)
            {
                this.btnAddUser.Click -= this.btnAddUser_Click;
                this.btnAddUser.Click += new System.EventHandler(this.btnAddUser_Click);
            }
            // Search for Active Users
            if (this.txtSearchUserListActive != null)
            {
                this.txtSearchUserListActive.TextChanged -= this.txtSearchUserListActive_TextChanged;
                this.txtSearchUserListActive.TextChanged += new System.EventHandler(this.txtSearchUserListActive_TextChanged);
            }
            // Search for Inactive Users
            if (this.txtSearchUserListInactive != null)
            {
                this.txtSearchUserListInactive.TextChanged -= this.txtSearchUserListInactive_TextChanged;
                this.txtSearchUserListInactive.TextChanged += new System.EventHandler(this.txtSearchUserListInactive_TextChanged);
            }

            // Event Handlers for Active Users Grid
            if (this.dataGridViewUsersActive != null)
            {
                this.dataGridViewUsersActive.CellPainting -= this.dataGridViewActiveUsers_CellPainting; // Use specific handler
                this.dataGridViewUsersActive.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewActiveUsers_CellPainting);
                this.dataGridViewUsersActive.CellClick -= this.dataGridViewActiveUsers_CellClick; // Use specific handler
                this.dataGridViewUsersActive.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewActiveUsers_CellClick);
                this.dataGridViewUsersActive.CellContentClick -= this.dataGridViewUsersActive_CellContentClick; // For checkbox
                this.dataGridViewUsersActive.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewUsersActive_CellContentClick);
                this.dataGridViewUsersActive.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridViewUsers_CellFormatting);

            }
            // Event Handlers for Inactive Users Grid
            if (this.dataGridViewUsersInactive != null)
            {
                this.dataGridViewUsersInactive.CellPainting -= this.dataGridViewInactiveUsers_CellPainting; // Use specific handler
                this.dataGridViewUsersInactive.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridViewInactiveUsers_CellPainting);
                this.dataGridViewUsersInactive.CellClick -= this.dataGridViewInactiveUsers_CellClick; // Use specific handler
                this.dataGridViewUsersInactive.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewInactiveUsers_CellClick);
                this.dataGridViewUsersInactive.CellContentClick -= this.dataGridViewUsersInactive_CellContentClick; // For checkbox
                this.dataGridViewUsersInactive.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewUsersInactive_CellContentClick);
                this.dataGridViewUsersInactive.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridViewUsers_CellFormatting);

            }

            #endregion

            _targetMachineName = ConfigurationManager.AppSettings["ServiceControlTargetMachine"];
            if (string.IsNullOrWhiteSpace(_targetMachineName))
            {
                _targetMachineName = ".";
            }


            #region Netwatch Manager
                    // Event handlers for View Logs buttons (Service Management Tab)
                    if (this.btnViewNetwatchService != null)
                        this.btnViewNetwatchService.Click += BtnViewNetwatchLogs_Click;
                    if (this.btnViewPPPoESyncService != null)
                        this.btnViewPPPoESyncService.Click += BtnViewPPPoESyncLogs_Click;

            #endregion
        }

        #region Router
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
                                // --- NEW CODE: Archive associated customers FIRST ---
                                CustomerRepository customerRepo = new CustomerRepository();
                                int customersArchived = customerRepo.ArchiveCustomersByRouterId(selectedRouter.Id);

                                if (customersArchived > 0)
                                {
                                    MessageBox.Show($"{customersArchived} customer(s) associated with router '{selectedRouter.RouterName}' have been archived.", "Customers Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                // --- END OF NEW CODE ---

                                // Now delete the router
                                MikrotikRouterRepository routerRepo = new MikrotikRouterRepository(); // Renamed 'repo' to 'routerRepo' for clarity
                                routerRepo.DeleteRouter(selectedRouter.Id);

                                MessageBox.Show($"Router '{selectedRouter.RouterName}' deleted successfully.", "Router Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                LoadRouters(); // Refresh the grid
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error deleting router or archiving customers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                // Optionally, re-load routers even on error to reflect partial changes if any, or current state.
                                LoadRouters();
                            }
                        }
                    }
                }
            }
        }
        #endregion


        private void Settings_Load(object sender, EventArgs e)
        {
            UpdateAllServiceStatusesAndButtons();

            _serviceStatusRefreshTimer = new System.Windows.Forms.Timer();
            _serviceStatusRefreshTimer.Interval = 10000; // Refresh every 10 seconds (adjust as needed)
            _serviceStatusRefreshTimer.Tick += ServiceStatusRefreshTimer_Tick;
            _serviceStatusRefreshTimer.Start();

            // Hide the Start/Stop buttons as per user request
            if (btnStartStopNetwatchService != null) btnStartStopNetwatchService.Visible = false;
            if (btnStartStopPPPoESyncService != null) btnStartStopPPPoESyncService.Visible = false;
        }
        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serviceStatusRefreshTimer?.Stop();
            _serviceStatusRefreshTimer?.Dispose();
        }
        private void ServiceStatusRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed || this.Disposing) return;
            UpdateAllServiceStatusesAndButtons();
        }
        private void UpdateAllServiceStatusesAndButtons()
        {
            if (this.IsDisposed || this.Disposing) return;
            UpdateSimplifiedServiceStatusUI(ActualNetwatchPingerServiceName, lblNetwatchService);
            UpdateSimplifiedServiceStatusUI(ActualPPPoESyncServiceName, lblPPPoESyncService);
        }
        private void UpdateSimplifiedServiceStatusUI(string serviceName, Label statusLabel)
        {
            if (statusLabel == null || statusLabel.IsDisposed) return;
            if (this.IsDisposed || this.Disposing) return;

            string statusText = "Unreachable - Check Server Services"; // Default
            Color statusColor = Color.OrangeRed; // Default for unreachable

            try
            {
                using (ServiceController sc = new ServiceController(serviceName, _targetMachineName))
                {
                    sc.Refresh(); // Important to get the latest status
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        statusText = "Running";
                        statusColor = Color.Green;
                    }
                    // For any other status (Stopped, Paused, Pending, Not Found), it's considered "Unreachable" for this simplified view
                }
            }
            catch (InvalidOperationException) // Service not found
            {
                // Status remains "Unreachable..."
            }
            catch (Exception ex)
            {
                // Status remains "Unreachable..."
                Console.WriteLine($"Error getting status for {serviceName} on '{_targetMachineName}': {ex.Message}");
            }
            finally
            {
                if (!statusLabel.IsDisposed)
                {
                    statusLabel.Text = $"Status: {statusText}";
                    statusLabel.ForeColor = statusColor;
                }
            }
        }

        #region Roles
        private void InitializeRolesGrid()
        {
            // Assuming dataGridView1 is your DataGridView for roles on tabPage5
            dataGridView1.Columns.Clear();
            dataGridView1.AutoGenerateColumns = false;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RoleId",
                DataPropertyName = "Id", // From UserRole model
                HeaderText = "ID",
                Visible = false
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RoleName",
                DataPropertyName = "RoleName", // From UserRole model
                HeaderText = "Role Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RoleDescription",
                DataPropertyName = "Description", // From UserRole model
                HeaderText = "Description",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RoleDateCreated",
                DataPropertyName = "DateCreated", // From UserRole model
                HeaderText = "Date Created",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            // Action column for Edit/Delete
            DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn
            {
                Name = "RoleActionColumn",
                HeaderText = "Action",
                ReadOnly = true,
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            dataGridView1.Columns.Add(actionColumn);

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true; // Make grid read-only, actions via buttons
        }
        private void LoadRolesData()
        {
            try
            {
                if (_userRoleRepository == null)
                {
                    MessageBox.Show("UserRoleRepository is not initialized. Cannot load roles.", "Internal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var roles = _userRoleRepository.GetRoles();
                if (roles == null) // Should not happen with current GetRoles() impl. but good check
                {
                    MessageBox.Show("Received no role data from the repository.", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    roles = new List<UserRole>(); // Ensure roles is not null for further processing
                }


                string searchTerm = string.Empty;
                if (txtSearchAccountRoles != null) // Check if the TextBox control itself exists
                {
                    // TextBox.Text returns empty string if null, so direct access is usually safe if control exists
                    searchTerm = txtSearchAccountRoles.Text.Trim().ToLowerInvariant();
                }
                else
                {
                    // This case should ideally not happen if the form is loaded correctly.
                    MessageBox.Show("Search box for roles is not available.", "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    roles = roles.Where(r =>
                                    (r.RoleName != null && r.RoleName.ToLowerInvariant().Contains(searchTerm)) ||
                                    (r.Description != null && r.Description.ToLowerInvariant().Contains(searchTerm)))
                                    .ToList();
                }

                if (dataGridView1 != null) // Check if the DataGridView control exists
                {
                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = roles;
                }
                else
                {
                    MessageBox.Show("Roles DataGridView (dataGridView1) is not available to display data.", "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (NullReferenceException nre) // Catch NullReferenceException specifically
            {
                MessageBox.Show($"Error loading user roles (Null Reference):\n{nre.Message}\n\nTrace:\n{nre.StackTrace}", "Load Error - Null Reference", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex) // Catch other exceptions
            {
                MessageBox.Show($"Error loading user roles:\n{ex.Message}\n\nTrace:\n{ex.StackTrace}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnAddRole_Click(object sender, EventArgs e)
        {
            // Ensure the namespace for AddUserRole is correct:
            // CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms.AddUserRole
            using (AddUserRole addUserRoleForm = new AddUserRole())
            {
                addUserRoleForm.RoleSaved += AddUserRoleForm_RoleSaved; // Subscribe to the event
                addUserRoleForm.StartPosition = FormStartPosition.CenterScreen;
                ShowOverlay(); // Reusing your existing Overlay methods
                addUserRoleForm.ShowDialog(this);
                CloseOverlay();

                addUserRoleForm.RoleSaved -= AddUserRoleForm_RoleSaved; // Unsubscribe
            }
        }
        private void AddUserRoleForm_RoleSaved(object sender, EventArgs e)
        {
            LoadRolesData(); // Refresh the grid when a new role is saved
        }
        private void txtSearchAccountRoles_TextChanged(object sender, EventArgs e)
        {
            LoadRolesData();
        }
        private void dataGridViewRoles_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Columns["RoleActionColumn"] != null && e.ColumnIndex == dataGridView1.Columns["RoleActionColumn"].Index)
            {
                e.PaintBackground(e.ClipBounds, true);
                int buttonWidth = (e.CellBounds.Width - 6) / 2; // Adjusted for padding/spacing
                if (buttonWidth < 10) buttonWidth = 10;
                int buttonHeight = e.CellBounds.Height - 4;
                if (buttonHeight < 5) buttonHeight = 5;

                int xStart = e.CellBounds.X + 2;
                int yStart = e.CellBounds.Y + 2;

                Rectangle editButtonRect = new Rectangle(xStart, yStart, buttonWidth, buttonHeight);
                Rectangle deleteButtonRect = new Rectangle(xStart + buttonWidth + 4, yStart, buttonWidth, buttonHeight);

                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", dataGridView1.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", dataGridView1.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                e.Handled = true;
            }
        }
        private void dataGridViewRoles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dataGridView1.Columns["RoleActionColumn"] == null || e.ColumnIndex != dataGridView1.Columns["RoleActionColumn"].Index)
                return;

            if (dataGridView1.Rows[e.RowIndex].DataBoundItem is UserRole selectedRoleData) // This is just RoleName, Description etc.
            {
                Rectangle cellRect = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dataGridView1.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;
                if (buttonWidth < 10) buttonWidth = (cellRect.Width - 2) / 2;

                if (relativeX <= buttonWidth + 2) // Clicked on Edit
                {
                    // Fetch the full role details including permissions
                    UserRole roleWithPermissions = _userRoleRepository.GetRoleByIdWithPermissions(selectedRoleData.Id);
                    if (roleWithPermissions == null)
                    {
                        MessageBox.Show("Could not retrieve role details for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (AddUserRole editUserRoleForm = new AddUserRole(roleWithPermissions)) // Pass the full role object
                    {
                        editUserRoleForm.RoleSaved += AddUserRoleForm_RoleSaved; // Re-use the same handler
                        editUserRoleForm.StartPosition = FormStartPosition.CenterParent;
                        ShowOverlay();
                        editUserRoleForm.ShowDialog(this);
                        CloseOverlay();

                        editUserRoleForm.RoleSaved -= AddUserRoleForm_RoleSaved;
                    }
                }
                else // Clicked on Delete
                {
                    if (MessageBox.Show($"Are you sure you want to delete the role '{selectedRoleData.RoleName}'?",
                                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            bool deleted = _userRoleRepository.DeleteRole(selectedRoleData.Id);
                            if (deleted)
                            {
                                MessageBox.Show($"Role '{selectedRoleData.RoleName}' deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadRolesData(); // Refresh the grid
                            }
                            else
                            {
                                MessageBox.Show($"Could not delete role '{selectedRoleData.RoleName}'. It might have already been deleted.", "Not Deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (InvalidOperationException ex) // Specifically for "role in use"
                        {
                            MessageBox.Show(ex.Message, "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting role: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        #endregion

        #region Users
        #region Active Users
        // In Settings.cs, within #region Users

        private void dataGridViewUsers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // Check if this is the "UserRoleName" column and a valid data row
            if (dgv.Columns[e.ColumnIndex].Name == "UserRoleName" && e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count)
            {
                // Get the UserAccount object for the current row
                if (dgv.Rows[e.RowIndex].DataBoundItem is UserAccount userAccount)
                {
                    if (userAccount.Role != null && !string.IsNullOrEmpty(userAccount.Role.RoleName))
                    {
                        e.Value = userAccount.Role.RoleName;
                    }
                    else if (userAccount.Role != null && string.IsNullOrEmpty(userAccount.Role.RoleName))
                    {
                        e.Value = "N/A (Role Name Empty)"; // Should ideally not happen if data is clean
                    }
                    else
                    {
                        e.Value = "N/A (No Role Object)"; // Should ideally not happen
                    }
                    e.FormattingApplied = true; // Indicate that we've handled the formatting for this cell
                }
            }
        }
        private void InitializeActiveUsersGrid()
        {
            if (dataGridViewUsersActive == null) return;
            AddUserGridColumns(dataGridViewUsersActive, isActiveGrid: true);

            if (dataGridViewUsersActive == null) return;

            dataGridViewUsersActive.Columns.Clear();
            dataGridViewUsersActive.AutoGenerateColumns = false;

            // Add common columns (ID, FullName, Username, Email, Phone, Role, DateCreated)
            AddUserGridColumns(dataGridViewUsersActive, isActiveGrid: true);

            //// Specific "Active" checkbox column for this grid
            //DataGridViewCheckBoxColumn activeColumn = new DataGridViewCheckBoxColumn
            //{
            //    Name = "UserIsActive",
            //    DataPropertyName = "IsActive",
            //    HeaderText = "Active", // Header for this grid
            //    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            //    ReadOnly = false // Allow user to click it
            //};
            //dataGridViewUsersActive.Columns.Insert(dataGridViewUsersActive.Columns.Count - 1, activeColumn); // Insert before Action

            dataGridViewUsersActive.AllowUserToAddRows = false;
            // dataGridViewUsersActive.ReadOnly = true; // Most cells are read-only, checkbox is not.
        }
        private void LoadActiveUsersData()
        {
            if (_userAccountRepository == null || dataGridViewUsersActive == null) return;
            try
            {
                var allUsers = _userAccountRepository.GetUserAccountsWithRoles();
                var activeUsers = allUsers.Where(u => u.IsActive).ToList();

                string searchTerm = txtSearchUserListActive.Text.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    activeUsers = activeUsers.Where(u =>
                                    (u.FullName != null && u.FullName.ToLowerInvariant().Contains(searchTerm)) ||
                                    (u.Username != null && u.Username.ToLowerInvariant().Contains(searchTerm)) ||
                                    (u.Email != null && u.Email.ToLowerInvariant().Contains(searchTerm)) ||
                                    (u.Role?.RoleName != null && u.Role.RoleName.ToLowerInvariant().Contains(searchTerm)))
                                 .ToList();
                }
                dataGridViewUsersActive.DataSource = null;
                dataGridViewUsersActive.DataSource = activeUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading active user accounts: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void txtSearchUserListActive_TextChanged(object sender, EventArgs e)
        {
            LoadActiveUsersData();
        }
        private void dataGridViewActiveUsers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Re-use or adapt dataGridViewUsers_CellPainting logic if column name is "UserActionColumn"
            PaintUserActionButtons(sender as DataGridView, e, "UserActionColumn");
        }
        private void dataGridViewActiveUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleUserActionCellClick(dataGridViewUsersActive, e);
           
        }
        private void dataGridViewUsersActive_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewUsersActive == null || e.RowIndex < 0 ||
        dataGridViewUsersActive.Columns[e.ColumnIndex].Name != "UserIsActiveCol")
                return;

            dataGridViewUsersActive.CommitEdit(DataGridViewDataErrorContexts.Commit);

            if (dataGridViewUsersActive.Rows[e.RowIndex].DataBoundItem is UserAccount selectedUser)
            {
                if (!selectedUser.IsActive) // User is trying to DEACTIVATE
                {
                    using (DeactivationReasonForm reasonForm = new DeactivationReasonForm(selectedUser.Username))
                    {
                        ShowOverlay();
                        DialogResult dialogResult = reasonForm.ShowDialog(this);
                        CloseOverlay();

                        if (dialogResult == DialogResult.OK)
                        {
                            string reason = reasonForm.Reason;

                            // *** SET THE REASON ON THE USER OBJECT ***
                            selectedUser.DeactivationReason = reason;

                            // Log the reason
                            _serviceLogRepositoryForSettings?.WriteLog(new ServiceLogEntry
                            {
                                ServiceName = "SettingsForm.UserManagement",
                                LogLevel = SharedLibrary.Models.LogLevel.INFO.ToString(), // Use enum from SharedLibrary
                                Message = $"User '{selectedUser.Username}' deactivated. Reason: {reason}"
                            });
                            Console.WriteLine($"Deactivation reason for {selectedUser.Username}: {reason}");

                            selectedUser.IsActive = false;
                            if (_userAccountRepository.UpdateUserAccount(selectedUser))
                            {
                                MessageBox.Show($"User '{selectedUser.Username}' deactivated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Failed to deactivate user.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            LoadActiveUsersData(); // Refresh both grids
                            LoadInactiveUsersData();
                        }
                        else
                        {
                            LoadActiveUsersData(); // Revert checkbox by reloading data
                        }
                    }
                }
            }
        }
        #endregion

        #region Inactive Users
        private void InitializeInactiveUsersGrid()
        {
            if (dataGridViewUsersInactive == null) return;

            dataGridViewUsersInactive.Columns.Clear();
            dataGridViewUsersInactive.AutoGenerateColumns = false;

            AddUserGridColumns(dataGridViewUsersInactive, isActiveGrid: false);

            // Specific "Active" (but showing inactive users) checkbox column
            DataGridViewCheckBoxColumn activeColumn = new DataGridViewCheckBoxColumn
            {
                Name = "UserIsActive", // Keep DataPropertyName same for binding UserAccount.IsActive
                DataPropertyName = "IsActive",
                HeaderText = "Activate", // Header indicates action to take
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                ReadOnly = false // Allow user to click it
            };
            // Style it to appear as an "activate" action - this might involve CellFormatting later
            dataGridViewUsersInactive.Columns.Insert(dataGridViewUsersInactive.Columns.Count - 1, activeColumn);

            dataGridViewUsersInactive.AllowUserToAddRows = false;
            // dataGridViewUsersInactive.ReadOnly = true;
        }
        private void LoadInactiveUsersData()
        {
            if (dataGridViewUsersInactive == null) return;
            AddUserGridColumns(dataGridViewUsersInactive, isActiveGrid: false); // Pass false


            if (_userAccountRepository == null || dataGridViewUsersInactive == null) return;
            try
            {
                var allUsers = _userAccountRepository.GetUserAccountsWithRoles();
                var inactiveUsers = allUsers.Where(u => !u.IsActive).ToList();

                string searchTerm = txtSearchUserListInactive.Text.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    inactiveUsers = inactiveUsers.Where(u =>
                                    (u.FullName != null && u.FullName.ToLowerInvariant().Contains(searchTerm)) ||
                                    (u.Username != null && u.Username.ToLowerInvariant().Contains(searchTerm)) ||
                                    (u.Email != null && u.Email.ToLowerInvariant().Contains(searchTerm)) ||
                                    (u.Role?.RoleName != null && u.Role.RoleName.ToLowerInvariant().Contains(searchTerm)))
                                 .ToList();
                }
                dataGridViewUsersInactive.DataSource = null;
                dataGridViewUsersInactive.DataSource = inactiveUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inactive user accounts: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void txtSearchUserListInactive_TextChanged(object sender, EventArgs e)
        {
            LoadInactiveUsersData();
        }
        private void dataGridViewInactiveUsers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            PaintUserActionButtons(sender as DataGridView, e, "UserActionColumn");
        }
        private void dataGridViewInactiveUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleUserActionCellClick(dataGridViewUsersInactive, e);
        }
        private void dataGridViewUsersInactive_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewUsersInactive == null || e.RowIndex < 0 ||
        dataGridViewUsersInactive.Columns[e.ColumnIndex].Name != "UserIsActiveCol")
                return;

            dataGridViewUsersInactive.CommitEdit(DataGridViewDataErrorContexts.Commit);

            if (dataGridViewUsersInactive.Rows[e.RowIndex].DataBoundItem is UserAccount selectedUser)
            {
                if (selectedUser.IsActive) // User is trying to ACTIVATE (checkbox is now checked)
                {
                    if (MessageBox.Show($"Are you sure you want to activate user '{selectedUser.Username}'?",
                                        "Confirm Activation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        selectedUser.IsActive = true;
                        selectedUser.DeactivationReason = null; // *** CLEAR THE REASON UPON ACTIVATION ***

                        if (_userAccountRepository.UpdateUserAccount(selectedUser))
                        {
                            MessageBox.Show($"User '{selectedUser.Username}' activated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to activate user.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        LoadActiveUsersData(); // Refresh both grids
                        LoadInactiveUsersData();
                    }
                    else
                    {
                        LoadInactiveUsersData(); // Revert checkbox by reloading data
                    }
                }
            }
        }
        #endregion


        private void AddUserGridColumns(DataGridView dgv, bool isActiveGrid)
        {
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserRoleName", HeaderText = "Role", DataPropertyName = "Role.RoleName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserFullName", DataPropertyName = "FullName", HeaderText = "Full Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25 });
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserUsername", DataPropertyName = "Username", HeaderText = "Username", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 20 });
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserEmail", DataPropertyName = "Email", HeaderText = "Email", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserPhoneNumber", DataPropertyName = "PhoneNumber", HeaderText = "Phone", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            //dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserDateCreated", DataPropertyName = "DateCreated", HeaderText = "Date Created", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });

            //DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn { Name = "UserActionColumn", HeaderText = "Action", ReadOnly = true, Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            //dgv.Columns.Add(actionColumn);
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserRoleName", HeaderText = "Role", DataPropertyName = "Role.RoleName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserFullName", DataPropertyName = "FullName", HeaderText = "Full Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserUsername", DataPropertyName = "Username", HeaderText = "Username", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, FillWeight = 20 });

            // *** ADD DeactivationReason column ONLY FOR INACTIVE GRID ***
            if (!isActiveGrid)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "UserDeactivationReason",
                    DataPropertyName = "DeactivationReason",
                    HeaderText = "Reason for Deactivation",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 30 // Give it some space
                });
            }

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserEmail", DataPropertyName = "Email", HeaderText = "Email", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserPhoneNumber", DataPropertyName = "PhoneNumber", HeaderText = "Phone", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserDateCreated", DataPropertyName = "DateCreated", HeaderText = "Date Created", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserLastLogin", DataPropertyName = "LastLoginDate", HeaderText = "Last Login", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
            
            //Active Column
            DataGridViewCheckBoxColumn activeColumn = new DataGridViewCheckBoxColumn
            {
                Name = "UserIsActiveCol",
                DataPropertyName = "IsActive",
                HeaderText = isActiveGrid ? "Active" : "Activate",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                ReadOnly = false
            };
            dgv.Columns.Add(activeColumn);
            //dataGridViewUsersActive.Columns.Insert(dataGridViewUsersActive.Columns.Count - 1, activeColumn);

            DataGridViewTextBoxColumn actionColumn = new DataGridViewTextBoxColumn { Name = "UserActionColumn", HeaderText = "Action", ReadOnly = true, Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dgv.Columns.Add(actionColumn);

            dgv.AllowUserToAddRows = false;
        }    
        private void btnAddUser_Click(object sender, EventArgs e)
                {
                    // Ensure the namespace is correct for AddUserAccount
                    using (AddUserAccount addUserAccountForm = new AddUserAccount())
                    {
                        addUserAccountForm.Owner = this;
                        addUserAccountForm.UserAccountSaved += AddUserAccountForm_UserAccountSaved;
                        ShowOverlay();
                        addUserAccountForm.ShowDialog(this);
                        CloseOverlay();
                        addUserAccountForm.UserAccountSaved -= AddUserAccountForm_UserAccountSaved;
                    }
                }
        private void AddUserAccountForm_UserAccountSaved(object sender, EventArgs e)
                {
                    LoadActiveUsersData(); // Refresh active users primarily
                    LoadInactiveUsersData(); // Also refresh inactive in case an edit changed status
                }
        private void PaintUserActionButtons(DataGridView dgv, DataGridViewCellPaintingEventArgs e, string actionColumnName)
                {
                    if (dgv != null && e.RowIndex >= 0 && dgv.Columns[actionColumnName] != null && e.ColumnIndex == dgv.Columns[actionColumnName].Index)
                    {
                        e.PaintBackground(e.ClipBounds, true);
                        int buttonWidth = (e.CellBounds.Width - 6) / 2;
                        if (buttonWidth < 10) buttonWidth = 10;
                        int buttonHeight = e.CellBounds.Height - 4;
                        if (buttonHeight < 5) buttonHeight = 5;

                        Rectangle editButtonRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, buttonWidth, buttonHeight);
                        Rectangle deleteButtonRect = new Rectangle(e.CellBounds.X + buttonWidth + 4, e.CellBounds.Y + 2, buttonWidth, buttonHeight);

                        ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", dgv.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                        ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", dgv.Font, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                        e.Handled = true;
                    }
                }
        private void HandleUserActionCellClick(DataGridView dgv, DataGridViewCellEventArgs e)
        {
            if (dgv == null || e.RowIndex < 0 || dgv.Columns["UserActionColumn"] == null || e.ColumnIndex != dgv.Columns["UserActionColumn"].Index)
                return;

            if (dgv.Rows[e.RowIndex].DataBoundItem is UserAccount selectedUser)
            {
                Rectangle cellRect = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dgv.PointToClient(Cursor.Position);
                int relativeX = clickPoint.X - cellRect.X;
                int buttonWidth = (cellRect.Width - 6) / 2;
                if (buttonWidth < 10) buttonWidth = (cellRect.Width - 2) / 2;

                if (relativeX <= buttonWidth + 2) // Clicked on Edit
                {
                    UserAccount userToEdit = _userAccountRepository.GetUserAccountById(selectedUser.Id);
                    if (userToEdit == null)
                    {
                        MessageBox.Show("Could not retrieve user details for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    using (AddUserAccount editUserForm = new AddUserAccount(userToEdit))
                    {
                        editUserForm.UserAccountSaved += AddUserAccountForm_UserAccountSaved;
                        editUserForm.StartPosition = FormStartPosition.CenterScreen;
                        ShowOverlay();
                        editUserForm.ShowDialog(this);
                        CloseOverlay();
                        editUserForm.UserAccountSaved -= AddUserAccountForm_UserAccountSaved;
                    }
                }
                else // Clicked on Delete
                {
                    if (MessageBox.Show($"Are you sure you want to delete the user '{selectedUser.Username}' ({selectedUser.FullName})?",
                                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            bool deleted = _userAccountRepository.DeleteUserAccount(selectedUser.Id);
                            if (deleted)
                            {
                                MessageBox.Show($"User '{selectedUser.Username}' deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadActiveUsersData();
                                LoadInactiveUsersData();
                            }
                            else
                            {
                                MessageBox.Show($"Could not delete user '{selectedUser.Username}'.", "Not Deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        #endregion Users

        #region Netwatch Manager
        // Event Handlers for View Logs Buttons (remain unchanged)
        private void BtnViewNetwatchLogs_Click(object sender, EventArgs e)
        {
            ServiceLogs logsForm = new ServiceLogs(ActualNetwatchPingerServiceName, "Netwatch Pinger Service");
            logsForm.ShowDialog(this);
        }
        private void BtnViewPPPoESyncLogs_Click(object sender, EventArgs e)
        {
            ServiceLogs logsForm = new ServiceLogs(ActualPPPoESyncServiceName, "PPPoE Sync Service");
            logsForm.ShowDialog(this);
        }
        #endregion

        #region Email Management
        private void InitializeEmailSettingsGrid()
        {
            // Use the correct name of your DataGridView from the designer
            dgvEmail.Columns.Clear();
            dgvEmail.AutoGenerateColumns = false;

            // Add columns
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsDefault", DataPropertyName = "IsDefault", HeaderText = "Default", Width = 60 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SettingName", DataPropertyName = "SettingName", HeaderText = "Setting Name", Width = 150 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SmtpServer", DataPropertyName = "SmtpServer", HeaderText = "SMTP Server", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SmtpPort", DataPropertyName = "SmtpPort", HeaderText = "Port", Width = 50 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SenderEmail", DataPropertyName = "SenderEmail", HeaderText = "Sender Email", Width = 200 });
            dgvEmail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", ReadOnly = true, Width = 200 });

            // Set grid properties
            dgvEmail.AllowUserToAddRows = false;
            dgvEmail.ReadOnly = true;
            dgvEmail.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEmail.MultiSelect = false;

            // Wire up the new grid events
            dgvEmail.CellPainting += dgvEmail_CellPainting;
            dgvEmail.CellClick += dgvEmail_CellClick;
        }

        private void LoadEmailSettingsGrid()
        {
            try
            {
                var settingsList = _emailSettingsRepository.GetAllEmailSettings();
                dgvEmail.DataSource = null;
                if (settingsList != null)
                {
                    dgvEmail.DataSource = settingsList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load email settings from the database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddEmail_Click(object sender, EventArgs e)
        {
            using (var addForm = new AddEmail())
            {
                addForm.StartPosition = FormStartPosition.CenterParent;
                if (addForm.ShowDialog(this) == DialogResult.OK)
                {
                    LoadEmailSettingsGrid(); // Refresh the grid
                }
            }
        }

        private void dgvEmail_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvEmail.Columns["Action"].Index)
                return;

            e.PaintBackground(e.ClipBounds, true);

            // --- Start of Fix ---

            // A single, smaller font for all actions (buttons and text)
            Font actionFont = new Font(dgvEmail.Font.FontFamily, 8f);

            try
            {
                // Use the same layout calculations as before
                int padding = 4;
                int smallButtonWidth = 50;
                int largeButtonWidth = e.CellBounds.Width - (smallButtonWidth * 2) - (padding * 4);
                int topOffset = e.CellBounds.Top + 2;
                int buttonHeight = e.CellBounds.Height - 4;

                var setting = dgvEmail.Rows[e.RowIndex].DataBoundItem as EmailSettings;
                if (setting == null) return;

                // Action 1: "Default" / "Set as Default" (as plain black text)
                string defaultActionText = setting.IsDefault ? "Default" : "Set as Default";
                Rectangle textRect = new Rectangle(e.CellBounds.X + padding, topOffset, largeButtonWidth, buttonHeight);

                // Use TextRenderer to draw plain black text.
                TextRenderer.DrawText(e.Graphics, defaultActionText, actionFont, textRect,
                                      Color.Black, // Set text color to black
                                      TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);


                // Action 2: "Edit" Button
                Rectangle editButtonRect = new Rectangle(textRect.Right + padding, topOffset, smallButtonWidth, buttonHeight);
                ButtonRenderer.DrawButton(e.Graphics, editButtonRect, "Edit", actionFont, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);

                // Action 3: "Delete" Button
                Rectangle deleteButtonRect = new Rectangle(editButtonRect.Right + padding, topOffset, smallButtonWidth, buttonHeight);
                ButtonRenderer.DrawButton(e.Graphics, deleteButtonRect, "Delete", actionFont, false, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
            }
            finally
            {
                actionFont?.Dispose();
            }

            // --- End of Fix ---

            e.Handled = true;
        }

        private void dgvEmail_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvEmail.Columns["Action"].Index)
                return;

            var setting = dgvEmail.Rows[e.RowIndex].DataBoundItem as EmailSettings;
            if (setting == null) return;

            Rectangle cellBounds = dgvEmail.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            Point mousePosition = dgvEmail.PointToClient(Cursor.Position);
            Point clickPointInCell = new Point(mousePosition.X - cellBounds.Left, mousePosition.Y - cellBounds.Top);

            // --- Start of Fix ---

            // 1. Update rectangle calculations to match the new layout in CellPainting
            int padding = 4;
            int smallButtonWidth = 50;
            int largeButtonWidth = cellBounds.Width - (smallButtonWidth * 2) - (padding * 4);
            int topMargin = 2; // Use the same reduced margin
            int buttonHeight = cellBounds.Height - (topMargin * 2);

            Rectangle defaultActionRect = new Rectangle(padding, topMargin, largeButtonWidth, buttonHeight);
            Rectangle editButtonRect = new Rectangle(defaultActionRect.Right + padding, topMargin, smallButtonWidth, buttonHeight);
            Rectangle deleteButtonRect = new Rectangle(editButtonRect.Right + padding, topMargin, smallButtonWidth, buttonHeight);

            // --- End of Fix ---

            if (defaultActionRect.Contains(clickPointInCell))
            {
                if (setting.IsDefault) return;
                try
                {
                    _emailSettingsRepository.SetDefaultEmailSetting(setting.Id);
                    LoadEmailSettingsGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to set default: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (editButtonRect.Contains(clickPointInCell))
            {
                using (var editForm = new AddEmail(setting))
                {
                    editForm.StartPosition = FormStartPosition.CenterParent;
                    if (editForm.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadEmailSettingsGrid();
                    }
                }
            }
            else if (deleteButtonRect.Contains(clickPointInCell))
            {
                if (setting.IsDefault)
                {
                    MessageBox.Show($"Cannot delete the default setting '{setting.SettingName}'. Please set another as default first.", "Cannot Delete Default", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show($"Are you sure you want to delete '{setting.SettingName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        _emailSettingsRepository.DeleteEmailSetting(setting.Id);
                        LoadEmailSettingsGrid();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion


    }
}
