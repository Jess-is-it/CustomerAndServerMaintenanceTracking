using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking.Configuration
{
    public static class PermissionDefinitions
    {
        // Define your permission keys as constants
        public const string NAV_CUSTOMER_LIST = "NAV_CUSTOMER_LIST";
        public const string CUSTOMER_VIEW_ACTIVE = "CUSTOMER_VIEW_ACTIVE";
        public const string CUSTOMER_VIEW_ARCHIVED = "CUSTOMER_VIEW_ARCHIVED";
        // Add more customer-related permissions if needed (e.g., ADD, EDIT, DELETE)

        public const string NAV_TAGS = "NAV_TAGS"; // Main navigation to TagForm
        public const string TAGS_VIEW_CUSTOMER_TAB = "TAGS_VIEW_CUSTOMER_TAB";
        public const string TAGS_ADD_CUSTOMER_TAG = "TAGS_ADD_CUSTOMER_TAG";
        public const string TAGS_EDIT_CUSTOMER_TAG = "TAGS_EDIT_CUSTOMER_TAG";
        public const string TAGS_DELETE_CUSTOMER_TAG = "TAGS_DELETE_CUSTOMER_TAG";
        public const string TAGS_ASSIGN_CUSTOMER_TO_TAG = "TAGS_ASSIGN_CUSTOMER_TO_TAG";
        public const string TAGS_VIEW_DEVICEIP_TAB = "TAGS_VIEW_DEVICEIP_TAB";
        public const string TAGS_ADD_DEVICEIP_TAG = "TAGS_ADD_DEVICEIP_TAG";
        public const string TAGS_EDIT_DEVICEIP_TAG = "TAGS_EDIT_DEVICEIP_TAG";
        public const string TAGS_DELETE_DEVICEIP_TAG = "TAGS_DELETE_DEVICEIP_TAG";
        public const string TAGS_ASSIGN_DEVICEIP_TO_TAG = "TAGS_ASSIGN_DEVICEIP_TO_TAG";

        public const string NAV_NETWORK_CLUSTER = "NAV_NETWORK_CLUSTER"; // Main navigation to NetworkClusterTag form
        public const string NETWORK_CLUSTER_VIEW_LIST = "NETWORK_CLUSTER_VIEW_LIST";
        public const string NETWORK_CLUSTER_ADD = "NETWORK_CLUSTER_ADD";
        public const string NETWORK_CLUSTER_EDIT = "NETWORK_CLUSTER_EDIT";
        public const string NETWORK_CLUSTER_DELETE = "NETWORK_CLUSTER_DELETE";
        public const string NETWORK_CLUSTER_ARRANGE_HIERARCHY = "NETWORK_CLUSTER_ARRANGE_HIERARCHY";

        public const string NAV_TOOLS = "NAV_TOOLS"; // Main "Tools" menu in Dashboard
        public const string NAV_TOOLS_NETWATCH_ADD = "NAV_TOOLS_NETWATCH_ADD"; // Sub-menu to open NetwatchAdd form
        public const string NAV_TOOLS_NETWATCH_LIST = "NAV_TOOLS_NETWATCH_LIST"; // Sub-menu to open NetwatchList form

        public const string NETWATCH_LIST_VIEW_ALL = "NETWATCH_LIST_VIEW_ALL"; // View the list itself
        public const string NETWATCH_CONFIG_ADD = "NETWATCH_CONFIG_ADD"; // Ability to add new configs (from NetwatchAdd or NetwatchList)
        public const string NETWATCH_CONFIG_TOGGLE_ENABLE = "NETWATCH_CONFIG_TOGGLE_ENABLE"; // Start/Stop in NetwatchList
        public const string NETWATCH_CONFIG_DELETE = "NETWATCH_CONFIG_DELETE"; // Delete in NetwatchList
        public const string NETWATCH_CONFIG_VIEW_DETAILS = "NETWATCH_CONFIG_VIEW_DETAILS"; // Click status to view details

        public const string NAV_IP_MANAGEMENT = "NAV_IP_MANAGEMENT"; // Main "IP" menu in Dashboard
        public const string NAV_IP_DEVICE_IP_LIST = "NAV_IP_DEVICE_IP_LIST"; // Sub-menu to open DeviceIPForm

        public const string DEVICE_IP_VIEW_LIST = "DEVICE_IP_VIEW_LIST";
        public const string DEVICE_IP_ADD = "DEVICE_IP_ADD";
        public const string DEVICE_IP_EDIT = "DEVICE_IP_EDIT";
        public const string DEVICE_IP_DELETE = "DEVICE_IP_DELETE";

        public const string NAV_LOCATIONS = "NAV_LOCATIONS"; // For the new Locations feature
        public const string LOCATION_VIEW_LIST = "LOCATION_VIEW_LIST";
        public const string LOCATION_ADD = "LOCATION_ADD";
        public const string LOCATION_EDIT = "LOCATION_EDIT";
        public const string LOCATION_DELETE = "LOCATION_DELETE";

        public const string NAV_SETTINGS = "NAV_SETTINGS"; // Main navigation to Settings form
        public const string SETTINGS_VIEW_MIKROTIK_TAB = "SETTINGS_VIEW_MIKROTIK_TAB";
        public const string SETTINGS_VIEW_ROUTERS = "SETTINGS_VIEW_ROUTERS";
        public const string SETTINGS_ADD_ROUTER = "SETTINGS_ADD_ROUTER";
        public const string SETTINGS_EDIT_ROUTER = "SETTINGS_EDIT_ROUTER";
        public const string SETTINGS_DELETE_ROUTER = "SETTINGS_DELETE_ROUTER";
        // SMS tab is empty, skip for now
        public const string SETTINGS_VIEW_USERS_TAB = "SETTINGS_VIEW_USERS_TAB"; // Main Users Tab
        public const string SETTINGS_VIEW_USER_LIST_SUB_TAB = "SETTINGS_VIEW_USER_LIST_SUB_TAB";
        public const string SETTINGS_ADD_USER = "SETTINGS_ADD_USER";
        public const string SETTINGS_EDIT_USER = "SETTINGS_EDIT_USER"; // (placeholder for future)
        public const string SETTINGS_DELETE_USER = "SETTINGS_DELETE_USER"; // (placeholder for future)
        public const string SETTINGS_VIEW_ROLE_LIST_SUB_TAB = "SETTINGS_VIEW_ROLE_LIST_SUB_TAB";
        public const string SETTINGS_ADD_ROLE = "SETTINGS_ADD_ROLE";
        public const string SETTINGS_EDIT_ROLE = "SETTINGS_EDIT_ROLE"; // (placeholder for future)
        public const string SETTINGS_DELETE_ROLE = "SETTINGS_DELETE_ROLE"; // (placeholder for future)
        public const string SETTINGS_VIEW_SERVICE_MANAGEMENT_TAB = "SETTINGS_VIEW_SERVICE_MANAGEMENT_TAB";
        public const string SETTINGS_VIEW_SERVICE_LOGS = "SETTINGS_VIEW_SERVICE_LOGS";


        // Helper method to get the hierarchical structure for the TreeView
        public static List<TreeNode> GetPermissionTree()
        {
            var rootNodes = new List<TreeNode>();

            // Customer List
            var customerNode = new TreeNode("Customer List Access") { Tag = NAV_CUSTOMER_LIST };
            customerNode.Nodes.Add(new TreeNode("View Active Customers") { Tag = CUSTOMER_VIEW_ACTIVE });
            customerNode.Nodes.Add(new TreeNode("View Archived Customers") { Tag = CUSTOMER_VIEW_ARCHIVED });
            rootNodes.Add(customerNode);

            // Tags
            var tagsNode = new TreeNode("Tags Management") { Tag = NAV_TAGS };
            tagsNode.Nodes.Add(new TreeNode("View Customer Tags Tab") { Tag = TAGS_VIEW_CUSTOMER_TAB });
            tagsNode.Nodes.Add(new TreeNode("Add Customer Tag") { Tag = TAGS_ADD_CUSTOMER_TAG });
            tagsNode.Nodes.Add(new TreeNode("Edit Customer Tag") { Tag = TAGS_EDIT_CUSTOMER_TAG });
            tagsNode.Nodes.Add(new TreeNode("Delete Customer Tag") { Tag = TAGS_DELETE_CUSTOMER_TAG });
            tagsNode.Nodes.Add(new TreeNode("Assign Customer to Tag") { Tag = TAGS_ASSIGN_CUSTOMER_TO_TAG });
            tagsNode.Nodes.Add(new TreeNode("View Device IP Tags Tab") { Tag = TAGS_VIEW_DEVICEIP_TAB });
            tagsNode.Nodes.Add(new TreeNode("Add Device IP Tag") { Tag = TAGS_ADD_DEVICEIP_TAG });
            tagsNode.Nodes.Add(new TreeNode("Edit Device IP Tag") { Tag = TAGS_EDIT_DEVICEIP_TAG });
            tagsNode.Nodes.Add(new TreeNode("Delete Device IP Tag") { Tag = TAGS_DELETE_DEVICEIP_TAG });
            tagsNode.Nodes.Add(new TreeNode("Assign Device IP to Tag") { Tag = TAGS_ASSIGN_DEVICEIP_TO_TAG });
            rootNodes.Add(tagsNode);

            // Network Cluster
            var networkClusterNode = new TreeNode("Network Cluster Management") { Tag = NAV_NETWORK_CLUSTER };
            networkClusterNode.Nodes.Add(new TreeNode("View Cluster List") { Tag = NETWORK_CLUSTER_VIEW_LIST });
            networkClusterNode.Nodes.Add(new TreeNode("Add Cluster") { Tag = NETWORK_CLUSTER_ADD });
            networkClusterNode.Nodes.Add(new TreeNode("Edit Cluster") { Tag = NETWORK_CLUSTER_EDIT });
            networkClusterNode.Nodes.Add(new TreeNode("Delete Cluster") { Tag = NETWORK_CLUSTER_DELETE });
            networkClusterNode.Nodes.Add(new TreeNode("Arrange Hierarchy") { Tag = NETWORK_CLUSTER_ARRANGE_HIERARCHY });
            rootNodes.Add(networkClusterNode);

            // Tools (Netwatch)
            var toolsNode = new TreeNode("Tools Access") { Tag = NAV_TOOLS };
            var netwatchAddNode = new TreeNode("Access Netwatch Add/Config") { Tag = NAV_TOOLS_NETWATCH_ADD }; // For menu item
            netwatchAddNode.Nodes.Add(new TreeNode("Configure/Add New Netwatch") { Tag = NETWATCH_CONFIG_ADD }); // Actual add permission
            toolsNode.Nodes.Add(netwatchAddNode);
            var netwatchListNode = new TreeNode("Access Netwatch List") { Tag = NAV_TOOLS_NETWATCH_LIST }; // For menu item
            netwatchListNode.Nodes.Add(new TreeNode("View Netwatch List") { Tag = NETWATCH_LIST_VIEW_ALL });
            netwatchListNode.Nodes.Add(new TreeNode("Toggle Netwatch Enable/Disable") { Tag = NETWATCH_CONFIG_TOGGLE_ENABLE });
            netwatchListNode.Nodes.Add(new TreeNode("Delete Netwatch Configuration") { Tag = NETWATCH_CONFIG_DELETE });
            netwatchListNode.Nodes.Add(new TreeNode("View Netwatch Details") { Tag = NETWATCH_CONFIG_VIEW_DETAILS });
            toolsNode.Nodes.Add(netwatchListNode);
            rootNodes.Add(toolsNode);

            // IP Management (Device IP)
            var ipManagementNode = new TreeNode("IP Management Access") { Tag = NAV_IP_MANAGEMENT };
            var deviceIpNode = new TreeNode("Access Device IP List") { Tag = NAV_IP_DEVICE_IP_LIST }; // For menu item
            deviceIpNode.Nodes.Add(new TreeNode("View Device IP List") { Tag = DEVICE_IP_VIEW_LIST });
            deviceIpNode.Nodes.Add(new TreeNode("Add Device IP") { Tag = DEVICE_IP_ADD });
            deviceIpNode.Nodes.Add(new TreeNode("Edit Device IP") { Tag = DEVICE_IP_EDIT });
            deviceIpNode.Nodes.Add(new TreeNode("Delete Device IP") { Tag = DEVICE_IP_DELETE });
            ipManagementNode.Nodes.Add(deviceIpNode);
            rootNodes.Add(ipManagementNode);

            // Locations
            var locationsNode = new TreeNode("Location Profiling Access") { Tag = NAV_LOCATIONS };
            locationsNode.Nodes.Add(new TreeNode("View Locations List") { Tag = LOCATION_VIEW_LIST });
            locationsNode.Nodes.Add(new TreeNode("Add Location") { Tag = LOCATION_ADD });
            locationsNode.Nodes.Add(new TreeNode("Edit Location") { Tag = LOCATION_EDIT });
            locationsNode.Nodes.Add(new TreeNode("Delete Location") { Tag = LOCATION_DELETE });
            rootNodes.Add(locationsNode);

            // Settings
            var settingsNode = new TreeNode("Settings Access") { Tag = NAV_SETTINGS };
            var mikrotikTabNode = new TreeNode("Mikrotik Routers Tab") { Tag = SETTINGS_VIEW_MIKROTIK_TAB };
            mikrotikTabNode.Nodes.Add(new TreeNode("View Routers") { Tag = SETTINGS_VIEW_ROUTERS });
            mikrotikTabNode.Nodes.Add(new TreeNode("Add Router") { Tag = SETTINGS_ADD_ROUTER });
            mikrotikTabNode.Nodes.Add(new TreeNode("Edit Router") { Tag = SETTINGS_EDIT_ROUTER });
            mikrotikTabNode.Nodes.Add(new TreeNode("Delete Router") { Tag = SETTINGS_DELETE_ROUTER });
            settingsNode.Nodes.Add(mikrotikTabNode);

            var usersTabNode = new TreeNode("Users & Roles Tab") { Tag = SETTINGS_VIEW_USERS_TAB };
            var userListSubTabNode = new TreeNode("User List Sub-Tab") { Tag = SETTINGS_VIEW_USER_LIST_SUB_TAB };
            userListSubTabNode.Nodes.Add(new TreeNode("Add User") { Tag = SETTINGS_ADD_USER });
            userListSubTabNode.Nodes.Add(new TreeNode("Edit User") { Tag = SETTINGS_EDIT_USER });
            userListSubTabNode.Nodes.Add(new TreeNode("Delete User") { Tag = SETTINGS_DELETE_USER });
            usersTabNode.Nodes.Add(userListSubTabNode);
            var roleListSubTabNode = new TreeNode("Role List Sub-Tab") { Tag = SETTINGS_VIEW_ROLE_LIST_SUB_TAB };
            roleListSubTabNode.Nodes.Add(new TreeNode("Add Role") { Tag = SETTINGS_ADD_ROLE });
            roleListSubTabNode.Nodes.Add(new TreeNode("Edit Role") { Tag = SETTINGS_EDIT_ROLE });
            roleListSubTabNode.Nodes.Add(new TreeNode("Delete Role") { Tag = SETTINGS_DELETE_ROLE });
            usersTabNode.Nodes.Add(roleListSubTabNode);
            settingsNode.Nodes.Add(usersTabNode);

            var serviceMgmtTabNode = new TreeNode("Service Management Tab") { Tag = SETTINGS_VIEW_SERVICE_MANAGEMENT_TAB };
            serviceMgmtTabNode.Nodes.Add(new TreeNode("View Service Logs") { Tag = SETTINGS_VIEW_SERVICE_LOGS });
            settingsNode.Nodes.Add(serviceMgmtTabNode);
            rootNodes.Add(settingsNode);

            return rootNodes;
        }
    }
}
