using System;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Preview;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Portal.Handlers
{
    internal class ExportToPdfAction : UrlAction, IHttpHandler
    {
        public override bool IsHtmlOperation { get { return true; } }
        public override bool IsODataOperation { get { return false; } }
        public override bool CausesStateChange { get { return false; } }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            // If the user has Open permission for a pdf file, it should be downloaded using the Browse action instead.
            // This action should be available only if the user does not have Open permission.
            if (context.Name.ToLower().EndsWith(".pdf") && context.Security.HasPermission(PermissionType.Open))
                this.Forbidden = true;

            if (!this.Forbidden && !DocumentPreviewProvider.Current.HasPreviewImages(context.ContentHandler))
                this.Forbidden = true;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Clear();

            var file = this.Content.ContentHandler as File;
            var dpp = DocumentPreviewProvider.Current;
            var head = file == null ? null : NodeHead.Get(file.Id);

            if (file == null || dpp == null || !dpp.HasPreviewPermission(head))
            {
                context.Response.End();
                return;
            }

            context.Response.ContentType = "application/pdf";

            // append pdf extension only if the file is not already a pdf
            var name = this.Content.Name.ToLower().EndsWith(".pdf")
                ? this.Content.Name
                : this.Content.Name + ".pdf";

            Virtualization.HttpHeaderTools.SetContentDispositionHeader(name);

            // We store the restriction type for the current user to use it later inside the elevated block.
            var rt = dpp.GetRestrictionType(head);

            // We need to elevate here because otherwise preview images would 
            // not be accessible for a user that has only Preview permissions.)
            using (new SystemAccount())
            {
                using (var pdfStream = dpp.GetPreviewImagesDocumentStream(this.Content, DocumentFormat.Pdf, rt))
                {
                    if (pdfStream != null)
                    {
                        context.Response.AppendHeader("Content-Length", pdfStream.Length.ToString());

                        // We need to Flush the headers before we start to stream the actual binary.
                        context.Response.Flush();

                        // Let ASP.NET handle sending bytes to the client.
                        pdfStream.CopyTo(context.Response.OutputStream);
                    }
                } 
            }

            context.Response.End();
        }
    }
}
