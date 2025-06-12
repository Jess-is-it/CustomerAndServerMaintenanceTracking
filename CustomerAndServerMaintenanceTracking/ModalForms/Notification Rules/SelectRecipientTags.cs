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

namespace CustomerAndServerMaintenanceTracking.ModalForms.Notification_Rules
{
    public partial class SelectRecipientTags : Form
    {
        private List<SelectableTagItem> _allSelectableTags;
        private TagRepository _tagRepository;

        // To store the final list of selected tag IDs
        public List<int> SelectedTagIds { get; private set; }

        // To pre-load with existing selections
        private HashSet<int> _initiallySelectedTagIds;

        public SelectRecipientTags(List<int> initiallySelectedTagIds = null)
        {
            InitializeComponent();
            _tagRepository = new TagRepository();
            SelectedTagIds = new List<int>();
            _initiallySelectedTagIds = initiallySelectedTagIds != null ? new HashSet<int>(initiallySelectedTagIds) : new HashSet<int>();

            InitializeTagsGrid();
            LoadTags();

            // Wire up events
            this.btnInsertTags.Click += new System.EventHandler(this.btnInsertTags_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged); // <-- ADD THIS LINE

            // Add TextChanged event for a search textbox if you add one
        }

        private void InitializeTagsGrid()
        {
            dataGridViewTags.Columns.Clear();
            dataGridViewTags.AutoGenerateColumns = false;

            DataGridViewCheckBoxColumn selectColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Select",
                Width = 50,
                DataPropertyName = "IsSelected" // From our helper class
            };
            dataGridViewTags.Columns.Add(selectColumn);

            dataGridViewTags.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            dataGridViewTags.Columns.Add(new DataGridViewTextBoxColumn { Name = "TagName", DataPropertyName = "TagName", HeaderText = "Tag Name", Width = 150 });
            dataGridViewTags.Columns.Add(new DataGridViewTextBoxColumn { Name = "TagDescription", DataPropertyName = "TagDescription", HeaderText = "Description", Width = 150 });
            dataGridViewTags.Columns.Add(new DataGridViewTextBoxColumn { Name = "TagType", DataPropertyName = "TagType", HeaderText = "Type", Width = 100 });
            dataGridViewTags.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetworkCluster", DataPropertyName = "NetworkCluster", HeaderText = "Cluster", Width = 120 });

            dataGridViewTags.AllowUserToAddRows = false;
        }

        private void LoadTags()
        {
            try
            {
                List<TagDisplayModel> allTags = _tagRepository.GetAllTagsWithCluster()
                                                    .Where(t => t.TagType != "Parent" && !t.IsParent).ToList();

                // Populate the master list
                _allSelectableTags = allTags.Select(t => new SelectableTagItem(t, _initiallySelectedTagIds.Contains(t.Id))).ToList();

                // Apply the filter, which will initially show all tags
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tags: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridViewTags.DataSource = null;
            }
        }
        private void ApplyFilter()
        {
            if (_allSelectableTags == null) return;

            string searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<SelectableTagItem> filteredList;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredList = _allSelectableTags;
            }
            else
            {
                filteredList = _allSelectableTags
                    .Where(t => (t.TagName?.ToLowerInvariant().Contains(searchText) ?? false) ||
                                (t.TagDescription?.ToLowerInvariant().Contains(searchText) ?? false) ||
                                (t.NetworkCluster?.ToLowerInvariant().Contains(searchText) ?? false) ||
                                (t.TagType?.ToLowerInvariant().Contains(searchText) ?? false))
                    .ToList();
            }

            dataGridViewTags.DataSource = null;
            dataGridViewTags.DataSource = filteredList;
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void btnInsertTags_Click(object sender, EventArgs e)
        {
            SelectedTagIds.Clear();
            if (dataGridViewTags.DataSource is List<SelectableTagItem> selectableItems)
            {
                foreach (var item in selectableItems)
                {
                    if (item.IsSelected)
                    {
                        SelectedTagIds.Add(item.Id);
                    }
                }
            }

            if (!SelectedTagIds.Any())
            {
                MessageBox.Show("No tags selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // Helper class for DataGridView binding
        private class SelectableTagItem
        {
            private TagDisplayModel _tag;
            public bool IsSelected { get; set; }

            public int Id => _tag.Id;
            public string TagName => _tag.TagName;
            public string TagDescription => _tag.TagDescription;
            public string NetworkCluster => _tag.NetworkCluster;
            public string TagType => _tag.TagType;
            public bool IsParent => _tag.IsParent;


            public SelectableTagItem(TagDisplayModel tag, bool isSelected)
            {
                _tag = tag;
                IsSelected = isSelected;
            }
        }
    }
}
