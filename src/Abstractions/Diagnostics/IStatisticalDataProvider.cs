using System;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{
    public interface IStatisticalDataRecord
    {
        int Id { get; }                    // by provider
        string DataType { get; }           // General
        DateTime WrittenTime { get; }      // by provider
        DateTime? RequestTime { get; }     // from WebTransfer
        DateTime? ResponseTime { get; }    // from WebTransfer
        long? RequestLength { get; }       // from WebTransfer
        long? ResponseLength { get; }      // from WebTransfer
        int? ResponseStatusCode { get; }   // from WebTransfer
        string Url { get; }                // from WebTransfer
        int? WebHookId { get; }            // from WebHook
        int? ContentId { get; }            // from WebHook
        string EventName { get; }          // from WebHook
        string ErrorMessage { get; }       // from WebHook

        string GeneralData { get; }        // from General
    }

    public interface IStatisticalDataProvider
    {
        Task WriteData(IStatisticalDataRecord data);
    }

    public class StatisticalDataRecord : IStatisticalDataRecord
    {
        public int Id { get; set; }
        public string DataType { get; set; }
        public DateTime WrittenTime { get; set; }
        public DateTime? RequestTime { get; set; }
        public DateTime? ResponseTime { get; set; }
        public long? RequestLength { get; set; }
        public long? ResponseLength { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string Url { get; set; }
        public int? WebHookId { get; set; }
        public int? ContentId { get; set; }
        public string EventName { get; set; }
        public string ErrorMessage { get; set; }
        public string GeneralData { get; set; }
    }
}
