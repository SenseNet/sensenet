using System;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Indexing
{
    public class IndexFilePathHandler : IHttpHandler
    {
        // =========================================================================== IHttpHandler members

        public void ProcessRequest(HttpContext context)
        {
            if (HttpContext.Current.Request.Url.Host != "127.0.0.1")
                throw new HttpException(403, "Accessing this service from a remote machine is forbidden");

            context.Response.Clear();
            context.Response.ContentType = "text/html";

            var mode = HttpContext.Current.Request.Params["continue"] ?? string.Empty;

            switch (mode.ToLowerInvariant())
            {
                case "1":
                case "true":
                    ContinueIndexingAndWriteResult(context);
                    break;
                default:
                    WriteFilePaths(context);
                    break;
            }

            context.Response.Flush();
            context.Response.End();
        }

        public bool IsReusable
        {
            get { return false; }
        }

        // =========================================================================== Helper methods

        private static void WriteFilePaths(HttpContext context)
        {
            context.Response.Write(string.Join(";", LuceneManager.PauseIndexingAndGetIndexFilePaths()));
        }

        private static void ContinueIndexingAndWriteResult(HttpContext context)
        {
            try
            {
                LuceneManager.ContinueIndexing();
                context.Response.Write("OK");
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                context.Response.Write(ex.Message);
            }
        }
    }
}
