using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipeRuleConfigurator.Data;
using PipeRuleConfigurator.Models;
using PipeRuleConfigurator.Services;
using PipeRuleConfigurator.Common;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace PipeRuleConfigurator.ViewModels
{
    public partial class DictionaryViewModel : ObservableObject
    {
        private readonly IPipeDictionaryService _service;

        [ObservableProperty]
        private ObservableCollection<TreeItem> _treeNodes = new();

        [ObservableProperty]
        private DataView _tableData;

        [ObservableProperty]
        private TreeItem _selectedTreeItem;

        [ObservableProperty]
        private DataRowView _selectedRow;

        [ObservableProperty]
        private bool _isAllSelected;

        [ObservableProperty]
        private string _searchText;

        public Func<string, string> InputProvider { get; set; }

        public DictionaryViewModel()
        {
            _service = new MockDictionaryDataService();
            InitStaticMenu();
        }

        partial void OnSearchTextChanged(string value)
        {
            if (TableData != null) TableData.ApplySearch(value);
        }

        async partial void OnSelectedTreeItemChanged(TreeItem value)
        {
            if (value == null) return;
            await LoadDataForNode(value);
        }

        private async Task LoadDataForNode(TreeItem item)
        {
            SearchText = string.Empty;
            IsAllSelected = false;

            DataTable dt = await _service.GetTableDataAsync(item.Title);

            if (!dt.Columns.Contains("IsSelected"))
            {
                DataColumn selectCol = new DataColumn("IsSelected", typeof(bool));
                selectCol.DefaultValue = false;
                dt.Columns.Add(selectCol);
                selectCol.SetOrdinal(0);
            }

            TableData = dt.DefaultView;
        }

        partial void OnIsAllSelectedChanged(bool value)
        {
            if (TableData?.Table == null) return;
            foreach (DataRow row in TableData.Table.Rows)
            {
                if (row.RowState != DataRowState.Deleted && TableData.Table.Columns.Contains("IsSelected"))
                    row["IsSelected"] = value;
            }
        }

        // --- 功能命令 ---
        [RelayCommand]
        private void AddRow()
        {
            if (TableData == null || TableData.Table == null)
            {
                MessageBox.Show("请先选择左侧的一个节点以加载数据。", "提示");
                return;
            }
            var newRow = TableData.Table.AddNewRow();
            if (TableData.Count > 0) SelectedRow = TableData[TableData.Count - 1];
        }

        [RelayCommand]
        private void AddColumn()
        {
            if (TableData == null || TableData.Table == null) return;
            if (InputProvider == null) return;
            string newColName = InputProvider.Invoke("请输入新列的名称：");
            if (string.IsNullOrWhiteSpace(newColName)) return;
            if (TableData.Table.Columns.Contains(newColName)) { MessageBox.Show("列名已存在。"); return; }

            try { TableData.Table.AddColumn(newColName, typeof(string), isRequired: false); TableData = new DataView(TableData.Table); }
            catch (Exception ex) { MessageBox.Show("添加失败：" + ex.Message); }
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (TableData == null || TableData.Table == null) return;
            if (MessageBox.Show("确定删除勾选行？", "确认", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            int count = TableData.Table.DeleteSelectedRows();
            if (count > 0) { IsAllSelected = false; MessageBox.Show($"已删除 {count} 行。"); }
        }

        [RelayCommand]
        private void DuplicateRow()
        {
            if (SelectedRow == null) { MessageBox.Show("请先选中一行。"); return; }
            TableData.Table.DuplicateRow(SelectedRow.Row);
        }

        [RelayCommand]
        private void Save()
        {
            if (TableData == null || TableData.Table == null) return;
            if (TableData.Table.TryCommit(out string msg)) MessageBox.Show(msg, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show(msg, "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        [RelayCommand]
        private void Edit() { if (TableData != null) MessageBox.Show("请直接在表格中编辑。"); }
        public void ValidateRow(DataRow row) { DataValidationHelper.ValidateRow(row); }

        // --- 初始化菜单 ---
        private void InitStaticMenu()
        {
            var root = new TreeItem { Title = "业务属性字典定义", IconKind = "FolderKey" };

            // 根据你的最新需求更新了菜单结构
            root.Children.Add(new TreeItem { Title = "标准系列", IconKind = "FileDocumentOutline" });

            // A-管材标准 (原 A-管材等级)
            root.Children.Add(new TreeItem { Title = "A-管材标准", IconKind = "AlphaABoxOutline" });

            // B1-主材料
            root.Children.Add(new TreeItem { Title = "B1-主材料", IconKind = "MaterialDesign" });

            // B3-牌号
            root.Children.Add(new TreeItem { Title = "B3-牌号", IconKind = "TagOutline" });

            // C1-法兰标准 (新增)
            root.Children.Add(new TreeItem { Title = "C1-法兰标准", IconKind = "ShapeSquarePlus" });

            // C2-法兰压力等级
            root.Children.Add(new TreeItem { Title = "C2-法兰压力等级", IconKind = "Gauge" });

            // D-壁厚等级
            root.Children.Add(new TreeItem { Title = "D-壁厚等级", IconKind = "ArrowCollapseVertical" });

            root.Children.Add(new TreeItem { Title = "接口表", IconKind = "Table" });

            TreeNodes = new ObservableCollection<TreeItem> { root };
        }
    }
}