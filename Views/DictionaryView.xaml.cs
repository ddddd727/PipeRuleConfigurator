using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using PipeRuleConfigurator.ViewModels;

namespace PipeRuleConfigurator.Views
{
    public partial class DictionaryView : UserControl
    {
        // 刷新信号依赖属性
        public static readonly DependencyProperty RefreshTriggerProperty =
            DependencyProperty.Register("RefreshTrigger", typeof(int), typeof(DictionaryView), new PropertyMetadata(0));

        public int RefreshTrigger
        {
            get { return (int)GetValue(RefreshTriggerProperty); }
            set { SetValue(RefreshTriggerProperty, value); }
        }

        public DictionaryView()
        {
            InitializeComponent();
            this.DataContextChanged += DictionaryView_DataContextChanged;
            if (this.DataContext is DictionaryViewModel vm) InjectInputProvider(vm);
        }

        private void DictionaryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DictionaryViewModel vm) InjectInputProvider(vm);
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

        // --- 核心修改 1：拦截编辑操作 ---
        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // 1. 获取列名 (Header 或 SortMemberPath)
            string colName = e.Column.Header as string;
            // 兼容自动生成的列名情况
            if (string.IsNullOrEmpty(colName)) colName = e.Column.SortMemberPath;

            // 2. 如果是“状态”列，永远允许编辑（否则禁用了就切不回去了）
            if (colName == "状态") return;

            // 3. 检查当前行的状态
            if (e.Row.Item is DataRowView drv)
            {
                // 假设状态列的值是 "禁用" 或 "启用"
                string status = drv["状态"]?.ToString();

                if (status == "禁用")
                {
                    // 如果状态是禁用，且试图编辑其他列，则取消编辑
                    e.Cancel = true;
                }
            }
        }

        // --- 核心修改 2：在列生成时，注入单元格样式 ---
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "IsSelected") { e.Cancel = true; return; }

            // 创建单元格样式
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.BasedOn = (Style)this.TryFindResource(typeof(DataGridCell));

            // 设置背景色绑定 (精准高亮)
            var multiBinding = new MultiBinding();
            multiBinding.Converter = new CellChangeConverter();
            multiBinding.ConverterParameter = e.PropertyName;
            multiBinding.Bindings.Add(new Binding(e.PropertyName) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            multiBinding.Bindings.Add(new Binding("."));
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, multiBinding));

            // 防止遮挡 Hover/Selected 效果
            var selectedTrigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(selectedTrigger);

            var mouseOverTrigger = new Trigger { Property = DataGridCell.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(mouseOverTrigger);

            // 应用样式
            if (e.PropertyName == "状态")
            {
                var viewModel = this.DataContext as DictionaryViewModel;
                var comboColumn = new DataGridComboBoxColumn();
                comboColumn.Header = "状态";
                comboColumn.SelectedItemBinding = new Binding("状态") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

                var comboStyle = (Style)this.TryFindResource("MaterialDesignDataGridComboBoxColumnStyle");
                if (comboStyle != null) comboColumn.ElementStyle = comboStyle;

                if (viewModel != null) comboColumn.ItemsSource = viewModel.StatusOptions;
                else comboColumn.ItemsSource = new string[] { "启用", "禁用" };

                comboColumn.CellStyle = cellStyle;
                e.Column = comboColumn;
            }
            else
            {
                e.Column.CellStyle = cellStyle;
                if (e.PropertyType == typeof(System.DateTime))
                {
                    (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd HH:mm";
                }
            }
        }

        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshTrigger++;
            }), DispatcherPriority.ContextIdle);
        }
    }

    // --- 辅助类 ---
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

    public class RowStateToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is DataRowView drv)
            {
                switch (drv.Row.RowState)
                {
                    case DataRowState.Added: return new SolidColorBrush(Color.FromRgb(0xDC, 0xF8, 0xC6));
                    case DataRowState.Modified: return new SolidColorBrush(Color.FromRgb(0xFF, 0xF9, 0xC4));
                    default: return DependencyProperty.UnsetValue;
                }
            }
            return DependencyProperty.UnsetValue;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class CellChangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 1 && values[1] is DataRowView drv && parameter is string colName)
            {
                var row = drv.Row;
                if (row.RowState == DataRowState.Added) return DependencyProperty.UnsetValue;
                if (row.RowState == DataRowState.Modified && row.HasVersion(DataRowVersion.Original))
                {
                    var originalVal = row[colName, DataRowVersion.Original];
                    var currentVal = row[colName, DataRowVersion.Current];
                    if (!Equals(originalVal, currentVal)) return new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82));
                }
            }
            return DependencyProperty.UnsetValue;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}