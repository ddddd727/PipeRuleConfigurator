using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace PipeRuleConfigurator.Common
{
    public static class DataGridHighlightHelper
    {
        // --- 1. 定义“启用”附加属性 ---
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(DataGridHighlightHelper), new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        // --- 2. 定义内部使用的“刷新信号” ---
        private static readonly DependencyProperty RefreshTriggerProperty =
            DependencyProperty.RegisterAttached("RefreshTrigger", typeof(int), typeof(DataGridHighlightHelper), new PropertyMetadata(0));

        private static int GetRefreshTrigger(DependencyObject obj) => (int)obj.GetValue(RefreshTriggerProperty);
        private static void SetRefreshTrigger(DependencyObject obj, int value) => obj.SetValue(RefreshTriggerProperty, value);

        // --- 属性变更回调：自动订阅/取消订阅事件 ---
        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid grid)
            {
                if ((bool)e.NewValue)
                {
                    grid.AutoGeneratingColumn += Grid_AutoGeneratingColumn;
                    grid.LoadingRow += Grid_LoadingRow;
                    grid.RowEditEnding += Grid_RowEditEnding;

                    // 立即注入行样式
                    ApplyRowStyle(grid);
                }
                else
                {
                    grid.AutoGeneratingColumn -= Grid_AutoGeneratingColumn;
                    grid.LoadingRow -= Grid_LoadingRow;
                    grid.RowEditEnding -= Grid_RowEditEnding;
                }
            }
        }

        // --- 逻辑 A：注入行样式 (RowStyle) ---
        private static void ApplyRowStyle(DataGrid grid)
        {
            var rowStyle = new Style(typeof(DataGridRow));
            // 继承默认样式 (MaterialDesign)
            var baseStyle = grid.TryFindResource(typeof(DataGridRow)) as Style;
            if (baseStyle != null) rowStyle.BasedOn = baseStyle;

            // 1. 背景色绑定 (新增=绿, 修改=黄)
            var multiBinding = new MultiBinding { Converter = new RowStateToBrushConverter() };
            multiBinding.Bindings.Add(new Binding(".")); // 行数据
            // 绑定到 Helper 的 RefreshTrigger 以实现强制刷新
            multiBinding.Bindings.Add(new Binding { Path = new PropertyPath("(0)", RefreshTriggerProperty), Source = grid });

            rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, multiBinding));

            // 2. 触发器：如果“状态”是“禁用”，整行变灰 (视觉上不可编辑)
            var disabledTrigger = new DataTrigger { Binding = new Binding("状态"), Value = "禁用" };
            disabledTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)))); // 浅灰背景
            disabledTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Gray)); // 灰色文字

            rowStyle.Triggers.Add(disabledTrigger);

            grid.RowStyle = rowStyle;
        }

        // --- 逻辑 B：注入单元格样式 (CellStyle) ---
        private static void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "IsSelected") { e.Cancel = true; return; }

            var grid = sender as DataGrid;

            // 创建单元格样式
            var cellStyle = new Style(typeof(DataGridCell));
            var baseCellStyle = grid.TryFindResource(typeof(DataGridCell)) as Style;
            if (baseCellStyle != null) cellStyle.BasedOn = baseCellStyle;

            // 1. 设置背景色 (精准高亮：值变了=橙色)
            var multiBinding = new MultiBinding { Converter = new CellChangeConverter(), ConverterParameter = e.PropertyName };
            multiBinding.Bindings.Add(new Binding(e.PropertyName) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            multiBinding.Bindings.Add(new Binding("."));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, multiBinding));

            // 2. 防止遮挡 Hover/Selected 效果 (设为透明，透出 Row 的颜色)
            void AddTransparentTrigger(DependencyProperty prop)
            {
                var trigger = new Trigger { Property = prop, Value = true };
                trigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                cellStyle.Triggers.Add(trigger);
            }
            AddTransparentTrigger(DataGridCell.IsSelectedProperty);
            AddTransparentTrigger(DataGridCell.IsMouseOverProperty);

            // 同时监听 Row 级别的 Hover/Selected，防止闪烁
            var rowSelectedTrigger = new DataTrigger { Binding = new Binding("IsSelected") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) }, Value = true };
            rowSelectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(rowSelectedTrigger);

            var rowMouseOverTrigger = new DataTrigger { Binding = new Binding("IsMouseOver") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) }, Value = true };
            rowMouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(rowMouseOverTrigger);


            // 3. 处理特殊列：状态列 (ComboBox)
            if (e.PropertyName == "状态")
            {
                var comboColumn = new DataGridComboBoxColumn { Header = "状态" };
                comboColumn.SelectedItemBinding = new Binding("状态") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

                var comboStyle = grid.TryFindResource("MaterialDesignDataGridComboBoxColumnStyle") as Style;
                if (comboStyle != null) comboColumn.ElementStyle = comboStyle;

                // 这里简单给个默认源，实际项目可以用 Behavior 绑定 ViewModel 里的源
                comboColumn.ItemsSource = new string[] { "启用", "禁用" };

                comboColumn.CellStyle = cellStyle;
                e.Column = comboColumn;
            }
            else
            {
                // 普通列
                e.Column.CellStyle = cellStyle;
                if (e.PropertyType == typeof(DateTime))
                    (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd HH:mm";
            }
        }

        // --- 逻辑 C：刷新触发器 ---
        private static void ForceRefresh(DataGrid grid)
        {
            int current = GetRefreshTrigger(grid);
            SetRefreshTrigger(grid, current + 1);
        }

        private static void Grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            ForceRefresh(sender as DataGrid);
        }

        private static void Grid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var grid = sender as DataGrid;
            // 延迟刷新，等待数据提交
            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                ForceRefresh(grid);
            }), DispatcherPriority.ContextIdle);
        }
    }
}