using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading; // 引用 Dispatcher
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
                if (dialog.ShowDialog() == true) return dialog.InputText;
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
            if (e.PropertyName == "IsSelected") { e.Cancel = true; return; }

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

        // --- 【新增核心逻辑】手动刷新行颜色 ---

        private void RefreshRowColor(DataGridRow row)
        {
            if (row.Item is DataRowView drv)
            {
                var brush = new RowStateToBrushConverter().Convert(drv, typeof(Brush), null, null) as Brush;
                // 如果转换结果有效，手动赋值覆盖默认样式
                if (brush != DependencyProperty.UnsetValue)
                {
                    row.Background = brush;
                }
                else
                {
                    // 恢复默认（这里设为透明，让 DataGrid 自身的交替色生效）
                    row.Background = Brushes.Transparent;
                }
            }
        }

        // 1. 行加载时设置颜色 (针对 AddRow)
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            RefreshRowColor(e.Row);
        }

        // 2. 编辑结束时刷新颜色 (针对 Modified)
        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // 使用 Dispatcher 延迟执行，因为 RowEditEnding 触发时数据还没完全 Commit 到底层 DataTable
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshRowColor(e.Row);
            }), DispatcherPriority.ContextIdle);
        }
    }

    public class SimpleInputDialog : Window
    {
        private TextBox _textBox;
        public string InputText => _textBox.Text;

        public SimpleInputDialog(string title)
        {
            Title = "输入";
            Width = 350;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            stackPanel.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 10), FontSize = 14 });
            _textBox = new TextBox { Margin = new Thickness(0, 0, 0, 20), FontSize = 14 };
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

    public class RowStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataRowView drv)
            {
                switch (drv.Row.RowState)
                {
                    case DataRowState.Added:
                        return new SolidColorBrush(Color.FromRgb(0xDC, 0xF8, 0xC6)); // 浅绿 (新增)
                    case DataRowState.Modified:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xF9, 0xC4)); // 浅黄 (修改)
                    default:
                        return DependencyProperty.UnsetValue;
                }
            }
            return DependencyProperty.UnsetValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}