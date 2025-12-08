using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipeRuleConfigurator.Data;
using PipeRuleConfigurator.Models;
using PipeRuleConfigurator.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.ViewModels
{
    public partial class DictionaryViewModel : ObservableObject
    {
        // 确保使用的是 IPipeDictionaryService 接口
        private readonly IPipeDictionaryService _service;

        [ObservableProperty]
        private ObservableCollection<TreeItem> _treeNodes = new();

        [ObservableProperty]
        private DataView _tableData;

        // 绑定 ListBox 选中项
        [ObservableProperty]
        private TreeItem _selectedTreeItem;

        [ObservableProperty]
        private string _selectedNodeTitle = "未选择";

        public DictionaryViewModel()
        {
            // 初始化服务 (Mock 或 Sql)
            _service = new MockDictionaryService();

            InitStaticMenu();
        }

        // 当选中项改变时触发
        async partial void OnSelectedTreeItemChanged(TreeItem value)
        {
            if (value == null) return;

            SelectedNodeTitle = value.Title;

            // 调用查库
            await LoadDataForNode(value);
        }

        private async Task LoadDataForNode(TreeItem item)
        {
            DataTable dt = await _service.GetTableDataAsync(item.Title);
            TableData = dt.DefaultView;
        }

        private void InitStaticMenu()
        {
            var root = new TreeItem { Title = "业务属性字典定义", IconKind = "FolderKey" };

            root.Children.Add(new TreeItem { Title = "标准系列", IconKind = "FileDocumentOutline" });
            root.Children.Add(new TreeItem { Title = "标准", IconKind = "FileDocumentOutline" });
            root.Children.Add(new TreeItem { Title = "A-管材等级", IconKind = "AlphaABoxOutline" });
            root.Children.Add(new TreeItem { Title = "B1-主材料", IconKind = "MaterialDesign" });
            root.Children.Add(new TreeItem { Title = "B3-牌号", IconKind = "TagOutline" });
            root.Children.Add(new TreeItem { Title = "C2-法兰压力等级", IconKind = "Gauge" });
            root.Children.Add(new TreeItem { Title = "D-壁厚等级", IconKind = "ArrowCollapseVertical" });
            root.Children.Add(new TreeItem { Title = "接口表", IconKind = "Table" });

            TreeNodes = new ObservableCollection<TreeItem> { root };
        }

        // 按钮命令
        [RelayCommand] private void AddRow() { }
        [RelayCommand] private void AddColumn() { }
        [RelayCommand] private void Edit() { }
        [RelayCommand] private void Save() { }
    }
}