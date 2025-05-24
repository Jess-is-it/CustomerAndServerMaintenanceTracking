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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Windows.Documents;
using System.Xml.Linq;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.UserControl
{
    public partial class UC_NetworkCluster_Netwatch : System.Windows.Forms.UserControl
    {
        public int ClusterId { get; set; }
        private TagRepository _tagRepository;
        private NetworkClusterRepository _networkClusterRepository;
        private List<TreeNode> _originalRootNodes = new List<TreeNode>();
        private OverlayForm overlayForm;

        public UC_NetworkCluster_Netwatch()
        {
            InitializeComponent();
            _tagRepository = new TagRepository();
            _networkClusterRepository = new NetworkClusterRepository();
            treeViewCluster.CheckBoxes = true;

            this.treeViewCluster.AfterCheck -= treeViewCluster_AfterCheck;
            this.treeViewCluster.AfterCheck += treeViewCluster_AfterCheck;

            this.txtSearchTags.TextChanged -= txtSearchTags_TextChanged;
            this.txtSearchTags.TextChanged += txtSearchTags_TextChanged;

            this.btnAddNetwatch.Click -= btnAddNetwatch_Click;
            this.btnAddNetwatch.Click += btnAddNetwatch_Click;
        }

        private void Overlay()
        {
            if (overlayForm == null || overlayForm.IsDisposed)
            {
                overlayForm = new OverlayForm();
            }
            // Assuming OverlayForm constructor handles Maximized, Opacity.
            // Consider parent form for Bounds if OverlayForm isn't self-maximizing.
            // Form parentForm = this.FindForm();
            // if (parentForm != null) { overlayForm.Owner = parentForm; overlayForm.Bounds = parentForm.Bounds; }
            overlayForm.Show();
            overlayForm.BringToFront();
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
                        AddChildNodes(childNode, parentToChildrenMap, allClusterTags);
                        parentNode.Nodes.Add(childNode);
                    }
                }
            }
        }

        private void UC_NetworkCluster_Netwatch_Load(object sender, EventArgs e)
        {
            LoadClusterHierarchy();
        }

        private void LoadClusterHierarchy()
        {
            if (this.ClusterId <= 0)
            {
                treeViewCluster.Nodes.Clear();
                _originalRootNodes.Clear();
                return;
            }
            treeViewCluster.Nodes.Clear();
            _originalRootNodes.Clear();
            try
            {
                List<TagClass> tagsInThisCluster = _tagRepository.GetTagsForCluster(this.ClusterId);
                if (!tagsInThisCluster.Any()) return;

                var tagIdsInCluster = new HashSet<int>(tagsInThisCluster.Select(t => t.Id));
                List<TagAssignment> allAssignments = _tagRepository.GetAllTagAssignments();
                List<TagAssignment> clusterAssignments = allAssignments
                    .Where(a => tagIdsInCluster.Contains(a.ParentTagId) && tagIdsInCluster.Contains(a.ChildTagId))
                    .ToList();

                Dictionary<int, List<int>> parentToChildrenMap = new Dictionary<int, List<int>>();
                foreach (var assign in clusterAssignments)
                {
                    if (!parentToChildrenMap.ContainsKey(assign.ParentTagId))
                    {
                        parentToChildrenMap[assign.ParentTagId] = new List<int>();
                    }
                    parentToChildrenMap[assign.ParentTagId].Add(assign.ChildTagId);
                }

                var childIdsInClusterAssignments = new HashSet<int>(clusterAssignments.Select(a => a.ChildTagId));
                var rootTags = tagsInThisCluster
                                    .Where(t => !childIdsInClusterAssignments.Contains(t.Id))
                                    .ToList();

                List<TreeNode> currentRootNodes = new List<TreeNode>();
                foreach (var rootTag in rootTags)
                {
                    TreeNode rootTagNode = new TreeNode(rootTag.TagName) { Tag = rootTag };
                    AddChildNodes(rootTagNode, parentToChildrenMap, tagsInThisCluster);
                    currentRootNodes.Add(rootTagNode);
                }

                _originalRootNodes = currentRootNodes.Select(n => (TreeNode)n.Clone()).ToList();
                RefreshNodeLabels(_originalRootNodes); // Apply text formatting

                treeViewCluster.BeginUpdate();
                foreach (var originalNode in _originalRootNodes)
                {
                    treeViewCluster.Nodes.Add((TreeNode)originalNode.Clone());
                }
                treeViewCluster.EndUpdate();
                treeViewCluster.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tag hierarchy for Cluster ID {this.ClusterId}: {ex.Message}",
                                "Hierarchy Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region TreeView Helper Methods (From your last full code paste)
        private void CheckAllOriginalChildNodes(TreeNode originalNode, bool nodeChecked)
        {
            foreach (TreeNode originalChildNode in originalNode.Nodes.Cast<TreeNode>())
            {
                bool stateChanged = (originalChildNode.Checked != nodeChecked);
                if (stateChanged)
                {
                    originalChildNode.Checked = nodeChecked;
                    TreeNode displayedChildNode = FindDisplayedNode(treeViewCluster.Nodes, (originalChildNode.Tag as TagClass)?.Id ?? -1);
                    if (displayedChildNode != null && displayedChildNode.Checked != nodeChecked)
                    {
                        this.treeViewCluster.AfterCheck -= treeViewCluster_AfterCheck;
                        displayedChildNode.Checked = nodeChecked;
                        this.treeViewCluster.AfterCheck += treeViewCluster_AfterCheck;
                    }
                }
                if (originalChildNode.Nodes.Count > 0) CheckAllOriginalChildNodes(originalChildNode, nodeChecked);
            }
        }

        private void UpdateParentNodeCheckState(TreeNode displayedNode)
        {
            TreeNode displayedParentNode = displayedNode.Parent;
            if (displayedParentNode != null)
            {
                TreeNode originalParentNode = null;
                if (displayedParentNode.Tag is TagClass parentTag && parentTag.Id > 0)
                {
                    originalParentNode = FindNodeInOriginalTree(_originalRootNodes, parentTag.Id);
                }
                if (originalParentNode != null)
                {
                    bool allOriginalChildrenChecked = originalParentNode.Nodes.Cast<TreeNode>().All(child => child.Checked);
                    if (displayedParentNode.Checked != allOriginalChildrenChecked)
                    {
                        this.treeViewCluster.AfterCheck -= treeViewCluster_AfterCheck;
                        displayedParentNode.Checked = allOriginalChildrenChecked;
                        this.treeViewCluster.AfterCheck += treeViewCluster_AfterCheck;
                        originalParentNode.Checked = allOriginalChildrenChecked;
                    }
                }
                UpdateParentNodeCheckState(displayedParentNode);
            }
        }

        private void treeViewCluster_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                TreeNode clickedNode = e.Node;
                bool isChecked = clickedNode.Checked;
                TreeNode originalNode = null;
                if (clickedNode.Tag is TagClass clickedTag && clickedTag.Id > 0)
                {
                    originalNode = FindNodeInOriginalTree(_originalRootNodes, clickedTag.Id);
                }
                if (originalNode != null)
                {
                    if (originalNode.Checked != isChecked) originalNode.Checked = isChecked;
                    CheckAllOriginalChildNodes(originalNode, isChecked);
                    UpdateParentNodeCheckState(clickedNode);
                }
                if (clickedNode != null) treeViewCluster.SelectedNode = clickedNode;
            }
        }

        private int GetDirectAssignmentCount(TagClass tag)
        {
            if (tag == null || tag.IsParent) return 0;
            if (tag.TagType == "DeviceIP") return _tagRepository.GetAssignedDeviceIPCount(tag.Id);
            else if (tag.TagType == "Customer") return _tagRepository.GetAssignedCustomerIds(tag.Id).Count;
            return 0;
        }

        private int GetTotalSubtreeCount(TreeNode node)
        {
            int totalCount = 0;
            if (node.Tag is TagClass tag && !tag.IsParent) totalCount = GetDirectAssignmentCount(tag);
            foreach (TreeNode childNode in node.Nodes) totalCount += GetTotalSubtreeCount(childNode);
            return totalCount;
        }

        private void RefreshNodeLabels(IEnumerable<TreeNode> nodes)
        {
            if (_tagRepository == null) _tagRepository = new TagRepository();
            foreach (TreeNode node in nodes)
            {
                string nodeText;
                if (node.Tag is TagClass tag)
                {
                    nodeText = tag.TagName;
                    int totalSubtreeCount = GetTotalSubtreeCount(node);
                    if (tag.IsParent)
                    {
                        node.ForeColor = Color.Blue;
                        if (totalSubtreeCount == 1) nodeText += " (1) Entity";
                        else if (totalSubtreeCount > 1) nodeText += $" ({totalSubtreeCount}) Entities";
                    }
                    else
                    {
                        node.ForeColor = Color.Black;
                        if (totalSubtreeCount > 0) nodeText += $" ({totalSubtreeCount})";
                    }
                }
                else { nodeText = node.Text; node.ForeColor = Color.Black; }
                node.Text = nodeText;
                if (node.Nodes.Count > 0) RefreshNodeLabels(node.Nodes.Cast<TreeNode>());
            }
        }

        private void FilterTreeView(string filterText)
        {
            filterText = filterText.Trim().ToLower();
            treeViewCluster.BeginUpdate();
            treeViewCluster.Nodes.Clear();
            if (string.IsNullOrEmpty(filterText))
            {
                foreach (TreeNode originalNode in _originalRootNodes) treeViewCluster.Nodes.Add((TreeNode)originalNode.Clone());
            }
            else
            {
                foreach (TreeNode originalNode in _originalRootNodes)
                {
                    TreeNode filteredNode = CloneNodeIfMatches(originalNode, filterText);
                    if (filteredNode != null) treeViewCluster.Nodes.Add(filteredNode);
                }
            }
            treeViewCluster.EndUpdate();
            if (treeViewCluster.Nodes.Count > 0) treeViewCluster.ExpandAll();
        }

        private TreeNode CloneNodeIfMatches(TreeNode originalNode, string filter)
        {
            bool nodeTextMatches = originalNode.Text.ToLower().Contains(filter);
            bool descendantMatches = false;
            List<TreeNode> matchingChildClones = new List<TreeNode>();
            foreach (TreeNode originalChildNode in originalNode.Nodes)
            {
                TreeNode clonedChild = CloneNodeIfMatches(originalChildNode, filter);
                if (clonedChild != null) { descendantMatches = true; matchingChildClones.Add(clonedChild); }
            }
            if (nodeTextMatches || descendantMatches)
            {
                TreeNode clonedNode = (TreeNode)originalNode.Clone();
                clonedNode.Nodes.Clear();
                foreach (var matchingChildClone in matchingChildClones) clonedNode.Nodes.Add(matchingChildClone);
                if (nodeTextMatches) clonedNode.BackColor = Color.Yellow;
                return clonedNode;
            }
            return null;
        }

        private TreeNode FindNodeInOriginalTree(IEnumerable<TreeNode> nodesToSearch, int tagId)
        {
            foreach (TreeNode node in nodesToSearch)
            {
                if (node.Tag is TagClass tag && tag.Id == tagId) return node;
                if (node.Nodes.Count > 0)
                {
                    TreeNode foundInChildren = FindNodeInOriginalTree(node.Nodes.Cast<TreeNode>(), tagId);
                    if (foundInChildren != null) return foundInChildren;
                }
            }
            return null;
        }

        private TreeNode FindDisplayedNode(TreeNodeCollection nodesToSearch, int tagId)
        {
            foreach (TreeNode node in nodesToSearch)
            {
                if (node.Tag is TagClass tag && tag.Id == tagId) return node;
                if (node.Nodes.Count > 0)
                {
                    TreeNode foundInChildren = FindDisplayedNode(node.Nodes, tagId);
                    if (foundInChildren != null) return foundInChildren;
                }
            }
            return null;
        }
        #endregion

        #region Add Netwatch Methods

        // This helper recursively finds all descendant leaf TagIDs under a given node.
        private void FindDescendantLeafTagIdsRecursive(TreeNode node, HashSet<int> leafTagIds)
        {
            if (node.Tag is TagClass currentTag)
            {
                if (!currentTag.IsParent) // It's a leaf tag
                {
                    leafTagIds.Add(currentTag.Id);
                }
                else // It's a parent tag, so recurse for its children
                {
                    foreach (TreeNode childNode in node.Nodes)
                    {
                        FindDescendantLeafTagIdsRecursive(childNode, leafTagIds);
                    }
                }
            }
        }

        // This method collects all *leaf TagIDs* that are effectively selected by any checked node.
        private List<int> GetSelectedLeafTagIds()
        {
            HashSet<int> finalLeafTagIds = new HashSet<int>(); // Use HashSet for automatic duplicate removal
            List<TreeNode> topLevelCheckedNodes = new List<TreeNode>();
            List<TreeNode> allCurrentlyCheckedNodes = new List<TreeNode>();
            Queue<TreeNode> queue = new Queue<TreeNode>();

            // Phase 1: Get all currently checked nodes in the displayed tree
            foreach (TreeNode rootNode in treeViewCluster.Nodes)
            {
                queue.Enqueue(rootNode);
            }
            while (queue.Count > 0)
            {
                TreeNode n = queue.Dequeue();
                if (n.Checked && n.Tag is TagClass)
                {
                    allCurrentlyCheckedNodes.Add(n);
                }
                foreach (TreeNode child in n.Nodes)
                {
                    queue.Enqueue(child);
                }
            }

            // Phase 2: From allUiCheckedNodes, find the "highest level" ones
            foreach (TreeNode checkedNode in allCurrentlyCheckedNodes)
            {
                bool parentIsAlsoInCheckedList = false;
                TreeNode parent = checkedNode.Parent;
                while (parent != null)
                {
                    if (parent.Tag is TagClass && allCurrentlyCheckedNodes.Contains(parent))
                    {
                        parentIsAlsoInCheckedList = true;
                        break;
                    }
                    parent = parent.Parent;
                }
                if (!parentIsAlsoInCheckedList)
                {
                    topLevelCheckedNodes.Add(checkedNode);
                }
            }

            // Phase 3: For each top-level checked node, get its descendant leaf tag IDs
            foreach (TreeNode topNode in topLevelCheckedNodes)
            {
                FindDescendantLeafTagIdsRecursive(topNode, finalLeafTagIds);
            }

            return finalLeafTagIds.ToList();
        }

        private void btnAddNetwatch_Click(object sender, EventArgs e)
        {
            List<int> collectedLeafTagIds = GetSelectedLeafTagIds(); // Use the new method

            if (collectedLeafTagIds.Count == 0)
            {
                MessageBox.Show("Please check at least one tag. This will include all underlying leaf tags for monitoring.", "No Tags Selected for Monitoring", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string clusterName = "Unknown Cluster";
            if (this.ClusterId > 0)
            {
                var cluster = _networkClusterRepository.GetClusterById(this.ClusterId);
                if (cluster != null)
                {
                    clusterName = cluster.ClusterName;
                }
                else
                {
                    MessageBox.Show($"Could not find details for the current Cluster (ID: {this.ClusterId}).", "Cluster Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Cluster information is not available for this control.", "Missing Cluster ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Overlay();

            AddNetwatchConfigForm addForm = new AddNetwatchConfigForm();
            addForm.SourceNetworkClusterId = this.ClusterId;
            addForm.CurrentSourceType = NetwatchSourceType.NetworkCluster;
            addForm.InitialTagIdsToMonitor = collectedLeafTagIds; // Pass the list of LEAF tag IDs

            addForm.StartPosition = FormStartPosition.CenterScreen;
            addForm.ShowDialog();

            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                overlayForm.Close();
            }
        }
        #endregion

        private void txtSearchTags_TextChanged(object sender, EventArgs e)
        {
            FilterTreeView(txtSearchTags.Text);
        }
    }
}
