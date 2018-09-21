using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal;
using SenseNet.Portal.OData;
using SenseNet.Portal.Virtualization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.Services.OData.Tests.Results;
using SenseNet.Tests;

namespace SenseNet.Services.OData.Tests
{
    public abstract class ODataTestClass : TestBase
    {
        internal static T ODataGET<T>(string resource, string queryString) where T : IODataResult
        {
            using (var output = new System.IO.StringWriter())
            {
                var pc = CreatePortalContext(resource, queryString, output);
                var handler = new ODataHandler();
                handler.ProcessRequest(pc.OwnerHttpContext);
                output.Flush();
                return (T)GetResult<T>(output);
            }
        }
        internal static IODataResult GetResult<T>(System.IO.StringWriter output) where T : IODataResult
        {
            if (typeof(T) == typeof(ODataEntity))
            {
                CheckError(output);
                return GetEntity(output);
            }
            if (typeof(T) == typeof(ODataEntities))
            {
                CheckError(output);
                return GetEntities(output);
            }
            if (typeof(T) == typeof(ODataError))
            {
                return GetError(output);
            }
            if (typeof(T) == typeof(ODataRaw))
            {
                return GetODataRawResult(output);
            }
            throw new NotImplementedException();
        }


        protected const string TestSiteName = "ODataTestSite";
        protected static string TestSitePath => RepositoryPath.Combine("/Root/Sites", TestSiteName);
        protected static Site CreateTestSite()
        {
            var sites = new Folder(Repository.Root, "Sites") { Name = "Sites" };
            sites.Save();

            var site = new Site(sites) { Name = TestSiteName, UrlList = new Dictionary<string, string> { { "localhost", "None" } } };
            site.Save();

            return site;
        }

        protected static Stream CreateRequestStream(string request)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(request);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        protected static PortalContext CreatePortalContext(string pagePath, string queryString, System.IO.TextWriter output)
        {
            var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, queryString, output, "localhost");
            var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
            HttpContext.Current = simulatedHttpContext;
            var portalContext = PortalContext.Create(simulatedHttpContext);
            return portalContext;
        }
        protected static ODataExceptionCode GetExceptionCode(StringWriter output)
        {
            ODataExceptionCode oecode;
            Enum.TryParse<ODataExceptionCode>(GetExceptionCodeText(output), out oecode);
            return oecode;
        }
        protected static string GetExceptionCodeText(StringWriter output)
        {
            var json = Deserialize(output);
            var error = json["error"] as JObject;
            return error["code"].Value<string>();
        }
        protected static void CheckError(StringWriter output)
        {
            var text = GetStringResult(output);
            if (text.IndexOf("error") < 0)
                return;
            ODataError e = null;
            try
            {
                e = GetError(output);
            }
            catch { } // does nothing
            if (e != null)
                throw new ApplicationException(String.Format("Code: {0}, ExceptionType: {1}, Message: {2}, Stack: {3}",
                    e.Code, e.ExceptionType, e.Message, e.StackTrace));
        }
        protected static ODataError GetError(StringWriter output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var json = Deserialize(output);
            if (json == null)
                throw new InvalidOperationException("Deserialized output is null.");
            if (!(json["error"] is JObject error))
                throw new Exception("Object is not an error");

            var code = error["code"].Value<string>();
            var exceptiontype = error["exceptiontype"].Value<string>();
            var message = error["message"] as JObject;
            var value = message["value"].Value<string>();
            var innererror = error["innererror"] as JObject;
            var trace = innererror["trace"].Value<string>();
            Enum.TryParse<ODataExceptionCode>(code, out var oecode);
            return new ODataError { Code = oecode, ExceptionType = exceptiontype, Message = value, StackTrace = trace };
        }
        protected static ODataEntity GetEntity(StringWriter output)
        {
            var result = new Dictionary<string, object>();
            var jo = (JObject)Deserialize(output);
            return ODataEntity.Create((JObject)jo["d"]);
        }
        protected static ODataEntities GetEntities(StringWriter output)
        {
            var result = new List<ODataEntity>();
            var jo = (JObject)Deserialize(output);
            var d = (JObject)jo["d"];
            var count = d["__count"].Value<int>();
            var jarray = (JArray)d["results"];
            for (int i = 0; i < jarray.Count; i++)
                result.Add(ODataEntity.Create((JObject)jarray[i]));
            return new ODataEntities(result.ToList(), count);
        }

        protected static JContainer Deserialize(StringWriter output)
        {
            var text = GetStringResult(output);
            JContainer json;
            using (var reader = new StringReader(text))
                json = Deserialize(reader);
            return json;
        }
        protected static JContainer Deserialize(TextReader reader)
        {
            var models = reader?.ReadToEnd() ?? string.Empty;
            var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
            var serializer = JsonSerializer.Create(settings);
            var jreader = new JsonTextReader(new StringReader(models));
            var x = (JContainer)serializer.Deserialize(jreader);
            return x;

        }

        protected static ODataRaw GetODataRawResult(StringWriter output)
        {
            return new ODataRaw(output.GetStringBuilder().ToString());
        }
        protected static string GetStringResult(StringWriter output)
        {
            return output?.GetStringBuilder().ToString() ?? string.Empty;
        }
        protected object GetUrl(string path)
        {
            return string.Format("http://localhost/OData.svc/{0}('{1}')", RepositoryPath.GetParentPath(path), RepositoryPath.GetFileName(path));
        }

        protected static void EnsureCleanAdministratorsGroup()
        {
            var group = Group.Administrators;
            group.Members = new[] { User.Administrator };
            group.Save();
        }

        protected static void EnsureManagerOfAdmin()
        {
            var content = Content.Create(User.Administrator);
            if (((IEnumerable<Node>)content["Manager"]).Count() > 0)
                return;
            content["Manager"] = User.Administrator;
            content["Email"] = "anybody@somewhere.com";
            content.Save();
        }

        protected static SystemFolder CreateTestRoot(string name = null)
        {
            return CreateTestRoot(null, name);
        }
        protected static SystemFolder CreateTestRoot(Node parent, string name)
        {
            var systemFolder = new SystemFolder(parent ?? Repository.Root) {Name = name ?? Guid.NewGuid().ToString()};
            systemFolder.Save();
            return systemFolder;
        }
    }
}
