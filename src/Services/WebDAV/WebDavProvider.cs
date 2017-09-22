using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Tools;

namespace SenseNet.Portal.WebDAV
{
    public abstract class WebDavProvider
    {
        // ========================================================================================== Static interface

        private static WebDavProvider _current;
        private static readonly object _lock = new object();
        public static WebDavProvider Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_lock)
                    {
                        if (_current == null)
                        {
                            try
                            {
                                var wpType = TypeResolver.GetTypesByBaseType(typeof (WebDavProvider)).FirstOrDefault(t => t.FullName != typeof (DefaultWebDavProvider).FullName) ??
                                    typeof (DefaultWebDavProvider);

                                _current = (WebDavProvider)TypeResolver.CreateInstance(wpType.FullName);
                            }
                            catch (Exception ex)
                            {
                                SnLog.WriteException(ex);
                            }

                            if (_current != null)
                                SnLog.WriteInformation("WebDavProvider created: " + _current.GetType().FullName);
                            else
                                SnLog.WriteWarning("WebDavProvider is not present.", EventId.Services);
                        }
                    }
                }

                return _current;
            }
        }

        // ========================================================================================== WebDAV methods

        public abstract IEnumerable<Node> GetChildren(IFolder folder);
        public abstract void AssertCreateContent(string parentPath, string contentName, string contentTypeName);
        public abstract void AssertModifyContent(Node node);
        public abstract void AssertMoveContent(Node node, string destinationParentPath);

        // ========================================================================================== Office protocol (DWS) methods

        /// <summary>
        /// Returns documents and folders in a document library. Used by the Office protocol.
        /// </summary>
        /// <param name="documentLibraryPath">Path of a document library.</param>
        public abstract IEnumerable<Node> GetDocumentsAndFolders(string documentLibraryPath);
        public abstract IEnumerable<IUser> GetWorkspaceMembers(Workspace workspace);
    }

    public class DefaultWebDavProvider : WebDavProvider
    {
        public override IEnumerable<Node> GetChildren(IFolder folder)
        {
            return folder.Children;
        }
        
        public override void AssertCreateContent(string parentPath, string contentName, string contentTypeName)
        {
            // do nothing
        }

        public override void AssertModifyContent(Node node)
        {
            // do nothing
        }

        public override void AssertMoveContent(Node node, string destinationParentPath)
        {
            // do nothing
        }

        public override IEnumerable<Node> GetDocumentsAndFolders(string documentLibraryPath)
        {
            return ContentQuery.Query("+TypeIs:(File Folder) +InTree:@0 -Path:@0", null, documentLibraryPath).Nodes;
        }

        public override IEnumerable<IUser> GetWorkspaceMembers(Workspace workspace)
        {
            return workspace != null ? workspace.GetWorkspaceMembers() : new List<IUser>();
        }
    } 
}
