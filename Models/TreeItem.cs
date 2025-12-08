using System.Collections.ObjectModel;

namespace PipeRuleConfigurator.Models
{
    public class TreeItem
    {
        public string Title { get; set; } = string.Empty;
        public string IconKind { get; set; } = "Folder";

        // 子菜单集合
        public ObservableCollection<TreeItem> Children { get; set; } = new ObservableCollection<TreeItem>();
    }
}