using System;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Portal.Virtualization
{
    public class SenseNetStaticFileHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            VirtualFile vf = null;
            var filePath = request.FilePath;

            if (HostingEnvironment.VirtualPathProvider.FileExists(filePath))
                vf = HostingEnvironment.VirtualPathProvider.GetFile(filePath);

            if (vf == null)
                throw new HttpException(404, "File does not exist");
            
            response.ClearContent();

            // Set content type only if this is not a RepositoryFile, because the
            // Open method of RepositoryFile will set the content type itself.
            if (!(vf is RepositoryFile))
            {
                var extension = System.IO.Path.GetExtension(filePath);
                context.Response.ContentType = MimeTable.GetMimeType(extension);

                // add the necessary header for the css font-face rule
                if (MimeTable.IsFontType(extension))
                    HttpHeaderTools.SetAccessControlHeaders();
            }

            // The bytes we write into the output stream will NOT be buffered and will be sent 
            // to the client immediately. This makes sure that the whole (potentially large) 
            // file is not loaded into the memory on the server.
            response.BufferOutput = false;

            using (var stream = vf.Open())
            {
                response.AppendHeader("Content-Length", stream.Length.ToString());
                response.Clear();

                // Let ASP.NET handle sending bytes to the client (avoid Flush).
                stream.CopyTo(response.OutputStream);
            }
        }

        #endregion
    }
}
