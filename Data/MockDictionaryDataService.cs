using Bogus;
using PipeRuleConfigurator.Services;
using System;
using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.Data
{
    public class MockDictionaryDataService : IPipeDictionaryService
    {
        public async Task<DataTable> GetTableDataAsync(string nodeTitle)
        {
            await Task.Delay(200); 

            var dt = new DataTable();
            var faker = new Faker("zh_CN");

            // --- 现有: A-管材等级 ---
            if (nodeTitle == "A-管材等级")
            {
                // ... (保持原有代码不变) ...
                dt.Columns.Add("ID", typeof(int));
                dt.Columns.Add("管材等级", typeof(string));
                dt.Columns.Add("管材等级编码", typeof(string));
                dt.Columns.Add("描述", typeof(string));
                dt.Columns.Add("状态", typeof(string));
                dt.Columns.Add("更新人", typeof(string));
                dt.Columns.Add("更新时间", typeof(DateTime));

                for (int i = 1; i <= 20; i++)
                {
                    string pressure = faker.PickRandom(new[] { "CL150", "CL300", "CL600" });
                    string material = faker.PickRandom(new[] { "CS", "SS", "LTCS" });
                    dt.Rows.Add(i, $"{pressure}-{material}", faker.Random.AlphaNumeric(8).ToUpper(), faker.Commerce.ProductAdjective() + "工艺管线", faker.PickRandom(new[] { "启用", "禁用" }), faker.Name.FullName(), faker.Date.Past());
                }
            }
            // --- 现有: B1-主材料 ---
            else if (nodeTitle == "B1-主材料")
            {
                 // ... (保持原有代码不变) ...
                 dt.Columns.Add("材料代码", typeof(string));
                 dt.Columns.Add("材料名称", typeof(string));
                 dt.Columns.Add("密度 (g/cm³)", typeof(double));
                 dt.Columns.Add("状态", typeof(string));
                 dt.Columns.Add("供应商", typeof(string));
                 for (int i = 0; i < 15; i++)
                 {
                     dt.Rows.Add(faker.Random.Replace("??-###"), faker.Commerce.ProductName(), Math.Round(faker.Random.Double(2.5, 8.9), 2), faker.PickRandom(new[] { "启用", "禁用" }), faker.Company.CompanyName());
                 }
            }
            // --- 新增: 基础类 -> 弯管数据 ---
            else if (nodeTitle == "弯管数据")
            {
                dt.Columns.Add("ID", typeof(int));
                dt.Columns.Add("弯管角度", typeof(int));
                dt.Columns.Add("弯曲半径(R)", typeof(double));
                dt.Columns.Add("直管段长度", typeof(int));
                dt.Columns.Add("状态", typeof(string));

                for (int i = 1; i <= 10; i++)
                {
                    dt.Rows.Add(i, faker.PickRandom(new[] { 30, 45, 60, 90 }), faker.PickRandom(new[] { 1.5, 3.0, 5.0 }), 500, "启用");
                }
            }
            // --- 新增: 基础类 -> 壁厚系列 ---
            else if (nodeTitle == "壁厚系列")
            {
                dt.Columns.Add("尺寸(NPS)", typeof(string));
                dt.Columns.Add("Schedule", typeof(string));
                dt.Columns.Add("壁厚(mm)", typeof(double));
                dt.Columns.Add("状态", typeof(string));

                string[] schedules = { "SCH10", "SCH20", "SCH40", "STD", "XS" };
                for (int i = 1; i <= 20; i++)
                {
                    dt.Rows.Add($"{faker.Random.Number(1, 24)}\"", faker.PickRandom(schedules), Math.Round(faker.Random.Double(2.0, 20.0), 2), "启用");
                }
            }
            // --- 默认 ---
            else
            {
                dt.Columns.Add("提示信息", typeof(string));
                dt.Rows.Add($"[{nodeTitle}] 数据加载成功 (Mock)");
            }

            dt.AcceptChanges(); // 确保初始状态为 Unchanged
            return dt;
        }
    }
}