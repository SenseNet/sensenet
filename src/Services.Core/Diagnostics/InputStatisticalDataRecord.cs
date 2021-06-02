using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class InputStatisticalDataRecord : IStatisticalDataRecord
    {
        private readonly WebTransferStatInput _webTransferData;
        private readonly WebHookStatInput _webHookData;
        private readonly GeneralStatInput _generalData;
        private string _serializedGeneralData;

        public InputStatisticalDataRecord(WebTransferStatInput data)
        {
            _webTransferData = data;
        }
        public InputStatisticalDataRecord(WebHookStatInput data)
        {
            _webHookData = data;
        }
        public InputStatisticalDataRecord(GeneralStatInput data)
        {
            _generalData = data;
        }

        public int Id => 0;
        public string DataType => _generalData?.DataType ?? (_webHookData == null ? "WebTransfer" : "WebHook");
        public DateTime WrittenTime => DateTime.MinValue;
        public DateTime? RequestTime => _webHookData?.RequestTime ?? _webTransferData?.RequestTime;
        public DateTime? ResponseTime => _webHookData?.ResponseTime ?? _webTransferData?.ResponseTime;
        public long? RequestLength => _webHookData?.RequestLength ?? _webTransferData?.RequestLength;
        public long? ResponseLength => _webHookData?.ResponseLength ?? _webTransferData?.ResponseLength;
        public int? ResponseStatusCode => _webHookData?.ResponseStatusCode ?? _webTransferData?.ResponseStatusCode;
        public string Url => _webHookData?.Url ?? _webTransferData?.Url;
        public int? WebHookId => _webHookData?.WebHookId;
        public int? ContentId => _webHookData?.ContentId;
        public string EventName => _webHookData?.EventName;
        public string ErrorMessage => _webHookData?.ErrorMessage;

        public string GeneralData => _serializedGeneralData ??= SerializeGeneralData(_generalData?.Data);

        private string SerializeGeneralData(object data)
        {
            if (data == null)
                return string.Empty;
            var serializer = JsonSerializer.Create(new JsonSerializerSettings {Formatting = Formatting.Indented});
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                serializer.Serialize(writer, data);
            return sb.ToString();
        }
    }
}
