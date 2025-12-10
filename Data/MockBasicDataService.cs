using Bogus;
using PipeRuleConfigurator.Services;
using System;
using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.Data
{
    // 基础类模块专属的 Mock 数据服务
    public class MockBasicDataService : IPipeDictionaryService
    {
        public async Task<DataTable> GetTableDataAsync(string nodeTitle)
        {
            await Task.Delay(200); // 模拟数据库查询延迟

            var dt = new DataTable();
            var faker = new Faker("zh_CN");

            // --- 场景 1: 弯管数据 ---
            if (nodeTitle == "弯管数据")
            {
                dt.Columns.Add("ID", typeof(int));
                dt.Columns.Add("弯管角度(°)", typeof(int));
                dt.Columns.Add("弯曲半径(R)", typeof(string)); // 例如 1.5D, 3D
                dt.Columns.Add("直管段长度(mm)", typeof(int));
                dt.Columns.Add("制造标准", typeof(string));
                dt.Columns.Add("状态", typeof(string));
                dt.Columns.Add("更新时间", typeof(DateTime));

                for (int i = 1; i <= 15; i++)
                {
                    dt.Rows.Add(
                        i,
                        faker.PickRandom(new[] { 30, 45, 60, 90, 180 }),
                        faker.PickRandom(new[] { "1.0D", "1.5D", "3.0D", "5.0D" }),
                        faker.PickRandom(new[] { 300, 500, 600, 1000 }),
                        "ASME B16.9",
                        faker.PickRandom(new[] { "启用", "禁用" }),
                        faker.Date.Recent()
                    );
                }
            }
            // --- 场景 2: 壁厚系列 ---
            else if (nodeTitle == "壁厚系列")
            {
                dt.Columns.Add("公称直径(NPS)", typeof(string));
                dt.Columns.Add("Schedule", typeof(string)); // SCH号
                dt.Columns.Add("壁厚(mm)", typeof(double));
                dt.Columns.Add("状态", typeof(string));
                dt.Columns.Add("备注", typeof(string));

                string[] schedules = { "SCH10", "SCH20", "SCH40", "STD", "XS", "SCH80", "XXS" };

                for (int i = 1; i <= 30; i++)
                {
                    dt.Rows.Add(
                        $"{faker.Random.Number(1, 24)}\"",
                        faker.PickRandom(schedules),
                        Math.Round(faker.Random.Double(2.0, 30.0), 2),
                        faker.PickRandom(new[] { "启用", "禁用" }),
                        faker.Lorem.Sentence(2)
                    );
                }
            }
            // --- 默认情况 ---
            else
            {
                dt.Columns.Add("提示信息", typeof(string));
                dt.Rows.Add($"[{nodeTitle}] 暂无数据 (MockBasicDataService)");
            }

            // 【重要】提交更改，将所有行的状态重置为 Unchanged
            // 这样进入界面时，行背景才是白色的；新增/修改后才会变色
            dt.AcceptChanges();

            return dt;
        }
    }
}