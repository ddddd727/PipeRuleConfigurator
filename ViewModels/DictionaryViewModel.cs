using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipeRuleConfigurator.Data;
using PipeRuleConfigurator.Models;
using PipeRuleConfigurator.Services;
using System;
using System.Collections.Generic;
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
        private string _selectedNodeTitle = "未选择";

        // 选中行 (用于自动滚动)
        [ObservableProperty]
        private DataRowView _selectedRow;

        [ObservableProperty]
        private bool _isAllSelected;

        // --- 新增：输入提供者 (由 View 层赋值) ---
        // 参数是提示标题，返回值是用户输入的字符串
        public Func<string, string> InputProvider { get; set; }

        // 下拉框选项源
        public List<string> StatusOptions { get; } = new List<string> { "启用", "禁用" };

        public DictionaryViewModel()
        {
            _service = new MockDictionaryService();
            InitStaticMenu();
        }

        partial void OnIsAllSelectedChanged(bool value)
        {
            if (TableData?.Table == null) return;
            foreach (DataRow row in TableData.Table.Rows)
            {
                if (row.RowState != DataRowState.Deleted && TableData.Table.Columns.Contains("IsSelected"))
                {
                    row["IsSelected"] = value;
                }
            }
        }

        async partial void OnSelectedTreeItemChanged(TreeItem value)
        {
            if (value == null) return;
            SelectedNodeTitle = value.Title;
            await LoadDataForNode(value);
        }

        private async Task LoadDataForNode(TreeItem item)
        {
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

        private void InitStaticMenu()
        {
            var root = new TreeItem { Title = "业务属性字典定义", IconKind = "FolderKey" };
            root.Children.Add(new TreeItem { Title = "标准系列", IconKind = "FileDocumentOutline" });
            root.Children.Add(new TreeItem { Title = "A-管材等级", IconKind = "AlphaABoxOutline" });
            root.Children.Add(new TreeItem { Title = "B1-主材料", IconKind = "MaterialDesign" });
            root.Children.Add(new TreeItem { Title = "B3-牌号", IconKind = "TagOutline" });
            root.Children.Add(new TreeItem { Title = "C2-法兰压力等级", IconKind = "Gauge" });
            root.Children.Add(new TreeItem { Title = "D-壁厚等级", IconKind = "ArrowCollapseVertical" });
            root.Children.Add(new TreeItem { Title = "接口表", IconKind = "Table" });
            TreeNodes = new ObservableCollection<TreeItem> { root };
        }

        // --- 功能实现 ---

        [RelayCommand]
        private void AddRow()
        {
            if (TableData == null || TableData.Table == null)
            {
                MessageBox.Show("请先选择左侧的一个节点以加载数据。");
                return;
            }

            DataRow newRow = TableData.Table.NewRow();
            if (TableData.Table.Columns.Contains("IsSelected")) newRow["IsSelected"] = false;
            if (TableData.Table.Columns.Contains("状态")) newRow["状态"] = "启用";
            if (TableData.Table.Columns.Contains("更新时间")) newRow["更新时间"] = DateTime.Now;

            TableData.Table.Rows.Add(newRow);

            if (TableData.Count > 0)
            {
                SelectedRow = TableData[TableData.Count - 1];
            }
        }

        [RelayCommand]
        private void AddColumn()
        {
            if (TableData == null || TableData.Table == null)
            {
                MessageBox.Show("请先加载数据表！");
                return;
            }

            // 1. 调用 View 提供的输入框
            if (InputProvider == null)
            {
                MessageBox.Show("未初始化输入组件。");
                return;
            }

            string newColName = InputProvider.Invoke("请输入新列的名称：");

            // 如果用户取消或输入为空，则不处理
            if (string.IsNullOrWhiteSpace(newColName)) return;

            // 2. 校验重复
            if (TableData.Table.Columns.Contains(newColName))
            {
                MessageBox.Show($"列名 [{newColName}] 已存在，请重试。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 3. 添加列
                TableData.Table.Columns.Add(new DataColumn(newColName, typeof(string)) { AllowDBNull = true });

                // 4. 强制刷新：创建新的 DataView
                TableData = new DataView(TableData.Table);
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加列失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private void Edit()
        {
            if (TableData == null) return;
            bool hasChecked = false;
            foreach (DataRow row in TableData.Table.Rows)
            {
                if (row.RowState != DataRowState.Deleted && row["IsSelected"] is bool isSelected && isSelected)
                {
                    hasChecked = true;
                    break;
                }
            }

            if (!hasChecked)
                MessageBox.Show("请先勾选需要编辑的行！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("已进入编辑模式，请直接修改单元格内容。", "编辑", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Save()
        {
            if (TableData == null || TableData.Table == null) return;

            if (!ValidateData(out string errorMsg))
            {
                MessageBox.Show($"保存失败：\n{errorMsg}", "校验错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int added = 0, modified = 0;
            foreach (DataRow row in TableData.Table.Rows)
            {
                if (row.RowState == DataRowState.Added) added++;
                if (row.RowState == DataRowState.Modified) modified++;
            }
            TableData.Table.AcceptChanges();

            MessageBox.Show($"保存成功！\n(新增: {added} 行, 修改: {modified} 行)",
                            "系统提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ValidateData(out string errorMsg)
        {
            errorMsg = string.Empty;
            int rowIndex = 1;
            foreach (DataRow row in TableData.Table.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                if (TableData.Table.Columns.Count > 1)
                {
                    string firstDataColName = TableData.Table.Columns[1].ColumnName;
                    var val = row[firstDataColName]?.ToString();
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        errorMsg = $"第 {rowIndex} 行数据不完整：[{firstDataColName}] 不能为空。";
                        return false;
                    }
                }
                rowIndex++;
            }
            return true;
        }
    }
}