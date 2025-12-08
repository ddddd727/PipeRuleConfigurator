using Bogus;
using PipeRuleConfigurator.Services;
using System;
using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.Data
{
    public class MockDictionaryService : IPipeDictionaryService
    {
        public async Task<DataTable> GetTableDataAsync(string nodeTitle)
        {
            await Task.Delay(200); // 模拟延迟

            var dt = new DataTable();
            var faker = new Faker("zh_CN");

            // --- 场景 1: A-管材等级 ---
            if (nodeTitle == "A-管材等级")
            {
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

                    dt.Rows.Add(
                        i,
                        $"{pressure}-{material}",
                        faker.Random.AlphaNumeric(8).ToUpper(),
                        faker.Commerce.ProductAdjective() + "工艺管线",
                        faker.PickRandom(new[] { "启用", "禁用" }),
                        faker.Name.FullName(),
                        faker.Date.Past()
                    );
                }
            }
            // --- 场景 2: B1-主材料 ---
            else if (nodeTitle == "B1-主材料")
            {
                dt.Columns.Add("材料代码", typeof(string));
                dt.Columns.Add("材料名称", typeof(string));
                dt.Columns.Add("密度 (g/cm³)", typeof(double));
                dt.Columns.Add("状态", typeof(string));
                dt.Columns.Add("供应商", typeof(string));

                for (int i = 0; i < 15; i++)
                {
                    dt.Rows.Add(
                        faker.Random.Replace("??-###"),
                        faker.Commerce.ProductName(),
                        Math.Round(faker.Random.Double(2.5, 8.9), 2),
                        faker.PickRandom(new[] { "启用", "禁用" }),
                        faker.Company.CompanyName()
                    );
                }
            }
            else
            {
                dt.Columns.Add("提示信息", typeof(string));
                dt.Rows.Add($"[{nodeTitle}] 数据加载成功 (Mock)");
            }

            // 【关键修改】提交更改，将所有行的 RowState 重置为 Unchanged。
            // 这样，只有后续新增的行是 Added，修改的行是 Modified。
            dt.AcceptChanges();

            return dt;
        }
    }
}