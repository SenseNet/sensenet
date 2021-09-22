using Newtonsoft.Json.Linq;

namespace SenseNet.OData.IO
{
    internal class ImportedContent
    {
        private readonly JObject _data;

        public string Name => Get<string>("ContentName");
        public string Type => Get<string>("ContentType");
        public JObject Fields => Get<JObject>("Fields");
        public PermissionModel Permissions => GetPermissionModel();

        public ImportedContent(JObject data)
        {
            _data = data;
        }

        private T Get<T>(string name)
        {
            return _data.Value<T>(name);
        }

        private PermissionModel GetPermissionModel()
        {
            var raw = Get<JObject>("Permissions");
            if (raw == null)
                return null;
            var y = raw.ToObject<PermissionModel>();
            return y;
        }
    }
}
