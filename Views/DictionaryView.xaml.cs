using System.Windows.Controls;
using System.Windows.Data;
using PipeRuleConfigurator.ViewModels;

namespace PipeRuleConfigurator.Views
{
    public partial class DictionaryView : UserControl
    {
        public DictionaryView()
        {
            InitializeComponent();
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // 1. 隐藏/取消生成 "IsSelected" 列
            // 因为我们在 XAML 中已经显式定义了一个 CheckBox 列绑定了 IsSelected，
            // 这里必须取消自动生成，否则会重复显示两列。
            if (e.PropertyName == "IsSelected")
            {
                e.Cancel = true;
                return;
            }

            // 2. 处理 "状态" 列 -> 下拉框
            if (e.PropertyName == "状态")
            {
                var viewModel = this.DataContext as DictionaryViewModel;

                var comboColumn = new DataGridComboBoxColumn();
                comboColumn.Header = "状态";

                comboColumn.SelectedItemBinding = new Binding("状态")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                if (viewModel != null)
                {
                    comboColumn.ItemsSource = viewModel.StatusOptions;
                }
                else
                {
                    comboColumn.ItemsSource = new string[] { "启用", "禁用" };
                }

                e.Column = comboColumn;
            }
            // 3. 简单的日期格式化
            else if (e.PropertyType == typeof(System.DateTime))
            {
                (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd HH:mm";
            }
        }
    }
}