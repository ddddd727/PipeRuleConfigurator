using System;
using System.Data;

namespace PipeRuleConfigurator.Common
{
    public static class DataTableExtensions
    {
        // 1. 快捷添加列
        public static DataColumn AddColumn(this DataTable dt, string columnName, Type dataType, bool isRequired = false)
        {
            DataColumn col = dt.Columns.Add(columnName, dataType);
            if (isRequired)
            {
                col.ExtendedProperties["IsRequired"] = true;
            }
            return col;
        }

        // 2. 批量删除选中行
        public static int DeleteSelectedRows(this DataTable dt)
        {
            if (!dt.Columns.Contains("IsSelected")) return 0;

            int count = 0;
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                DataRow row = dt.Rows[i];
                if (row.RowState != DataRowState.Deleted)
                {
                    if (row["IsSelected"] != DBNull.Value && (bool)row["IsSelected"])
                    {
                        row.Delete();
                        count++;
                    }
                }
            }
            return count;
        }

        // 3. 复制行
        public static void DuplicateRow(this DataTable dt, DataRow sourceRow)
        {
            if (sourceRow == null) return;

            DataRow newRow = dt.NewRow();
            newRow.ItemArray = sourceRow.ItemArray;

            // 重置关键字段
            if (dt.Columns.Contains("ID")) newRow["ID"] = DBNull.Value;
            if (dt.Columns.Contains("IsSelected")) newRow["IsSelected"] = false;
            if (dt.Columns.Contains("状态")) newRow["状态"] = "启用";

            int currentIndex = dt.Rows.IndexOf(sourceRow);
            if (currentIndex >= 0 && currentIndex < dt.Rows.Count - 1)
                dt.Rows.InsertAt(newRow, currentIndex + 1);
            else
                dt.Rows.Add(newRow);
        }

        // 4. 【报错修复核心】通用增加行
        public static DataRow AddNewRow(this DataTable dt)
        {
            DataRow newRow = dt.NewRow();

            // 自动填入通用默认值
            if (dt.Columns.Contains("IsSelected")) newRow["IsSelected"] = false;
            if (dt.Columns.Contains("状态")) newRow["状态"] = "启用";
            if (dt.Columns.Contains("更新时间")) newRow["更新时间"] = DateTime.Now;

            dt.Rows.Add(newRow);
            return newRow;
        }

        // 5. 【报错修复核心】通用提交 (含校验)
        public static bool TryCommit(this DataTable dt, out string message)
        {
            message = string.Empty;
            if (dt == null) return false;

            // 调用校验帮助类
            if (!DataValidationHelper.ValidateTable(dt, out string errorMsg))
            {
                message = $"保存失败：\n{errorMsg}";
                return false;
            }

            // 统计变更
            int added = 0, modified = 0, deleted = 0;
            foreach (DataRow row in dt.Rows)
            {
                if (row.RowState == DataRowState.Added) added++;
                else if (row.RowState == DataRowState.Modified) modified++;
                else if (row.RowState == DataRowState.Deleted) deleted++;
            }

            // 提交更改
            dt.AcceptChanges();

            message = $"保存成功！\n(新增: {added}, 修改: {modified}, 删除: {deleted})";
            return true;
        }
    }
}