using System.Data;
using System.Text;

namespace PipeRuleConfigurator.Common
{
    public static class DataViewExtensions
    {
        /// <summary>
        /// 对 DataView 应用模糊搜索 (搜索所有字符串类型的列)
        /// </summary>
        public static void ApplySearch(this DataView dv, string keyword)
        {
            if (dv == null) return;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                dv.RowFilter = string.Empty; // 清空过滤
                return;
            }

            // 构造 SQL 风格的过滤语句: Name LIKE '%key%' OR Code LIKE '%key%' ...
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;

            foreach (DataColumn col in dv.Table.Columns)
            {
                // 只搜索 String 类型的列，避免类型转换错误
                if (col.DataType == typeof(string))
                {
                    if (!isFirst) sb.Append(" OR ");
                    // 注意转义单引号，防止 crash
                    sb.Append($"[{col.ColumnName}] LIKE '%{keyword.Replace("'", "''")}%'");
                    isFirst = false;
                }
            }

            // 如果没有字符串列，就不过滤
            dv.RowFilter = isFirst ? string.Empty : sb.ToString();
        }
    }
}