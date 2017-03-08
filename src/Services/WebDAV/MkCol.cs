using System;
using System.Collections.Generic;
using System.Security;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.WebDAV;

namespace SenseNet.Services.WebDav
{
    public class MkCol : IHttpMethod
    {
        private WebDavHandler _handler;

        public MkCol(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            var parentPath = RepositoryPath.GetParentPath(_handler.GlobalPath);
            var folderName = RepositoryPath.GetFileName(_handler.GlobalPath);

            WebDavProvider.Current.AssertCreateContent(parentPath, folderName, "Folder");

            try
            {
                var f = new Folder(Node.LoadNode(parentPath)) { Name = folderName };
                f.Save();

                _handler.Context.Response.StatusCode = 201;
            }
            catch (SecurityException e) // logged
            {
                SnLog.WriteException(e);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (SenseNetSecurityException ee) // logged
            {
                SnLog.WriteException(ee);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (Exception eee) // logged
            {
                SnLog.WriteException(eee, "Could not save folder.",
                    EventId.Services,
                    properties: new Dictionary<string, object> { { "Parent path", parentPath }, { "Folder name", folderName } });
                _handler.Context.Response.StatusCode = 405;
            }
        }

        #endregion
    }
}
