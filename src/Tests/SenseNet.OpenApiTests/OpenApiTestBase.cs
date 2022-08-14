using System.IO;
using Newtonsoft.Json;
using SenseNet.Tests.Core;

namespace SenseNet.OpenApiTests
{
    public class OpenApiTestBase : TestBase
    {
        private JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        protected string Serialize(object target)
        {
            using var writer = new StringWriter();
            var serializer = JsonSerializer.CreateDefault(settings);
            serializer.Serialize(writer, target);
            return writer.GetStringBuilder().ToString();
        }
    }
}
