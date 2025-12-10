using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipeRuleConfigurator.Data; // 引用 MockBasicDataService
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
    public partial class BasicClassViewModel : ObservableObject
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

        // 注入 View 层的弹窗逻辑
        public Func<string, string> InputProvider { get; set; }

        // 下拉框数据源
        public List<string> StatusOptions { get; } = new List<string> { "启用", "禁用" };

        public BasicClassViewModel()
        {
            // 【关键修改】这里切换为基础类专属的 Mock 服务
            _service = new MockBasicDataService();

            InitBasicMenu();
        }

        private void InitBasicMenu()
        {
            var root = new TreeItem { Title = "基础类", IconKind = "Folder" };

            // 定义二级菜单 (这些名字必须和 MockBasicDataService 里的判断一致)
            root.Children.Add(new TreeItem { Title = "弯管数据", IconKind = "Pipe" });
            root.Children.Add(new TreeItem { Title = "壁厚系列", IconKind = "Ruler" });

            TreeNodes = new ObservableCollection<TreeItem> { root };
        }

        // --- 以下逻辑与 DictionaryViewModel 保持一致 ---

        async partial void OnSelectedTreeItemChanged(TreeItem value)
        {
            if (value == null) return;
            await LoadDataForNode(value);
        }

        private async Task LoadDataForNode(TreeItem item)
        {
            IsAllSelected = false;
            DataTable dt = await _service.GetTableDataAsync(item.Title);

            // 注入前端辅助列
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
                if (row.RowState != DataRowState.Deleted) row["IsSelected"] = value;
            }
        }

        [RelayCommand]
        private void AddRow()
        {
            if (TableData == null)
            {
                MessageBox.Show("请先选择左侧的一个节点以加载数据。");
                return;
            }

            DataRow newRow = TableData.Table.NewRow();
            if (TableData.Table.Columns.Contains("IsSelected")) newRow["IsSelected"] = false;
            if (TableData.Table.Columns.Contains("状态")) newRow["状态"] = "启用";
            // 如果有更新时间列，自动填入
            if (TableData.Table.Columns.Contains("更新时间")) newRow["更新时间"] = DateTime.Now;

            TableData.Table.Rows.Add(newRow);

            if (TableData.Count > 0) SelectedRow = TableData[TableData.Count - 1];
        }

        [RelayCommand]
        private void AddColumn()
        {
            if (TableData == null) return;
            if (InputProvider == null) { MessageBox.Show("输入组件未初始化"); return; }

            string newColName = InputProvider.Invoke("请输入新列的名称：");
            if (string.IsNullOrWhiteSpace(newColName)) return;

            if (TableData.Table.Columns.Contains(newColName))
            {
                MessageBox.Show("列名已存在。");
                return;
            }

            try
            {
                TableData.Table.Columns.Add(new DataColumn(newColName, typeof(string)) { AllowDBNull = true });
                TableData = new DataView(TableData.Table); // 强制刷新
            }
            catch (Exception ex) { MessageBox.Show("添加失败：" + ex.Message); }
        }

        [RelayCommand]
        private void Edit()
        {
            MessageBox.Show("请直接双击单元格修改。\n注：禁用行不可编辑。", "提示");
        }

        [RelayCommand]
        private void Save()
        {
            if (TableData == null) return;

            // 可以在这里加一些基础校验
            // if (TableData.Table.Columns.Count > 1) { ... }

            TableData.Table.AcceptChanges(); // 提交更改 (颜色恢复白色)
            MessageBox.Show("数据保存成功！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}