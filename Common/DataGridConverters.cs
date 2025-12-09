using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PipeRuleConfigurator.Common
{
    // --- 行状态颜色 (行级别：新增=绿，修改=黄) ---
    public class RowStateToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: DataRowView (行数据)
            // values[1]: RefreshTrigger (刷新信号)

            if (values.Length > 0 && values[0] is DataRowView drv)
            {
                switch (drv.Row.RowState)
                {
                    case DataRowState.Added:
                        return new SolidColorBrush(Color.FromRgb(0xDC, 0xF8, 0xC6)); // 浅绿
                    case DataRowState.Modified:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xF9, 0xC4)); // 浅黄
                    default:
                        return DependencyProperty.UnsetValue;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // --- 单元格状态颜色 (精准比对：值变了=橙色) ---
    public class CellChangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: 当前单元格的值
            // values[1]: DataRowView
            // parameter: 列名

            if (values.Length > 1 && values[1] is DataRowView drv && parameter is string colName)
            {
                var row = drv.Row;
                if (row.RowState == DataRowState.Added) return DependencyProperty.UnsetValue;
                if (row.RowState == DataRowState.Modified)
                {
                    if (row.HasVersion(DataRowVersion.Original))
                    {
                        var originalVal = row[colName, DataRowVersion.Original];
                        var currentVal = row[colName, DataRowVersion.Current];
                        if (!Equals(originalVal, currentVal))
                        {
                            return new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82)); // 橙色
                        }
                    }
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}