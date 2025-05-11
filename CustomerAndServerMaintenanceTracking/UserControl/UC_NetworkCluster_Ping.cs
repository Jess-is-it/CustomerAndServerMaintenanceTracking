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

namespace CustomerAndServerMaintenanceTracking
{
    public partial class UC_NetworkCluster_Ping : UserControl
    {
        public int ClusterId { get; set; }
        private TagRepository _tagRepository;
        private List<TreeNode> _originalRootNodes = new List<TreeNode>();

        public UC_NetworkCluster_Ping()
        {
            InitializeComponent();
            _tagRepository = new TagRepository();
            treeViewCluster.CheckBoxes = true;
            this.treeViewCluster.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewCluster_AfterCheck);
            this.txtSearchTags.TextChanged += new System.EventHandler(this.txtSearchTags_TextChanged);

        }

        private void AddChildNodes(TreeNode parentNode, Dictionary<int, List<int>> parentToChildrenMap, List<TagClass> allClusterTags)
        {
            if (!(parentNode.Tag is TagClass parentTag)) return;

            if (parentToChildrenMap.TryGetValue(parentTag.Id, out List<int> childIds))
            {
                foreach (int childId in childIds)
                {
                    var childTag = allClusterTags.FirstOrDefault(t => t.Id == childId);
                    if (childTag != null)
                    {
                        TreeNode childNode = new TreeNode(childTag.TagName) { Tag = childTag };
                        AddChildNodes(childNode, parentToChildrenMap, allClusterTags); // Recursive call
                        parentNode.Nodes.Add(childNode);
                    }
                }
            }
        }
        private void UC_NetworkCluster_Ping_Load(object sender, EventArgs e)
        {
            // Call the method to load the hierarchy for the specific ClusterId assigned to this control
            LoadClusterHierarchy();
        }
        private void LoadClusterHierarchy()
        {
            // Ensure we have a valid ClusterId before proceeding
            if (this.ClusterId <= 0)
            {
                treeViewCluster.Nodes.Clear();
                _originalRootNodes.Clear(); // Also clear original nodes if ID is invalid
                return;
            }

            // Clear the currently displayed nodes and the stored original nodes
            treeViewCluster.Nodes.Clear();
            _originalRootNodes.Clear();

            try
            {
                // 1. Get all tags specifically for THIS cluster
                List<TagClass> tagsInThisCluster = _tagRepository.GetTagsForCluster(this.ClusterId);
                if (!tagsInThisCluster.Any())
                {
                    return; // No tags for this cluster, nothing to display or store
                }
                var tagIdsInCluster = new HashSet<int>(tagsInThisCluster.Select(t => t.Id));

                // 2. Get all tag assignments (parent-child links) from the database
                List<TagAssignment> allAssignments = _tagRepository.GetAllTagAssignments();

                // 3. Filter assignments to include only those relevant to THIS cluster
                List<TagAssignment> clusterAssignments = allAssignments
                    .Where(a => tagIdsInCluster.Contains(a.ParentTagId) && tagIdsInCluster.Contains(a.ChildTagId))
                    .ToList();

                // 4. Build the parent-to-children map for THIS cluster
                Dictionary<int, List<int>> parentToChildrenMap = new Dictionary<int, List<int>>();
                foreach (var assign in clusterAssignments)
                {
                    if (!parentToChildrenMap.ContainsKey(assign.ParentTagId))
                    {
                        parentToChildrenMap[assign.ParentTagId] = new List<int>();
                    }
                    parentToChildrenMap[assign.ParentTagId].Add(assign.ChildTagId);
                }

                // 5. Identify root tags for THIS cluster
                var childIdsInClusterAssignments = new HashSet<int>(clusterAssignments.Select(a => a.ChildTagId));
                var rootTags = tagsInThisCluster
                               .Where(t => !childIdsInClusterAssignments.Contains(t.Id))
                               .ToList();

                // 6. Populate a temporary list with the fully constructed nodes
                List<TreeNode> currentRootNodes = new List<TreeNode>();
                foreach (var rootTag in rootTags)
                {
                    TreeNode rootTagNode = new TreeNode(rootTag.TagName) { Tag = rootTag };
                    AddChildNodes(rootTagNode, parentToChildrenMap, tagsInThisCluster); // Recursively adds children
                    currentRootNodes.Add(rootTagNode);
                }

                // 7. Store deep clones of the fully constructed nodes as the master copy
                _originalRootNodes = currentRootNodes.Select(n => (TreeNode)n.Clone()).ToList();

                // 8. Apply formatting and counts to the master copy nodes
                RefreshNodeLabels(_originalRootNodes); // This modifies the nodes in _originalRootNodes

                // 9. Populate the actual TreeView with clones from the (now formatted) master copy
                treeViewCluster.BeginUpdate(); // Prevent flickering
                foreach (var originalNode in _originalRootNodes)
                {
                    treeViewCluster.Nodes.Add((TreeNode)originalNode.Clone()); // Add clones to ensure display is separate
                }
                treeViewCluster.EndUpdate(); // Allow drawing

                // 10. Expand nodes
                treeViewCluster.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tag hierarchy for Cluster ID {this.ClusterId}: {ex.Message}",
                                "Hierarchy Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Log error
            }
        }


        #region TreeView Methods
        // Helper method to recursively check/uncheck all child nodes
        // Recursively checks/unchecks children in the original list and updates displayed nodes
        private void CheckAllOriginalChildNodes(TreeNode originalNode, bool nodeChecked)
        {
            // Iterate through the children in the ORIGINAL node structure
            foreach (TreeNode originalChildNode in originalNode.Nodes.Cast<TreeNode>())
            {
                bool stateChanged = (originalChildNode.Checked != nodeChecked);
                if (stateChanged)
                {
                    // 1. Update the ORIGINAL node's check state
                    originalChildNode.Checked = nodeChecked;

                    // 2. Find the corresponding DISPLAYED node (if it exists/is visible)
                    TreeNode displayedChildNode = FindDisplayedNode(treeViewCluster.Nodes, (originalChildNode.Tag as TagClass)?.Id ?? -1);

                    // 3. Update the DISPLAYED node's check state WITHOUT triggering events
                    if (displayedChildNode != null && displayedChildNode.Checked != nodeChecked)
                    {
                        this.treeViewCluster.AfterCheck -= treeViewCluster_AfterCheck; // Detach
                        displayedChildNode.Checked = nodeChecked;
                        this.treeViewCluster.AfterCheck += treeViewCluster_AfterCheck; // Re-attach
                    }
                }

                // 4. Recurse for the original node's children
                if (originalChildNode.Nodes.Count > 0)
                {
                    CheckAllOriginalChildNodes(originalChildNode, nodeChecked);
                }
            }
        }


        // Helper method to update the check state of parent nodes based on children's state
        private void UpdateParentNodeCheckState(TreeNode displayedNode)
        {
            // Get the parent of the node currently displayed in the TreeView
            TreeNode displayedParentNode = displayedNode.Parent;

            // Only proceed if there is a displayed parent
            if (displayedParentNode != null)
            {
                // Find the corresponding parent node in the original, unfiltered tree structure
                TreeNode originalParentNode = null;
                if (displayedParentNode.Tag is TagClass parentTag && parentTag.Id > 0)
                {
                    originalParentNode = FindNodeInOriginalTree(_originalRootNodes, parentTag.Id);
                }

                // Only proceed if we found the corresponding original parent node
                if (originalParentNode != null)
                {
                    bool allOriginalChildrenChecked = true;

                    // *** CRITICAL CHANGE: Iterate through the children of the ORIGINAL parent node ***
                    foreach (TreeNode originalChildNode in originalParentNode.Nodes.Cast<TreeNode>())
                    {
                        // Check the state of each child IN THE ORIGINAL TREE
                        if (!originalChildNode.Checked)
                        {
                            allOriginalChildrenChecked = false;
                            break; // Found an unchecked child, parent should be unchecked
                        }
                    }

                    // Check if the displayed parent's state needs to change based on ALL original children
                    if (displayedParentNode.Checked != allOriginalChildrenChecked)
                    {
                        // Update the DISPLAYED parent node (temporarily detach handler)
                        this.treeViewCluster.AfterCheck -= treeViewCluster_AfterCheck;
                        displayedParentNode.Checked = allOriginalChildrenChecked;
                        this.treeViewCluster.AfterCheck += treeViewCluster_AfterCheck;

                        // Sync this CORRECT state back to the ORIGINAL parent node
                        originalParentNode.Checked = allOriginalChildrenChecked; // Update the master copy
                    }
                }
                // Else: Could not find original parent node - state cannot be reliably determined? Or log warning.
                // For now, we just don't update if the original isn't found.

                // Recursively call for the grandparent IN THE DISPLAYED TREE to propagate the check state upwards
                UpdateParentNodeCheckState(displayedParentNode);
            }
        }
        private void treeViewCluster_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Only process if the check was triggered by user interaction
            if (e.Action != TreeViewAction.Unknown)
            {
                TreeNode clickedNode = e.Node; // The node clicked in the displayed tree
                bool isChecked = clickedNode.Checked; // The new state

                // Find the corresponding node in the master list
                TreeNode originalNode = null;
                if (clickedNode.Tag is TagClass clickedTag && clickedTag.Id > 0)
                {
                    originalNode = FindNodeInOriginalTree(_originalRootNodes, clickedTag.Id);
                }

                // Only proceed if we found the original node
                if (originalNode != null)
                {
                    // 1. Sync the state of the clicked node to the original node FIRST
                    if (originalNode.Checked != isChecked)
                    {
                        originalNode.Checked = isChecked;
                    }


                    // 2. Perform the downward cascade starting from the ORIGINAL node
                    // This updates originals and corresponding displayed nodes
                    CheckAllOriginalChildNodes(originalNode, isChecked);

                    // 3. Perform the upward check starting from the CLICKED node
                    // This reads original children states and updates displayed/original parents
                    UpdateParentNodeCheckState(clickedNode);

                }
                // Else: Original node not found, cannot sync or cascade reliably. Maybe log an error.
            }
        }
        // Gets the direct count of assigned Customers or Device IPs for a LEAF tag
        private int GetDirectAssignmentCount(TagClass tag)
        {
            // Parent tags don't have direct assignments displayed this way
            if (tag == null || tag.IsParent)
            {
                return 0;
            }

            // Check the TagType to call the correct repository method
            if (tag.TagType == "DeviceIP")
            {
                // Use the specific count method for Device IPs
                return _tagRepository.GetAssignedDeviceIPCount(tag.Id); //
            }
            else if (tag.TagType == "Customer")
            {
                // Use the method that gets Customer IDs and count the result
                return _tagRepository.GetAssignedCustomerIds(tag.Id).Count; //
            }
            // Add other TagTypes here if necessary in the future
            return 0;
        }

        // Recursively gets the TOTAL count for a node and all its descendants
        private int GetTotalSubtreeCount(TreeNode node)
        {
            int totalCount = 0;

            // If the node itself represents a leaf tag (not a parent), get its direct count
            if (node.Tag is TagClass tag && !tag.IsParent)
            {
                totalCount = GetDirectAssignmentCount(tag); // Use the direct count method
            }

            // Recursively add counts from all child nodes
            foreach (TreeNode childNode in node.Nodes)
            {
                totalCount += GetTotalSubtreeCount(childNode); // Recursion sums up everything below
            }

            return totalCount;
        }

        // Refreshes the text and formatting of nodes to include counts
        private void RefreshNodeLabels(IEnumerable<TreeNode> nodes)
        {
            // Ensure we have a TagRepository instance
            if (_tagRepository == null) _tagRepository = new TagRepository();

            foreach (TreeNode node in nodes)
            {
                string nodeText; // Variable to build the final node text
                int totalSubtreeCount; // Variable to store the calculated count

                if (node.Tag is TagClass tag) // Check if the node represents a Tag
                {
                    nodeText = tag.TagName; // Start with the clean tag name
                    totalSubtreeCount = GetTotalSubtreeCount(node); // Calculate the total count

                    // Apply formatting based on whether it's a parent or leaf tag
                    if (tag.IsParent)
                    {
                        node.ForeColor = System.Drawing.Color.Blue; // Parent tags are blue

                        // Display count for parents only if it's > 0, using "Entity" / "Entities"
                        if (totalSubtreeCount == 1)
                        {
                            nodeText += " (1) Entity";
                        }
                        else if (totalSubtreeCount > 1)
                        {
                            nodeText += $" ({totalSubtreeCount}) Entities";
                        }
                        // If count is 0 for a parent, show only the TagName
                    }
                    else // It's a leaf node (Customer or DeviceIP tag type)
                    {
                        node.ForeColor = System.Drawing.Color.Black; // Leaf tags are black

                        // Display count for leaves only if it's > 0, using just the number
                        if (totalSubtreeCount > 0)
                        {
                            nodeText += $" ({totalSubtreeCount})";
                        }
                        // If count is 0 for a leaf, show only the TagName
                    }
                }
                else
                {
                    // If node.Tag is not a TagClass (e.g., could be a placeholder)
                    // Keep the original text and default color
                    nodeText = node.Text;
                    node.ForeColor = System.Drawing.Color.Black; // Or your default color
                }


                node.Text = nodeText; // Set the final display text for the node

                // Recursively call this method for child nodes
                if (node.Nodes.Count > 0)
                {
                    RefreshNodeLabels(node.Nodes.Cast<TreeNode>());
                }
            }
        }

        // Main method to apply the filter
        private void FilterTreeView(string filterText)
        {
            filterText = filterText.Trim().ToLower();

            treeViewCluster.BeginUpdate();
            treeViewCluster.Nodes.Clear();

            if (string.IsNullOrEmpty(filterText))
            {

                // If filter is empty, restore all original nodes (now guaranteed consistent)
                foreach (TreeNode originalNode in _originalRootNodes)
                {
                    treeViewCluster.Nodes.Add((TreeNode)originalNode.Clone());
                }
            }
            else
            {
                // If filter has text, build a filtered tree (Keep this part the same)
                foreach (TreeNode originalNode in _originalRootNodes)
                {
                    TreeNode filteredNode = CloneNodeIfMatches(originalNode, filterText);
                    if (filteredNode != null)
                    {
                        treeViewCluster.Nodes.Add(filteredNode);
                    }
                }
            }

            treeViewCluster.EndUpdate();
            treeViewCluster.ExpandAll();
        }

        // Recursive helper method to clone a node IF it or any descendant matches the filter
        private TreeNode CloneNodeIfMatches(TreeNode originalNode, string filter)
        {
            bool nodeTextMatches = originalNode.Text.ToLower().Contains(filter);
            bool descendantMatches = false;
            List<TreeNode> matchingChildClones = new List<TreeNode>();

            // Recursively check children
            foreach (TreeNode originalChildNode in originalNode.Nodes)
            {
                TreeNode clonedChild = CloneNodeIfMatches(originalChildNode, filter);
                if (clonedChild != null)
                {
                    descendantMatches = true;
                    matchingChildClones.Add(clonedChild);
                }
            }

            // Include this node if its text matches OR if any descendant matches
            if (nodeTextMatches || descendantMatches)
            {
                // Create a clone of the original node
                TreeNode clonedNode = (TreeNode)originalNode.Clone(); // Creates a shallow clone initially
                                                                      // Crucially, clear the cloned node's children collection,
                                                                      // as Clone() copies the child references from the original.
                clonedNode.Nodes.Clear();
                // Add back only the children (and their descendants) that matched the filter.
                foreach (var matchingChildClone in matchingChildClones)
                {
                    clonedNode.Nodes.Add(matchingChildClone);
                }

                // Optional: Highlight the node if its *own* text matched
                if (nodeTextMatches)
                {
                    clonedNode.BackColor = System.Drawing.Color.Yellow; // Highlight direct matches
                }


                return clonedNode;
            }

            // No match for this node or its descendants
            return null;
        }
        // Recursively finds a TreeNode within a given collection based on the Tag.Id
        private TreeNode FindNodeInOriginalTree(IEnumerable<TreeNode> nodesToSearch, int tagId)
        {
            foreach (TreeNode node in nodesToSearch)
            {
                // Check if the current node's tag matches
                if (node.Tag is TagClass tag && tag.Id == tagId)
                {
                    return node; // Found it
                }

                // If not found, search recursively in children
                if (node.Nodes.Count > 0)
                {
                    TreeNode foundInChildren = FindNodeInOriginalTree(node.Nodes.Cast<TreeNode>(), tagId);
                    if (foundInChildren != null)
                    {
                        return foundInChildren; // Found in a descendant
                    }
                }
            }
            return null; // Not found in this branch
        }
        // Recursively ensures that if a parent node is checked, all its descendants are also checked.
        // Recursively finds a node in the displayed tree based on Tag.Id
        private TreeNode FindDisplayedNode(TreeNodeCollection nodesToSearch, int tagId)
        {
            foreach (TreeNode node in nodesToSearch)
            {
                // Check if the current node's tag matches
                if (node.Tag is TagClass tag && tag.Id == tagId)
                {
                    return node; // Found it
                }

                // If not found, search recursively in children
                if (node.Nodes.Count > 0)
                {
                    TreeNode foundInChildren = FindDisplayedNode(node.Nodes, tagId); // Search the displayed children
                    if (foundInChildren != null)
                    {
                        return foundInChildren; // Found in a descendant
                    }
                }
            }
            return null; // Not found in this branch
        }

        #endregion

        private void txtSearchTags_TextChanged(object sender, EventArgs e)
        {
            FilterTreeView(txtSearchTags.Text);
        }

        private void btnAddPingTags_Click(object sender, EventArgs e)
        {

        }
    }
}
