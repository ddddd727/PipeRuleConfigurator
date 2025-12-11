using Bogus;
using PipeRuleConfigurator.Services;
using PipeRuleConfigurator.Common; // 引用扩展方法
using System;
using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.Data
{
    public class MockBasicDataService : IPipeDictionaryService
    {
        public async Task<DataTable> GetTableDataAsync(string nodeTitle)
        {
            await Task.Delay(200);

            var dt = new DataTable();
            var faker = new Faker("zh_CN");

            // --- 场景 1: 弯管数据 ---
            if (nodeTitle == "弯管数据")
            {
                // 使用扩展方法：最后参数 true 代表必填
                dt.AddColumn("ID", typeof(int));
                dt.AddColumn("弯管角度(°)", typeof(int), isRequired: true); // 必填
                dt.AddColumn("弯曲半径(R)", typeof(string), isRequired: true); // 必填
                dt.AddColumn("直管段长度(mm)", typeof(int), isRequired: false); // 选填
                dt.AddColumn("制造标准", typeof(string), isRequired: true); // 必填
                dt.AddColumn("状态", typeof(string), isRequired: true); // 必填
                dt.AddColumn("更新时间", typeof(DateTime));

                // 生成数据... (保持原有逻辑)
                for (int i = 1; i <= 15; i++)
                {
                    dt.Rows.Add(
                        i,
                        faker.PickRandom(new[] { 30, 45, 60, 90, 180 }),
                        faker.PickRandom(new[] { "1.0D", "1.5D", "3.0D", "5.0D" }),
                        faker.PickRandom(new[] { 300, 500, 600, 1000 }),
                        "ASME B16.9",
                        "启用",
                        faker.Date.Recent()
                    );
                }
            }
            // --- 场景 2: 壁厚系列 ---
            else if (nodeTitle == "壁厚系列")
            {
                dt.AddColumn("公称直径(NPS)", typeof(string), isRequired: true);
                dt.AddColumn("Schedule", typeof(string), isRequired: true);
                dt.AddColumn("壁厚(mm)", typeof(double), isRequired: true);
                dt.AddColumn("状态", typeof(string), isRequired: true);
                dt.AddColumn("备注", typeof(string), isRequired: false); // 选填

                // 生成数据... (保持原有逻辑)
                // ...
            }
            else
            {
                dt.AddColumn("提示信息", typeof(string));
                dt.Rows.Add($"[{nodeTitle}] 暂无数据");
            }

            dt.AcceptChanges();
            return dt;
        }
    }
}