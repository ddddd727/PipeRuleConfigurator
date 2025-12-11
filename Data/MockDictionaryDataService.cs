using Bogus;
using PipeRuleConfigurator.Services;
using PipeRuleConfigurator.Common; // 引用扩展方法
using System;
using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.Data
{
    public class MockDictionaryDataService : IPipeDictionaryService
    {
        public async Task<DataTable> GetTableDataAsync(string nodeTitle)
        {
            await Task.Delay(150); // 模拟极速加载

            var dt = new DataTable();
            var faker = new Faker("zh_CN");

            // ==========================================
            // 1. B1-主材料
            // 字段：ID、主材料、主材料编码、状态
            // ==========================================
            if (nodeTitle == "B1-主材料")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("主材料", typeof(string), isRequired: true);
                dt.AddColumn("主材料编码", typeof(string), isRequired: true);
                dt.AddColumn("状态", typeof(string), isRequired: true);

                // 模拟数据
                var materials = new[] { "CS", "SS", "LTCS", "LAS", "DSS" };
                for (int i = 0; i < materials.Length; i++)
                {
                    dt.Rows.Add(i + 1, materials[i], "M" + (100 + i), "启用");
                }
            }

            // ==========================================
            // 2. A-管材标准 (原管材等级)
            // 字段：ID、标准、标准编码、主材料（选填）、标准系列名称（选填）、状态
            // ==========================================
            else if (nodeTitle == "A-管材标准")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("标准", typeof(string), isRequired: true);
                dt.AddColumn("标准编码", typeof(string), isRequired: true);
                dt.AddColumn("主材料", typeof(string), isRequired: false);     // 选填
                dt.AddColumn("标准系列名称", typeof(string), isRequired: false); // 选填
                dt.AddColumn("状态", typeof(string), isRequired: true);

                dt.Rows.Add(1, "API 5L", "STD_01", "CS", "ASME B36.10M", "启用");
                dt.Rows.Add(2, "ASTM A106", "STD_02", "CS", "ASME B36.10M", "启用");
                dt.Rows.Add(3, "ASTM A312", "STD_03", "SS", "ASME B36.19M", "启用");
            }

            // ==========================================
            // 3. B3-牌号
            // 字段：ID、牌号编码、管材标准、状态
            // ==========================================
            else if (nodeTitle == "B3-牌号")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("牌号编码", typeof(string), isRequired: true);
                dt.AddColumn("管材标准", typeof(string), isRequired: true); // 关联字段
                dt.AddColumn("状态", typeof(string), isRequired: true);

                dt.Rows.Add(1, "Gr.B", "ASTM A106", "启用");
                dt.Rows.Add(2, "TP304", "ASTM A312", "启用");
                dt.Rows.Add(3, "TP316L", "ASTM A312", "启用");
            }

            // ==========================================
            // 4. C1-法兰标准 (新增)
            // 字段：ID、法兰标准、法兰标准编码、标准系列名称（选填）、状态
            // ==========================================
            else if (nodeTitle == "C1-法兰标准")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("法兰标准", typeof(string), isRequired: true);
                dt.AddColumn("法兰标准编码", typeof(string), isRequired: true);
                dt.AddColumn("标准系列名称", typeof(string), isRequired: false); // 选填
                dt.AddColumn("状态", typeof(string), isRequired: true);

                dt.Rows.Add(1, "ASME B16.5", "FLG_STD_01", "ASME系列", "启用");
                dt.Rows.Add(2, "ASME B16.47 A", "FLG_STD_02", "ASME系列", "启用");
                dt.Rows.Add(3, "EN 1092-1", "FLG_STD_03", "DIN系列", "启用");
            }

            // ==========================================
            // 5. C2-法兰压力等级
            // 字段：ID、法兰压力等级、法兰压力等级编码、状态
            // ==========================================
            else if (nodeTitle == "C2-法兰压力等级")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("法兰压力等级", typeof(string), isRequired: true);
                dt.AddColumn("法兰压力等级编码", typeof(string), isRequired: true);
                dt.AddColumn("状态", typeof(string), isRequired: true);

                var ratings = new[] { "150", "300", "600", "900", "1500", "2500" };
                for (int i = 0; i < ratings.Length; i++)
                {
                    dt.Rows.Add(i + 1, "CL" + ratings[i], "R_" + ratings[i], "启用");
                }
            }

            // ==========================================
            // 6. D-壁厚等级
            // 字段：ID、壁厚等级标准、壁厚等级编码、主材料（选填）、状态
            // ==========================================
            else if (nodeTitle == "D-壁厚等级")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("壁厚等级标准", typeof(string), isRequired: true);
                dt.AddColumn("壁厚等级编码", typeof(string), isRequired: true);
                dt.AddColumn("主材料", typeof(string), isRequired: false); // 选填
                dt.AddColumn("状态", typeof(string), isRequired: true);

                dt.Rows.Add(1, "SCH40", "WT_01", "CS", "启用");
                dt.Rows.Add(2, "SCH80", "WT_02", "CS", "启用");
                dt.Rows.Add(3, "SCH10S", "WT_03", "SS", "启用");
                dt.Rows.Add(4, "STD", "WT_04", "", "启用");
            }

            // ==========================================
            // 7. 兜底：标准系列 (之前的功能保留)
            // ==========================================
            else if (nodeTitle == "标准系列")
            {
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("标准系列名称", typeof(string), isRequired: true);
                dt.AddColumn("标准系列描述", typeof(string), isRequired: false);
                dt.AddColumn("状态", typeof(string), isRequired: true);

                dt.Rows.Add(1, "ASME B36.10M", "碳钢管标准", "启用");
                dt.Rows.Add(2, "ASME B36.19M", "不锈钢管标准", "启用");
            }
            else
            {
                // 默认空表
                dt.AddColumn("提示", typeof(string));
                dt.Rows.Add("未定义的数据结构");
            }

            dt.AcceptChanges();
            return dt;
        }
    }
}