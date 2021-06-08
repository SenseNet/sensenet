using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{
    public class GeneralStatInput
    {
        public string DataType { get; set; }
        public object Data { get; set; }
    }
    public class WebTransferStatInput
    {
        public string Url { get; set; }
        public string HttpMethod { get; set; }
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
        public string EventName { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IStatisticalDataCollector
    {
        Task RegisterWebTransfer(WebTransferStatInput data, CancellationToken cancel);
        Task RegisterWebHook(WebHookStatInput data, CancellationToken cancel);
        Task RegisterGeneralData(GeneralStatInput data, CancellationToken cancel);
        Task RegisterGeneralData(string dataType, TimeResolution resolution, object data, CancellationToken cancel);
    }

    public class NullStatisticalDataCollector : IStatisticalDataCollector
    {
        public Task RegisterWebTransfer(WebTransferStatInput data, CancellationToken cancel) { return Task.CompletedTask; }
        public Task RegisterWebHook(WebHookStatInput data, CancellationToken cancel) { return Task.CompletedTask; }
        public Task RegisterGeneralData(GeneralStatInput data, CancellationToken cancel) { return Task.CompletedTask; }
        public Task RegisterGeneralData(string dataType, TimeResolution resolution, object data, CancellationToken cancel) { return Task.CompletedTask; }
    }
}