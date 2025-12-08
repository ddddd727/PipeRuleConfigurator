using System.Windows;
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

            this.DataContextChanged += DictionaryView_DataContextChanged;

            if (this.DataContext is DictionaryViewModel vm)
            {
                InjectInputProvider(vm);
            }
        }

        private void DictionaryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DictionaryViewModel vm)
            {
                InjectInputProvider(vm);
            }
        }

        private void InjectInputProvider(DictionaryViewModel vm)
        {
            vm.InputProvider = (title) =>
            {
                var dialog = new SimpleInputDialog(title);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    return dialog.InputText;
                }
                return null;
            };
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem != null)
            {
                grid.ScrollIntoView(grid.SelectedItem);
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "状态")
            {
                var viewModel = this.DataContext as DictionaryViewModel;
                var comboColumn = new DataGridComboBoxColumn();
                comboColumn.Header = "状态";
                comboColumn.SelectedItemBinding = new Binding("状态") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

                if (viewModel != null) comboColumn.ItemsSource = viewModel.StatusOptions;
                else comboColumn.ItemsSource = new string[] { "启用", "禁用" };

                e.Column = comboColumn;
            }
            else if (e.PropertyType == typeof(System.DateTime))
            {
                (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd HH:mm";
            }
        }
    }

    // --- 修复后的输入弹窗类 ---
    public class SimpleInputDialog : Window
    {
        private TextBox _textBox;
        public string InputText => _textBox.Text;

        public SimpleInputDialog(string title)
        {
            Title = "输入";
            Width = 350; //稍微加宽一点
            // Height = 150;  <-- 删除固定高度，这就是导致按钮陷下去的原因
            SizeToContent = SizeToContent.Height; // <-- 让窗口自动适应内容高度
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            var stackPanel = new StackPanel { Margin = new Thickness(20) }; // 增加内边距

            stackPanel.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 10), FontSize = 14 });

            _textBox = new TextBox { Margin = new Thickness(0, 0, 0, 20), FontSize = 14 };
            // 让输入框加载时自动获得焦点
            this.Loaded += (s, e) => _textBox.Focus();
            stackPanel.Children.Add(_textBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnOk = new Button { Content = "确定", Width = 80, Height = 30, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
            btnOk.Click += (s, e) => { DialogResult = true; Close(); };

            var btnCancel = new Button { Content = "取消", Width = 80, Height = 30, IsCancel = true };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            stackPanel.Children.Add(btnPanel);

            Content = stackPanel;
        }
    }
}