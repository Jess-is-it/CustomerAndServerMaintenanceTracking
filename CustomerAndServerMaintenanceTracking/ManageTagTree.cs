using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.Models;
using SharedLibrary.Models;
using CustomerAndServerMaintenanceTracking.DataAccess;
using System.Data.SqlClient;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.ModalForms
{
    public partial class ManageTagTree : Form
    {
        private int currentClusterId;
        private Dictionary<int, bool> expansionState = new Dictionary<int, bool>();
        private TagRepository tagRepo = new TagRepository();

        public ManageTagTree(int clusterId)
        {
            InitializeComponent();

            // First, assign the cluster ID.
            currentClusterId = clusterId;

            // Optionally, update the form title using the cluster name.
            NetworkClusterRepository repo = new NetworkClusterRepository();
            NetworkCluster cluster = repo.GetClusters().FirstOrDefault(c => c.Id == currentClusterId);
            if (cluster != null)
                this.Text = "Arrange Hierarchy: " + cluster.ClusterName;

            // Attach drag and drop event handlers.
            treeViewHierarchy.ItemDrag += treeViewHierarchy_ItemDrag;
            treeViewHierarchy.DragEnter += treeViewHierarchy_DragEnter;
            treeViewHierarchy.DragDrop += treeViewHierarchy_DragDrop;
            treeViewHierarchy.DragOver += treeViewHierarchy_DragOver;
            // Attach filtering event.
            btnFilter.Click += BtnFilter_Click;

            // Now load the hierarchy.
            LoadTagHierarchy();
            RefreshNodeLabels(treeViewHierarchy.Nodes);
        }

        private void LoadTagHierarchy()
        {
            treeViewHierarchy.Nodes.Clear();

            NetworkClusterRepository clusterRepo = new NetworkClusterRepository();
            NetworkCluster cluster = clusterRepo.GetClusters().FirstOrDefault(c => c.Id == currentClusterId);
            if (cluster == null)
            {
                MessageBox.Show("Cluster not found.");
                return;
            }

            // Create a top node for the cluster.
            TreeNode clusterNode = new TreeNode(cluster.ClusterName);
            treeViewHierarchy.Nodes.Add(clusterNode);

            // Automatically expand the cluster node (e.g., EPON)
            clusterNode.Expand();

            // Get all tags for the current cluster.
            TagRepository tagRepo = new TagRepository();
            List<TagClass> clusterTags = tagRepo.GetTagsForCluster(currentClusterId);
            if (clusterTags.Count == 0)
                return;

            // Get assignments (if any) and build mapping.
            List<TagAssignment> assignments = tagRepo.GetAllTagAssignments()
                .Where(a => clusterTags.Any(t => t.Id == a.ParentTagId) && clusterTags.Any(t => t.Id == a.ChildTagId))
                .ToList();

            Dictionary<int, List<int>> parentToChildren = new Dictionary<int, List<int>>();
            foreach (var assign in assignments)
            {
                if (!parentToChildren.ContainsKey(assign.ParentTagId))
                    parentToChildren[assign.ParentTagId] = new List<int>();
                parentToChildren[assign.ParentTagId].Add(assign.ChildTagId);
            }

            // Identify root tags (not assigned as a child)
            var childIds = new HashSet<int>(assignments.Select(a => a.ChildTagId));
            var rootTags = clusterTags.Where(t => !childIds.Contains(t.Id)).ToList();

            foreach (var tag in rootTags)
            {
                TreeNode node = new TreeNode(tag.TagName) { Tag = tag };
                AddChildNodes(node, parentToChildren, clusterTags);
                clusterNode.Nodes.Add(node);
            }

            // Optionally add orphan tags
            var addedTagIds = new HashSet<int>(treeViewHierarchy.Nodes.Cast<TreeNode>()
                .SelectMany(n => GetAllNodeTagIds(n)));
            var orphanTags = clusterTags.Where(t => !addedTagIds.Contains(t.Id)).ToList();
            if (orphanTags.Count > 0)
            {
                TreeNode orphanRoot = new TreeNode("Orphan Tags");
                foreach (var tag in orphanTags)
                {
                    TreeNode orphanNode = new TreeNode(tag.TagName) { Tag = tag };
                    orphanRoot.Nodes.Add(orphanNode);
                }
                clusterNode.Nodes.Add(orphanRoot);
            }

        }

        #region Preserving (or Restoring) Expanded States of Tree View
        private List<string> _expandedNodes = new List<string>();
        private void SaveExpandedNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.IsExpanded)
                {
                    // "FullPath" gives the path from the root to this node, 
                    // using the TreeView's PathSeparator (default is '\')
                    _expandedNodes.Add(node.FullPath);
                }

                // Recurse into children
                if (node.Nodes.Count > 0)
                {
                    SaveExpandedNodes(node.Nodes);
                }
            }
        }
        private void RestoreExpandedNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                // If this node's FullPath was previously expanded, expand it
                if (_expandedNodes.Contains(node.FullPath))
                {
                    node.Expand();
                }

                // Recurse into children
                if (node.Nodes.Count > 0)
                {
                    RestoreExpandedNodes(node.Nodes);
                }
            }
        }
        private void treeViewHierarchy_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }
        private void treeViewHierarchy_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(TreeNode))) return;

            // Save expanded states
            _expandedNodes.Clear();
            SaveExpandedNodes(treeViewHierarchy.Nodes);

            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = treeViewHierarchy.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewHierarchy.GetNodeAt(pt);

            if (targetNode == null || draggedNode == targetNode) return;

            // The node we are dragging must be a TagClass
            if (!(draggedNode.Tag is TagClass childTag))
                return;

            if (targetNode.Tag is TagClass newParentTag)
            {
                if (!newParentTag.IsParent)
                {
                    MessageBox.Show("You can only drop tags into a Parent Tag or the cluster node.");
                    return;
                }

                // Move tag under new parent
                MoveTagToNewParent(childTag, draggedNode, newParentTag);

                // (Recalculate assigned-customer counts if needed, e.g. newParentTag.AssignedCustomerCount = SomeCount)
            }
            else
            {
                // Move to cluster root
                MoveTagToClusterRoot(childTag, draggedNode);

                // (Recalculate assigned-customer counts if needed here too)
            }

            // Expand newly dropped node if desired
            targetNode?.Expand();

            // Restore expansions
            RestoreExpandedNodes(treeViewHierarchy.Nodes);

            // **IMPORTANT**: Refresh the labels now that your structure/counts might have changed
            RefreshNodeLabels(treeViewHierarchy.Nodes);
        }
        private void treeViewHierarchy_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void treeViewHierarchy_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void MoveTagToClusterRoot(TagClass childTag, TreeNode draggedNode)
        {
            // 1) If old parent is a TagClass, remove the old TagAssignment
            if (draggedNode.Parent != null && draggedNode.Parent.Tag is TagClass oldParentTag)
            {
                tagRepo.RemoveTagFromTag(oldParentTag.Id, childTag.Id);
            }
            // 2) Insert the childTag into ClusterTagMapping so it’s recognized as root
            using (SqlConnection conn = new DatabaseHelper().GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM ClusterTagMapping WHERE ClusterId=@CId AND TagId=@TId) " +
                    "INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@CId, @TId)",
                    conn
                );
                cmd.Parameters.AddWithValue("@CId", currentClusterId);
                cmd.Parameters.AddWithValue("@TId", childTag.Id);
                cmd.ExecuteNonQuery();
            }

            // 3) Reload
            LoadTagHierarchy();
        }
        private void MoveTagToNewParent(TagClass childTag, TreeNode draggedNode, TagClass newParentTag)
        {
            //    if (draggedNode.Parent != null && draggedNode.Parent.Tag is TagClass oldParentTag)
            //    {
            //        tagRepo.RemoveTagFromTag(oldParentTag.Id, childTag.Id);
            //    }
            //    else
            //    {
            //        // The old parent is the cluster node => remove from cluster mapping
            //        using (SqlConnection conn = new DatabaseHelper().GetConnection())
            //        {
            //            conn.Open();
            //            SqlCommand cmd = new SqlCommand(@"
            //        DELETE FROM ClusterTagMapping
            //        WHERE ClusterId=@CId AND TagId=@TId", conn);
            //            cmd.Parameters.AddWithValue("@CId", currentClusterId);
            //            cmd.Parameters.AddWithValue("@TId", childTag.Id);
            //            cmd.ExecuteNonQuery();
            //        }
            //    }

            //    // 2) Create a new link to the new parent
            //    tagRepo.AssignTagToTag(newParentTag.Id, childTag.Id);

            //    // 3) Ensure the child is recognized in the cluster
            //    using (SqlConnection conn = new DatabaseHelper().GetConnection())
            //    {
            //        conn.Open();
            //        SqlCommand cmd = new SqlCommand(@"
            //    IF NOT EXISTS (SELECT 1 FROM ClusterTagMapping WHERE ClusterId=@CId AND TagId=@TId)
            //    INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@CId, @TId)",
            //            conn);
            //        cmd.Parameters.AddWithValue("@CId", currentClusterId);
            //        cmd.Parameters.AddWithValue("@TId", childTag.Id);
            //        cmd.ExecuteNonQuery();
            //    }

            //    // 4) Reload the hierarchy to refresh the tree view
            //    LoadTagHierarchy();

            //    // 5) Expand the new parent's node so its children (like NAP1) are visible
            //    ExpandParentNode(newParentTag.Id);
            int? oldParentId = null;

            // 1) If old parent is a TagClass => remove that link and store its ID
            if (draggedNode.Parent != null && draggedNode.Parent.Tag is TagClass oldParentTag)
            {
                oldParentId = oldParentTag.Id;
                tagRepo.RemoveTagFromTag(oldParentTag.Id, childTag.Id);
            }
            else
            {
                // The old parent is the cluster node => remove from cluster mapping
                using (SqlConnection conn = new DatabaseHelper().GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(@"
                DELETE FROM ClusterTagMapping
                WHERE ClusterId=@CId AND TagId=@TId", conn);
                    cmd.Parameters.AddWithValue("@CId", currentClusterId);
                    cmd.Parameters.AddWithValue("@TId", childTag.Id);
                    cmd.ExecuteNonQuery();
                }
            }

            // 2) Create a new link to the new parent
            tagRepo.AssignTagToTag(newParentTag.Id, childTag.Id);

            // 3) Ensure the child is recognized in the cluster mapping
            using (SqlConnection conn = new DatabaseHelper().GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM ClusterTagMapping WHERE ClusterId=@CId AND TagId=@TId)
            INSERT INTO ClusterTagMapping (ClusterId, TagId) VALUES (@CId, @TId)",
                    conn);
                cmd.Parameters.AddWithValue("@CId", currentClusterId);
                cmd.Parameters.AddWithValue("@TId", childTag.Id);
                cmd.ExecuteNonQuery();
            }

            // 4) Reload the hierarchy to refresh the tree view
            LoadTagHierarchy();

            // 5) Expand the new parent's node
            ExpandParentNode(newParentTag.Id);

            // 6) Also expand the old parent's node, if applicable
            if (oldParentId.HasValue)
            {
                ExpandParentNode(oldParentId.Value);
            }
        }
        private TreeNode FindNodeByTagId(TreeNode node, int tagId)
        {
            if (node.Tag is TagClass tag && tag.Id == tagId)
                return node;
            foreach (TreeNode child in node.Nodes)
            {
                TreeNode found = FindNodeByTagId(child, tagId);
                if (found != null)
                    return found;
            }
            return null;
        }
        private void ExpandParentNode(int parentId)
        {
            foreach (TreeNode root in treeViewHierarchy.Nodes)
            {
                TreeNode node = FindNodeByTagId(root, parentId);
                if (node != null)
                {
                    node.Expand();
                    break;
                }
            }
        }

        #endregion

        private IEnumerable<int> GetAllNodeTagIds(TreeNode node)
        {
            List<int> ids = new List<int>();
            if (node.Tag is TagClass tag)
                ids.Add(tag.Id);
            foreach (TreeNode child in node.Nodes)
            {
                ids.AddRange(GetAllNodeTagIds(child));
            }
            return ids;
        }

        private void AddChildNodes(TreeNode parentNode, Dictionary<int, List<int>> mapping, List<TagClass> allTags)
        {
            TagClass parentTag = (TagClass)parentNode.Tag;
            if (mapping.ContainsKey(parentTag.Id))
            {
                foreach (int childId in mapping[parentTag.Id])
                {
                    var childTag = allTags.FirstOrDefault(t => t.Id == childId);
                    if (childTag != null)
                    {
                        TreeNode childNode = new TreeNode(childTag.TagName) { Tag = childTag };
                        AddChildNodes(childNode, mapping, allTags);
                        parentNode.Nodes.Add(childNode);
                    }
                }
            }
        }
        private void BtnFilter_Click(object sender, EventArgs e)
        {
            string filterText = txtSearch.Text.Trim().ToLower();
            foreach (TreeNode node in treeViewHierarchy.Nodes)
            {
                FilterNode(node, filterText);
            }
        }

        private bool FilterNode(TreeNode node, string filter)
        {
            bool isMatch = node.Text.ToLower().Contains(filter);
            foreach (TreeNode child in node.Nodes)
            {
                isMatch |= FilterNode(child, filter);
            }
            node.BackColor = isMatch ? Color.LightYellow : Color.White;
            return isMatch;
        }

        private void removeParentToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (treeViewHierarchy.SelectedNode == null)
            {
                MessageBox.Show("Please select a tag to delete.");
                return;
            }

            if (!(treeViewHierarchy.SelectedNode.Tag is TagClass selectedTag))
            {
                MessageBox.Show("The selected node does not contain valid tag information.");
                return;
            }

            // Only allow deletion if the tag is marked as a parent.
            if (!selectedTag.IsParent)
            {
                MessageBox.Show("Only Parent Tags can be deleted.");
                return;
            }

            // Confirm deletion.
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete the parent tag '{selectedTag.TagName}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                TagRepository repo = new TagRepository();
                repo.DeleteTag(selectedTag.Id);

                // Reload the hierarchy and refresh node labels.
                LoadTagHierarchy();
                RefreshNodeLabels(treeViewHierarchy.Nodes);

                // Force the root (cluster) node to expand.
                if (treeViewHierarchy.Nodes.Count > 0)
                {
                    treeViewHierarchy.Nodes[0].Expand();
                }
            }
        }

        private void btnAddTag_Click(object sender, EventArgs e)
        {
            AddParentTag addParentTagForm = new AddParentTag();
            addParentTagForm.CurrentClusterId = currentClusterId;
            addParentTagForm.ParentTagAdded += OnParentTagAdded;  // subscribe
            addParentTagForm.StartPosition = FormStartPosition.CenterScreen;
            addParentTagForm.ShowDialog();
            //AddParentTag addParentTagForm = new AddParentTag();
            //// Pass the current cluster ID so the new tag gets assigned to this cluster.
            //addParentTagForm.CurrentClusterId = currentClusterId;
            //addParentTagForm.ParentTagAdded += (s, ea) =>
            //{
            //    addParentTagForm.StartPosition = FormStartPosition.CenterScreen;
            //    LoadTagHierarchy();
            //    // After reloading, expand the root node (cluster node)
            //    if (treeViewHierarchy.Nodes.Count > 0)
            //    {
            //        treeViewHierarchy.Nodes[0].Expand();
            //    }
            //};
            //addParentTagForm.ShowDialog();
        }
        private void OnParentTagAdded(object sender, EventArgs e)
        {
            // 1) Reload the entire hierarchy to show the new tag
            LoadTagHierarchy();

            // 2) Re-apply your label logic
            RefreshNodeLabels(treeViewHierarchy.Nodes);

            // 3) Optionally expand the root node
            if (treeViewHierarchy.Nodes.Count > 0)
            {
                treeViewHierarchy.Nodes[0].Expand();
            }
        }
        private int GetDirectAssignmentCount(TagClass tag)
        {
            if (tag == null || tag.IsParent) // Parents have no direct count displayed this way
            {
                return 0;
            }

            if (tag.TagType == "DeviceIP")
            {
                return tagRepo.GetAssignedDeviceIPCount(tag.Id);
            }
            else if (tag.TagType == "Customer")
            {
                // Use the existing GetAssignedEntities and count the result
                List<string> entities = tagRepo.GetAssignedEntities(tag.Id);
                // IMPORTANT: GetAssignedEntities returns BOTH customers and child tags.
                // We need to ensure it ONLY counts customers if TagType is "Customer".
                // Let's assume for now GetAssignedEntities is modified or we add GetAssignedCustomerCount
                // For simplicity here, we'll use GetAssignedEntities count, but refine if needed.
                return entities?.Count ?? 0; // You might need a GetAssignedCustomerCount later
            }
            // Add other TagTypes if necessary
            return 0;
        }

        // Method 2: Recursively gets the TOTAL count for a node and all its descendants
        private int GetTotalSubtreeCount(TreeNode node)
        {
            int totalCount = 0;

            // If the node itself is a leaf tag, get its direct count
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

        private void RefreshNodeLabels(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                string nodeText = node.Text; // Default to existing text
                int totalSubtreeCount = 0;   // Calculate total count for this node and its children

                if (node.Tag is TagClass tag) // It's a tag node
                {
                    nodeText = tag.TagName; // Start with the clean tag name
                    totalSubtreeCount = GetTotalSubtreeCount(node); // Get the total count

                    // Apply formatting based on type and count
                    if (tag.IsParent)
                    {
                        node.ForeColor = Color.Blue; // Parent tags are blue
                                                     // Display count for parents only if it's > 0, using "Entities"
                        if (totalSubtreeCount == 1)
                        {
                            nodeText += " (1) Entity";
                        }
                        else if (totalSubtreeCount > 1)
                        {
                            nodeText += $" ({totalSubtreeCount}) Entities";
                        }
                        // If count is 0, show only the TagName
                    }
                    else // It's a leaf node (Customer or DeviceIP)
                    {
                        node.ForeColor = Color.Black; // Leaf tags are black
                                                      // Display count for leaves only if it's > 0, using just the number
                        if (totalSubtreeCount > 0)
                        {
                            nodeText += $" ({totalSubtreeCount})";
                        }
                        // If count is 0, show only the TagName
                    }
                }
                else if (node.Parent == null) // Root node (Cluster)
                {
                    node.ForeColor = Color.Red;
                    // Optionally display total count for the cluster root:
                    // totalSubtreeCount = GetTotalSubtreeCount(node);
                    // if (totalSubtreeCount > 0) nodeText += $" ({totalSubtreeCount})";
                }
                else if (node.Text == "Orphan Tags") // Handle Orphan node
                {
                    node.ForeColor = Color.Gray;
                }

                node.Text = nodeText; // Set the final display text

                // Recurse for child nodes
                if (node.Nodes.Count > 0)
                {
                    RefreshNodeLabels(node.Nodes);
                }
            }
        }

        //private void RefreshNodeLabels(TreeNodeCollection nodes)
        //{
        //foreach (TreeNode node in nodes)
        //{
        //    if (node.Parent == null)
        //    {
        //        node.ForeColor = Color.Red;
        //    }
        //    else if (node.Tag is TagClass tag)
        //    {
        //        // 1) Compute the total count for this node.
        //        int totalCount = GetAssignedCount(node);

            //        // 2) If it's a parent, skip direct assigned & show "Entity"/"Entities" if total > 0
            //        if (tag.IsParent)
            //        {
            //            node.ForeColor = Color.Blue;
            //            if (totalCount == 0)
            //            {
            //                // no children assigned => just show the tag name
            //                node.Text = $"{tag.TagName}";
            //            }
            //            else if (totalCount == 1)
            //            {
            //                node.Text = $"{tag.TagName} (1) Entity";
            //            }
            //            else
            //            {
            //                node.Text = $"{tag.TagName} ({totalCount}) Entities";
            //            }
            //        }
            //        else
            //        {
            //            node.ForeColor = Color.Black;
            //            // 3) If it's a child tag, show "TagName" if total=0, or "TagName (X)" if total>0
            //            if (totalCount == 0)
            //            {
            //                node.Text = $"{tag.TagName}";
            //            }
            //            else
            //            {
            //                node.Text = $"{tag.TagName} ({totalCount})";
            //            }
            //        }
            //    }

            //    // Recurse for children
            //    if (node.Nodes.Count > 0)
            //    {
            //        RefreshNodeLabels(node.Nodes);
            //    }
            //}

            //}

        private int GetAssignedCount(TreeNode node)
        {
            //int count = 0;

            //if (node.Tag is TagClass tag)
            //{
            //    // If NOT a parent, add direct assigned from DB
            //    if (!tag.IsParent)
            //    {
            //        TagRepository repo = new TagRepository();
            //        List<string> entities = repo.GetAssignedEntities(tag.Id);
            //        count += (entities != null) ? entities.Count : 0;
            //    }
            //}

            //// Recurse for child nodes
            //foreach (TreeNode child in node.Nodes)
            //{
            //    count += GetAssignedCount(child);
            //}

            //return count;

            int count = 0;

            if (node.Tag is TagClass tag)
            {
                // For LEAF nodes (non-parents), get the count based on TagType
                if (!tag.IsParent)
                {
                    if (tag.TagType == "DeviceIP")
                    {
                        // Use the new method to count assigned Device IPs
                        count = tagRepo.GetAssignedDeviceIPCount(tag.Id); // tagRepo must be accessible (make it a class field)
                    }
                    else if (tag.TagType == "Customer")
                    {
                        // Use the existing method (or a dedicated one) for customers
                        // Note: GetAssignedEntities currently returns names, we just need the count
                        List<string> entities = tagRepo.GetAssignedEntities(tag.Id); // Consider optimizing this later if needed
                        count = (entities != null) ? entities.Count : 0;
                    }
                    // Add other TagTypes here if necessary
                }
            }

            // For ALL nodes (including parents), recursively add the counts of their children
            foreach (TreeNode child in node.Nodes)
            {
                count += GetAssignedCount(child); // Recursion sums up counts from children
            }

            return count;

        }
    }
}
