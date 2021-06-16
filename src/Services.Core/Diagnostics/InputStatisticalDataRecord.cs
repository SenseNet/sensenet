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
        public DateTime? CreationTime => _webHookData?.RequestTime ?? _webTransferData?.RequestTime;
        public TimeSpan? Duration => GetDuration();
        public long? RequestLength => _webHookData?.RequestLength ?? _webTransferData?.RequestLength;
        public long? ResponseLength => _webHookData?.ResponseLength ?? _webTransferData?.ResponseLength;
        public int? ResponseStatusCode => _webHookData?.ResponseStatusCode ?? _webTransferData?.ResponseStatusCode;
        public string Url
        {
            get
            {
                var method = _webHookData?.HttpMethod ?? _webTransferData?.HttpMethod;
                var url = _webHookData?.Url ?? _webTransferData?.Url;
                return string.IsNullOrEmpty(method) ? url : $"{method} {url}";
            }
        }

        public int? TargetId => _webHookData?.WebHookId;
        public int? ContentId => _webHookData?.ContentId;
        public string EventName => _webHookData?.EventName;
        public string ErrorMessage => _webHookData?.ErrorMessage;

        public string GeneralData => _serializedGeneralData ??= SerializeGeneralData(_generalData?.Data);

        private TimeSpan? GetDuration()
        {
            var requestTime = _webHookData?.RequestTime ?? _webTransferData?.RequestTime;
            if (requestTime == null)
                return null;
            return (_webHookData?.ResponseTime ?? _webTransferData?.ResponseTime) - requestTime;
        }

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
