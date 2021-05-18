using System;
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
        Task RegisterGeneralData(GeneralStatInput data);
    }
}