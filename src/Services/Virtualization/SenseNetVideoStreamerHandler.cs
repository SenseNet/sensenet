using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SenseNet.Portal.Virtualization
{
    internal class SenseNetVideoStreamerHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

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

            // The bytes we write into the output stream will NOT be buffered and will be sent
            // to the client immediately. This makes sure that the whole (potentially large)
            // file is not loaded into the memory on the server.
            context.Response.Buffer = false;

            // -------------------------------------------------------------------------------------------
            // Copy from here: http://stackoverflow.com/questions/16862782/streaming-large-video-files-net
            // -------------------------------------------------------------------------------------------
            Stream iStream = null;
            byte[] buffer = new byte[4096];

            try
            {
                iStream = vf.Open();

                // Total bytes to read:
                long dataToRead = iStream.Length;

                response.AddHeader("Content-Type", "video/mp4");
                response.AddHeader("Accept-Ranges", "bytes");
                response.AddHeader("Accept-Length", iStream.Length.ToString());

                if (!string.IsNullOrEmpty(request.Headers["Range"]))
                {
                    int startbyte = 0;

                    string[] range = request.Headers["Range"].Split('=', '-');
                    startbyte = int.Parse(range[1]);
                    iStream.Seek(startbyte, SeekOrigin.Begin);

                    response.StatusCode = 206;
                    response.AddHeader("Content-Range", String.Format(" bytes {0}-{1}/{2}", startbyte, dataToRead - 1, dataToRead));
                }

                while (dataToRead > 0)
                {
                    if (response.IsClientConnected)
                    {
                        iStream.Read(buffer, 0, buffer.Length);

                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        response.Flush();

                        buffer = new byte[buffer.Length];
                        dataToRead = dataToRead - buffer.Length;
                    }
                    else
                    {
                        // prevent infinite loop if user disconnects
                        dataToRead = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }
            finally
            {
                if (iStream != null)
                    iStream.Close();
                response.Close();
            }
        }
    }
}
