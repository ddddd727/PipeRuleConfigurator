using System;
using System.Data;

namespace PipeRuleConfigurator.Common
{
    public static class DataValidationHelper
    {
        /// <summary>
        /// 校验单行数据
        /// </summary>
        public static bool ValidateRow(DataRow row)
        {
            if (row.RowState == DataRowState.Deleted) return true;

            row.ClearErrors();
            bool isValid = true;

            foreach (DataColumn col in row.Table.Columns)
            {
                // 检查该列是否有 "IsRequired" 标记
                if (col.ExtendedProperties.ContainsKey("IsRequired") && (bool)col.ExtendedProperties["IsRequired"])
                {
                    var val = row[col];
                    // 校验逻辑：DbNull 或 空字符串 视为未填
                    if (val == DBNull.Value || string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        row.SetColumnError(col, $"{col.ColumnName} 不能为空");
                        isValid = false;
                    }
                }
            }

            // 如果校验失败，设置行级错误文本，以便在 DataGrid 行头显示感叹号
            if (!isValid && string.IsNullOrEmpty(row.RowError))
            {
                row.RowError = "存在未填写的必填项";
            }

            return isValid;
        }

        /// <summary>
        /// 校验整张表 (通常用于保存前)
        /// </summary>
        public static bool ValidateTable(DataTable table, out string firstErrorMsg)
        {
            firstErrorMsg = string.Empty;
            if (table == null) return true;

            bool allValid = true;
            int rowIndex = 1;

            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                // 复用单行校验逻辑
                if (!ValidateRow(row))
                {
                    if (allValid) // 只记录遇到的第一个错误返回给界面
                    {
                        firstErrorMsg = $"第 {rowIndex} 行校验失败：{row.RowError}";
                    }
                    allValid = false;
                }
                rowIndex++;
            }

            return allValid;
        }
    }
}