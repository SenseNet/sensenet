using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage;
using SenseNet.Storage.DataModel.Usage;

namespace SenseNet.Extensions.DependencyInjection
{
    public static class StatExtensions
    {
        public static IServiceCollection AddStatisticalDataCollector(this IServiceCollection services)
        {
            return services.AddStatisticalDataCollector<NullStatisticalDataCollector>();
        }
        public static IServiceCollection AddStatisticalDataCollector<T>(this IServiceCollection services) where T: class, IStatisticalDataCollector
        {
            return services.AddSingleton<IStatisticalDataCollector, T>();
        }
    }
}
namespace SenseNet.ContentRepository.Storage
{
    public class GeneralStatInput
    {
        public string DataType { get; set; }
        public object Data { get; set; }
    }
    public class WebTransferStatInput
    {
        public string Url { get; set; }
        public DateTime RequestTime { get; set; }
        public DateTime ResponseTime { get; set; }
        public long RequestLength { get; set; }
        public long ResponseLength { get; set; }
        public int ResponseStatusCode { get; set; }
    }
    public class WebHookStatInput : WebTransferStatInput
    {
        public int WebHookId { get; set; }
        public int ContentId { get; set; }
        public string ErrorMessage { get; set; }
    }
    public interface IStatisticalDataCollector
    {
        Task RegisterWebTransfer(WebTransferStatInput data);
        Task RegisterWebHook(WebHookStatInput data);
        Task RegisterDatabaseUsage(DatabaseUsage data);
        Task RegisterGeneralData(GeneralStatInput data);
    }

    public class NullStatisticalDataCollector : IStatisticalDataCollector
    {
        public Task RegisterWebTransfer(WebTransferStatInput data)
        {
            return Task.CompletedTask;
        }

        public Task RegisterWebHook(WebHookStatInput data)
        {
            return Task.CompletedTask;
        }

        public Task RegisterDatabaseUsage(DatabaseUsage data)
        {
            return Task.CompletedTask;
        }

        public Task RegisterGeneralData(GeneralStatInput data)
        {
            return Task.CompletedTask;
        }
    }
}
