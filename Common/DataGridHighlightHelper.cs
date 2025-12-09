using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace PipeRuleConfigurator.Common
{
    public static class DataGridHighlightHelper
    {
        // 1. 定义“启用”附加属性
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(DataGridHighlightHelper), new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        // 2. 定义内部使用的“刷新信号”附加属性
        public static readonly DependencyProperty RefreshTriggerProperty =
            DependencyProperty.RegisterAttached("RefreshTrigger", typeof(int), typeof(DataGridHighlightHelper), new PropertyMetadata(0));

        public static int GetRefreshTrigger(DependencyObject obj) => (int)obj.GetValue(RefreshTriggerProperty);
        public static void SetRefreshTrigger(DependencyObject obj, int value) => obj.SetValue(RefreshTriggerProperty, value);

        // --- 属性变更回调 ---
        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid grid)
            {
                if ((bool)e.NewValue)
                {
                    // 启用：订阅事件，注入行样式
                    grid.AutoGeneratingColumn += Grid_AutoGeneratingColumn;
                    grid.LoadingRow += Grid_LoadingRow;
                    grid.RowEditEnding += Grid_RowEditEnding;

                    ApplyRowStyle(grid);
                }
                else
                {
                    // 禁用：取消订阅
                    grid.AutoGeneratingColumn -= Grid_AutoGeneratingColumn;
                    grid.LoadingRow -= Grid_LoadingRow;
                    grid.RowEditEnding -= Grid_RowEditEnding;
                }
            }
        }

        // --- 逻辑实现：注入行样式 ---
        private static void ApplyRowStyle(DataGrid grid)
        {
            // 创建一个新的行样式
            var rowStyle = new Style(typeof(DataGridRow));
            // 尝试继承默认样式 (MaterialDesign)
            var baseStyle = grid.TryFindResource(typeof(DataGridRow)) as Style;
            if (baseStyle != null) rowStyle.BasedOn = baseStyle;

            // 设置背景色 MultiBinding
            var multiBinding = new MultiBinding { Converter = new RowStateToBrushConverter() };
            multiBinding.Bindings.Add(new Binding(".")); // 行数据
            // 绑定到 DataGrid 自身的 RefreshTrigger 附加属性
            multiBinding.Bindings.Add(new Binding { Path = new PropertyPath("(0)", RefreshTriggerProperty), Source = grid });

            rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, multiBinding));

            grid.RowStyle = rowStyle;
        }

        // --- 逻辑实现：注入单元格样式 (AutoGeneratingColumn) ---
        private static void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "IsSelected") { e.Cancel = true; return; }

            var grid = sender as DataGrid;

            // 创建单元格样式
            var cellStyle = new Style(typeof(DataGridCell));
            var baseCellStyle = grid.TryFindResource(typeof(DataGridCell)) as Style;
            if (baseCellStyle != null) cellStyle.BasedOn = baseCellStyle;

            // 1. 设置背景色逻辑 (精准高亮)
            var multiBinding = new MultiBinding { Converter = new CellChangeConverter(), ConverterParameter = e.PropertyName };
            multiBinding.Bindings.Add(new Binding(e.PropertyName) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            multiBinding.Bindings.Add(new Binding("."));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, multiBinding));

            // 2. 处理 Hover/Selected 透明 (防止遮挡)
            var selectedTrigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(selectedTrigger);

            var mouseOverTrigger = new Trigger { Property = DataGridCell.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(mouseOverTrigger);

            // 行级别的 Hover/Selected 处理
            var rowSelectedTrigger = new DataTrigger
            {
                Binding = new Binding("IsSelected") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) },
                Value = true
            };
            rowSelectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(rowSelectedTrigger);

            var rowMouseOverTrigger = new DataTrigger
            {
                Binding = new Binding("IsMouseOver") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) },
                Value = true
            };
            rowMouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(rowMouseOverTrigger);


            // 3. 应用样式
            if (e.PropertyName == "状态")
            {
                var comboColumn = new DataGridComboBoxColumn { Header = "状态" };
                comboColumn.SelectedItemBinding = new Binding("状态") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                var comboStyle = grid.TryFindResource("MaterialDesignDataGridComboBoxColumnStyle") as Style;
                if (comboStyle != null) comboColumn.ElementStyle = comboStyle;

                // 这里需要一点反射或者约定来获取 ViewModel 的 StatusOptions，
                // 或者简单起见，这里硬编码默认值，实际项目建议使用 DataTemplateSelector 或 Behavior 参数化
                comboColumn.ItemsSource = new string[] { "启用", "禁用" };

                comboColumn.CellStyle = cellStyle;
                e.Column = comboColumn;
            }
            else
            {
                e.Column.CellStyle = cellStyle;
                if (e.PropertyType == typeof(DateTime))
                    (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd HH:mm";
            }
        }

        // --- 逻辑实现：刷新触发器 ---
        private static void ForceRefresh(DataGrid grid)
        {
            int current = GetRefreshTrigger(grid);
            SetRefreshTrigger(grid, current + 1);
        }

        private static void Grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // 初始加载时触发一次刷新以确保颜色正确
            ForceRefresh(sender as DataGrid);
        }

        private static void Grid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var grid = sender as DataGrid;
            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                ForceRefresh(grid);
            }), DispatcherPriority.ContextIdle);
        }
    }
}