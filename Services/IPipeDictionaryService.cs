using System.Data;
using System.Threading.Tasks;

namespace PipeRuleConfigurator.Services
{
    // 定义接口：规定了“查表”的能力
    public interface IPipeDictionaryService
    {
        Task<DataTable> GetTableDataAsync(string nodeTitle);
    }
}