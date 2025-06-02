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
using CustomerAndServerMaintenanceTracking.Configuration;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.ModalForms.SettingsForms
{
    public partial class AddUserRole : Form
    {
        public event EventHandler RoleSaved;
        private UserRoleRepository _userRoleRepository;
        private UserRole _editingRole;
        private bool _isProgrammaticallyChecking = false; // Flag to prevent event re-entrancy

        // Default constructor for ADD mode
        public AddUserRole()
        {
            InitializeComponent();
            _userRoleRepository = new UserRoleRepository();
            SetupFormText(isEditMode: false);
            WireEvents();
        }

        // Constructor for EDIT mode
        public AddUserRole(UserRole roleToEdit) : this()
        {
            _editingRole = roleToEdit ?? throw new ArgumentNullException(nameof(roleToEdit));
            SetupFormText(isEditMode: true);
        }

        private void WireEvents()
        {
            this.btnAddUserRole.Click -= this.btnAddUserRole_Click;
            this.btnCancel.Click -= this.btnCancel_Click;
            this.Load -= this.AddUserRole_Load;
            this.txtSearch.TextChanged -= this.txtSearch_TextChanged;

            this.btnAddUserRole.Click += new System.EventHandler(this.btnAddUserRole_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.Load += new System.EventHandler(this.AddUserRole_Load);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.treeViewPermissions.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewPermissions_AfterCheck);
        }

        // --- Methods from previous step (SetupFormText, LoadRoleDataForEdit, AddUserRole_Load, PopulatePermissionsTree, etc. ---
        // --- Make sure PopulatePermissionsTree and Search logic correctly handle node cloning if they rebuild the tree ---
        // --- The key change will be in treeViewPermissions_AfterCheck and its helpers ---

        private void SetupFormText(bool isEditMode)
        {
            if (isEditMode)
            {
                this.Text = "Edit User Role";
                this.label1.Text = "Edit User Role";
                this.btnAddUserRole.Text = "Update Role";
            }
            else
            {
                this.Text = "Add User Role";
                this.label1.Text = "Add User Role";
                this.btnAddUserRole.Text = "Add Role";
            }
        }

        private void LoadRoleDataForEdit()
        {
            if (_editingRole == null) return;
            txtRoleName.Text = _editingRole.RoleName; // Assuming txtFullName was renamed to txtRoleName
            txtDescription.Text = _editingRole.Description;
            CheckPermissionsInTree(_editingRole.PermissionKeys);
        }

        private void AddUserRole_Load(object sender, EventArgs e)
        {
            PopulatePermissionsTree(); // This builds the tree
            treeViewPermissions.CheckBoxes = true;
            if (_editingRole != null)
            {
                LoadRoleDataForEdit(); // This checks nodes based on existing permissions
            }
        }

        private void PopulatePermissionsTree()
        {
            treeViewPermissions.Nodes.Clear();
            _originalNodes = null;
            List<TreeNode> permissionNodes = PermissionDefinitions.GetPermissionTree();
            foreach (TreeNode node in permissionNodes)
            {
                treeViewPermissions.Nodes.Add(node);
            }
            treeViewPermissions.ExpandAll();
        }

        private void CheckPermissionsInTree(List<string> permissionKeys)
        {
            if (permissionKeys == null || !permissionKeys.Any()) return;
            _isProgrammaticallyChecking = true; // Prevent AfterCheck from firing for these initial checks
            Queue<TreeNode> nodesToProcess = new Queue<TreeNode>();
            foreach (TreeNode rootNode in treeViewPermissions.Nodes)
            {
                nodesToProcess.Enqueue(rootNode);
            }
            while (nodesToProcess.Count > 0)
            {
                TreeNode currentNode = nodesToProcess.Dequeue();
                if (currentNode.Tag is string permissionKey && permissionKeys.Contains(permissionKey))
                {
                    currentNode.Checked = true;
                }
                foreach (TreeNode childNode in currentNode.Nodes)
                {
                    nodesToProcess.Enqueue(childNode);
                }
            }
            _isProgrammaticallyChecking = false;
        }

        // --- New TreeView Checkbox Logic ---
        private void treeViewPermissions_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_isProgrammaticallyChecking) return; // Prevent re-entrancy

            _isProgrammaticallyChecking = true; // Set flag

            TreeNode node = e.Node;
            bool isChecked = node.Checked;

            // 1. Parent affects children
            if (node.Nodes.Count > 0) // If it's a parent node
            {
                CheckAllChildNodes(node, isChecked);
            }

            // 2. Children affect parent
            if (node.Parent != null)
            {
                UpdateParentNodeState(node.Parent);
            }

            // 3. Smart Dependencies
            ApplySmartDependencies(node, isChecked);

            _isProgrammaticallyChecking = false; // Clear flag
        }

        private void CheckAllChildNodes(TreeNode parentNode, bool isChecked)
        {
            foreach (TreeNode childNode in parentNode.Nodes)
            {
                if (childNode.Checked != isChecked)
                {
                    childNode.Checked = isChecked; // This will trigger AfterCheck for child, but _isProgrammaticallyChecking will prevent loop
                }
                // No explicit recursive call to ApplySmartDependencies here, as the child's AfterCheck will handle its own dependencies.
            }
        }

        private void UpdateParentNodeState(TreeNode parentNode)
        {
            if (parentNode == null) return;

            bool allChildrenChecked = true;
            // bool anyChildChecked = false; // For indeterminate state if you ever implement it

            if (parentNode.Nodes.Count > 0) // Only update if it has children
            {
                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    if (!childNode.Checked)
                    {
                        allChildrenChecked = false;
                        // break; // No need to continue if one is unchecked for 'allChildrenChecked'
                    }
                    // if(childNode.Checked) anyChildChecked = true;
                }

                if (parentNode.Checked != allChildrenChecked)
                {
                    parentNode.Checked = allChildrenChecked; // This will trigger AfterCheck for parent
                }
            }
            // Recursively update grandparent if parent's state changed
            // The parentNode.Checked = allChildrenChecked will re-trigger AfterCheck for the parent,
            // which will then call UpdateParentNodeState for its own parent.
        }

        private void ApplySmartDependencies(TreeNode changedNode, bool isChecked)
        {
            string permissionKey = changedNode.Tag as string;
            if (string.IsNullOrEmpty(permissionKey)) return;

            // Example: If "Edit Device IP" is checked, ensure "View Device IP List" and its parents are checked.
            if (isChecked)
            {
                if (permissionKey == PermissionDefinitions.DEVICE_IP_EDIT ||
                    permissionKey == PermissionDefinitions.DEVICE_IP_ADD ||
                    permissionKey == PermissionDefinitions.DEVICE_IP_DELETE)
                {
                    CheckNodeByKey(PermissionDefinitions.DEVICE_IP_VIEW_LIST, true);
                    CheckNodeByKey(PermissionDefinitions.NAV_IP_DEVICE_IP_LIST, true); // Access to the form itself
                    CheckNodeByKey(PermissionDefinitions.NAV_IP_MANAGEMENT, true);     // Access to the main "IP" menu
                }
                // Add more "if checked" dependencies here
                if (permissionKey == PermissionDefinitions.NETWATCH_CONFIG_ADD ||
                   permissionKey == PermissionDefinitions.NETWATCH_CONFIG_TOGGLE_ENABLE ||
                   permissionKey == PermissionDefinitions.NETWATCH_CONFIG_DELETE ||
                   permissionKey == PermissionDefinitions.NETWATCH_CONFIG_VIEW_DETAILS)
                {
                    CheckNodeByKey(PermissionDefinitions.NETWATCH_LIST_VIEW_ALL, true); // Prerequisite to see the list
                    // This implies NAV_TOOLS_NETWATCH_LIST and NAV_TOOLS should also be checked
                    CheckNodeByKey(PermissionDefinitions.NAV_TOOLS_NETWATCH_LIST, true);
                    CheckNodeByKey(PermissionDefinitions.NAV_TOOLS, true);
                }
                if (permissionKey == PermissionDefinitions.NETWORK_CLUSTER_ARRANGE_HIERARCHY ||
                    permissionKey == PermissionDefinitions.NETWORK_CLUSTER_ADD ||
                    permissionKey == PermissionDefinitions.NETWORK_CLUSTER_EDIT ||
                    permissionKey == PermissionDefinitions.NETWORK_CLUSTER_DELETE)
                {
                    CheckNodeByKey(PermissionDefinitions.NETWORK_CLUSTER_VIEW_LIST, true);
                    CheckNodeByKey(PermissionDefinitions.NAV_NETWORK_CLUSTER, true);
                }
                // If any specific tag permission is checked, ensure main Tags nav is checked
                if (permissionKey.StartsWith("TAGS_") && permissionKey != PermissionDefinitions.NAV_TAGS)
                {
                    CheckNodeByKey(PermissionDefinitions.NAV_TAGS, true);
                }


            }
            else // If a node is UNCHECKED
            {
                // Example: If "View Device IP List" is unchecked, uncheck dependent actions like "Edit Device IP".
                if (permissionKey == PermissionDefinitions.DEVICE_IP_VIEW_LIST)
                {
                    CheckNodeByKey(PermissionDefinitions.DEVICE_IP_EDIT, false);
                    CheckNodeByKey(PermissionDefinitions.DEVICE_IP_ADD, false);
                    CheckNodeByKey(PermissionDefinitions.DEVICE_IP_DELETE, false);
                }
                if (permissionKey == PermissionDefinitions.NAV_IP_DEVICE_IP_LIST) // If cannot access the form
                {
                    CheckNodeByKey(PermissionDefinitions.DEVICE_IP_VIEW_LIST, false); // Also uncheck view, add, edit, delete
                }

                // If "View Netwatch List" is unchecked, uncheck specific actions within it
                if (permissionKey == PermissionDefinitions.NETWATCH_LIST_VIEW_ALL)
                {
                    CheckNodeByKey(PermissionDefinitions.NETWATCH_CONFIG_TOGGLE_ENABLE, false);
                    CheckNodeByKey(PermissionDefinitions.NETWATCH_CONFIG_DELETE, false);
                    CheckNodeByKey(PermissionDefinitions.NETWATCH_CONFIG_VIEW_DETAILS, false);
                    // Note: NETWATCH_CONFIG_ADD is under NAV_TOOLS_NETWATCH_ADD, so it's a separate consideration
                }
                if (permissionKey == PermissionDefinitions.NETWORK_CLUSTER_VIEW_LIST)
                {
                    CheckNodeByKey(PermissionDefinitions.NETWORK_CLUSTER_ARRANGE_HIERARCHY, false);
                    CheckNodeByKey(PermissionDefinitions.NETWORK_CLUSTER_ADD, false);
                    CheckNodeByKey(PermissionDefinitions.NETWORK_CLUSTER_EDIT, false);
                    CheckNodeByKey(PermissionDefinitions.NETWORK_CLUSTER_DELETE, false);
                }


                // If a main navigation item is unchecked, uncheck its specific functional permissions
                if (permissionKey == PermissionDefinitions.NAV_TAGS)
                {
                    CheckNodeByKey(PermissionDefinitions.TAGS_VIEW_CUSTOMER_TAB, false);
                    CheckNodeByKey(PermissionDefinitions.TAGS_VIEW_DEVICEIP_TAB, false);
                    // Add others like ADD, EDIT, DELETE, ASSIGN for tags if they are direct children or handled by their parent tab being unchecked.
                }
            }
        }

        private void CheckNodeByKey(string keyToFind, bool checkState)
        {
            TreeNode nodeToChange = FindNodeByKey(treeViewPermissions.Nodes, keyToFind);
            if (nodeToChange != null && nodeToChange.Checked != checkState)
            {
                nodeToChange.Checked = checkState; // This will re-trigger AfterCheck, handled by _isProgrammaticallyChecking
            }
        }

        private TreeNode FindNodeByKey(TreeNodeCollection nodes, string keyToFind)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is string permissionKey && permissionKey == keyToFind)
                {
                    return node;
                }
                TreeNode foundInChild = FindNodeByKey(node.Nodes, keyToFind);
                if (foundInChild != null)
                {
                    return foundInChild;
                }
            }
            return null;
        }

        // --- Rest of your methods (GetCheckedPermissions, Search, Save, Cancel, etc.) ---
        // Make sure the GetCheckedPermissions method iterates correctly if you want only leaf node permissions
        // or all checked node permissions. The current GetCheckedPermissions gets all checked nodes' tags.

        // ... (btnCancel_Click, btnAddUserRole_Click, _originalNodes, txtSearch_TextChanged, FilterNode, GetCheckedPermissions from previous step) ...
        // Ensure your btnAddUserRole_Click correctly uses the (potentially renamed) txtRoleName and new txtDescription
        private void GetCheckedPermissions(TreeNodeCollection nodes, List<string> checkedPermissions)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is string permissionKey)
                {
                    if (!string.IsNullOrWhiteSpace(permissionKey))
                    {
                        checkedPermissions.Add(permissionKey);
                    }
                }
                // If you only want leaf permissions, you'd check node.Nodes.Count == 0 here.
                // For now, collecting all checked node tags is fine.
                if (node.Nodes.Count > 0)
                {
                    GetCheckedPermissions(node.Nodes, checkedPermissions);
                }
            }
        }

        private List<TreeNode> _originalNodes = null;

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_originalNodes == null && treeViewPermissions.Nodes.Count > 0)
            {
                _originalNodes = new List<TreeNode>();
                foreach (TreeNode n in treeViewPermissions.Nodes)
                {
                    _originalNodes.Add((TreeNode)n.Clone());
                }
            }
            string filterText = txtSearch.Text.Trim().ToLower();
            treeViewPermissions.BeginUpdate();
            treeViewPermissions.Nodes.Clear();
            if (string.IsNullOrWhiteSpace(filterText))
            {
                if (_originalNodes != null)
                {
                    foreach (TreeNode n in _originalNodes)
                    {
                        treeViewPermissions.Nodes.Add((TreeNode)n.Clone());
                    }
                }
                else
                {
                    PopulatePermissionsTree();
                }
            }
            else
            {
                if (_originalNodes != null)
                {
                    foreach (TreeNode originalNode in _originalNodes)
                    {
                        TreeNode filteredNode = FilterNode(originalNode, filterText);
                        if (filteredNode != null)
                        {
                            treeViewPermissions.Nodes.Add(filteredNode);
                        }
                    }
                }
            }
            treeViewPermissions.EndUpdate();
            if (treeViewPermissions.Nodes.Count > 0)
            {
                treeViewPermissions.ExpandAll();
            }
        }

        private TreeNode FilterNode(TreeNode originalNode, string filterText)
        {
            bool nodeMatches = originalNode.Text.ToLower().Contains(filterText);
            List<TreeNode> matchingChildren = new List<TreeNode>();
            foreach (TreeNode childNode in originalNode.Nodes)
            {
                TreeNode filteredChild = FilterNode(childNode, filterText);
                if (filteredChild != null)
                {
                    matchingChildren.Add(filteredChild);
                }
            }
            if (nodeMatches || matchingChildren.Any())
            {
                TreeNode clonedNode = (TreeNode)originalNode.Clone();
                clonedNode.Nodes.Clear();
                foreach (TreeNode matchingChild in matchingChildren)
                {
                    clonedNode.Nodes.Add(matchingChild);
                }
                return clonedNode;
            }
            return null;
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAddUserRole_Click(object sender, EventArgs e)
        {
            string roleName = txtRoleName.Text.Trim(); // Assuming you renamed txtFullName to txtRoleName
            string description = txtDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(roleName))
            {
                MessageBox.Show("Role Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            List<string> selectedPermissions = new List<string>();
            // Use the refined GetCheckedPermissions that collects all checked nodes
            GetCheckedPermissions(treeViewPermissions.Nodes, selectedPermissions);


            if (!selectedPermissions.Any() && _editingRole == null)
            {
                MessageBox.Show("Please select at least one permission for this new role.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool successInLogic = false;

                if (_editingRole == null) // Add Mode
                {
                    int newRoleId = _userRoleRepository.AddRoleWithPermissions(roleName, description, selectedPermissions);
                    if (newRoleId > 0)
                    {
                        MessageBox.Show($"Role '{roleName}' added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        successInLogic = true;
                    }
                    else if (newRoleId == 0)
                    {
                        MessageBox.Show($"A role with the name '{roleName}' already exists. Please use a different name.", "Duplicate Role Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtRoleName.Focus();
                        txtRoleName.SelectAll();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to add role '{roleName}'. An error occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else // Edit Mode
                {
                    _editingRole.RoleName = roleName;
                    _editingRole.Description = description;
                    bool updated = _userRoleRepository.UpdateRoleWithPermissions(_editingRole, selectedPermissions);
                    if (updated)
                    {
                        MessageBox.Show($"Role '{_editingRole.RoleName}' updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        successInLogic = true;
                    }
                    else
                    {
                        MessageBox.Show($"Failed to update role '{_editingRole.RoleName}'. The name might conflict with an existing role, or an error occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (successInLogic)
                {
                    RoleSaved?.Invoke(this, EventArgs.Empty);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving role: {ex.ToString()}");
                MessageBox.Show($"An error occurred while saving the role: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
