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
            var baseStyle = grid.TryFindResource(typeof(DataGridRow)) as Style;
            if (baseStyle != null) rowStyle.BasedOn = baseStyle;

            var multiBinding = new MultiBinding { Converter = new RowStateToBrushConverter() };
            multiBinding.Bindings.Add(new Binding("."));
            multiBinding.Bindings.Add(new Binding { Path = new PropertyPath("(0)", RefreshTriggerProperty), Source = grid });

            rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, multiBinding));

            var disabledTrigger = new DataTrigger { Binding = new Binding("状态"), Value = "禁用" };
            disabledTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5))));
            disabledTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Gray));

            rowStyle.Triggers.Add(disabledTrigger);
            grid.RowStyle = rowStyle;
        }

        // --- 逻辑 B：注入单元格样式 (CellStyle) ---
        private static void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "IsSelected") { e.Cancel = true; return; }

            var grid = sender as DataGrid;

            // ============== 【新增】必填项表头处理 Star Logic ==============
            if (grid.ItemsSource is DataView dv)
            {
                var table = dv.Table;
                if (table.Columns.Contains(e.PropertyName))
                {
                    var col = table.Columns[e.PropertyName];
                    // 检查我们在 Service 里设置的属性
                    if (col.ExtendedProperties.ContainsKey("IsRequired") && (bool)col.ExtendedProperties["IsRequired"])
                    {
                        // 给表头加上星号
                        e.Column.Header = e.PropertyName + " *";
                    }
                }
            }
            // =============================================================

            var cellStyle = new Style(typeof(DataGridCell));
            var baseCellStyle = grid.TryFindResource(typeof(DataGridCell)) as Style;
            if (baseCellStyle != null) cellStyle.BasedOn = baseCellStyle;

            var multiBinding = new MultiBinding { Converter = new CellChangeConverter(), ConverterParameter = e.PropertyName };
            multiBinding.Bindings.Add(new Binding(e.PropertyName) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            multiBinding.Bindings.Add(new Binding("."));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, multiBinding));

            void AddTransparentTrigger(DependencyProperty prop)
            {
                var trigger = new Trigger { Property = prop, Value = true };
                trigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                cellStyle.Triggers.Add(trigger);
            }
            AddTransparentTrigger(DataGridCell.IsSelectedProperty);
            AddTransparentTrigger(DataGridCell.IsMouseOverProperty);

            var rowSelectedTrigger = new DataTrigger { Binding = new Binding("IsSelected") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) }, Value = true };
            rowSelectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(rowSelectedTrigger);

            var rowMouseOverTrigger = new DataTrigger { Binding = new Binding("IsMouseOver") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) }, Value = true };
            rowMouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Triggers.Add(rowMouseOverTrigger);

            if (e.PropertyName == "状态")
            {
                var comboColumn = new DataGridComboBoxColumn { Header = "状态 *" }; // 状态通常也是必填的
                comboColumn.SelectedItemBinding = new Binding("状态") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

                var comboStyle = grid.TryFindResource("MaterialDesignDataGridComboBoxColumnStyle") as Style;
                if (comboStyle != null) comboColumn.ElementStyle = comboStyle;

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
            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                ForceRefresh(grid);
            }), DispatcherPriority.ContextIdle);
        }
    }
}