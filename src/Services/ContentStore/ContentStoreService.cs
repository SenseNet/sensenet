//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;

//using SenseNet.ContentRepository.Storage;
//using System.ServiceModel.Activation;
//using SenseNet.ContentRepository.Storage.Search;
//using SenseNet.ContentRepository.Schema;
//using SenseNet.ContentRepository;
//using SenseNet.Diagnostics;
//using SenseNet.Search;

//namespace SenseNet.Services.ContentStore
//{
//    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
//    [System.ServiceModel.ServiceBehavior(IncludeExceptionDetailInFaults = true)]
//    public class ContentStoreService : IContentStoreService
//    {

//        // Queries //////////////////////////////////////////////////////////////
//        public Content GetItem(string itemPath)
//        {
//            return GetItem2(itemPath, false, 0, 0);
//        }

//        public Content GetItem2(string itemPath, bool onlyFileChildren, int start, int limit)
//        {
//            return GetItem3(itemPath, false, onlyFileChildren, start, limit);
//        }

//        public Content GetItem3(string itemPath, bool withProperties, bool onlyFileChildren, int start, int limit)
//        {
//            try
//            {
//                Node node = GetNodeById(itemPath);
//                return new Content(node, withProperties, true, onlyFileChildren, false, start, limit);
//            }

//            catch (Exception ex) // rethrow
//            {
//                throw new NodeLoadException(ex.Message, ex);
//            }
//        }

//        public Content[] GetFeed(string feedPath)
//        {
//            return GetFeed2(feedPath, false, false, 0, 0);
//        }

//        public Node GetNodeById(string itemId)
//        {
//            Node result = null;
//            var id = 0;
//            result = int.TryParse(itemId, out id) ? Node.LoadNode(id) : Node.LoadNode(PreparePath(itemId));
//            return result;
//        }

//        public Content[] GetContentTypes()
//        {
//            var enabledTypes = new List<ContentType>();
//            foreach (var contentType in ContentType.GetContentTypes())
//            {
//                if (!contentType.Security.HasPermission(SenseNet.ContentRepository.Storage.Security.PermissionType.See))
//                    continue;
//                if (contentType.Name == "PortalRoot" || contentType.Name == "GenericContent" || contentType.Name == "MasterPage")
//                    continue;
//                enabledTypes.Add(contentType);
//            }
//            return enabledTypes.Select(node => new Content(node, false, false, false, true, 0, 0)).ToArray();
//        }

//        public Content[] GetFeed2(string feedPath, bool onlyFiles, bool onlyFolders, int start, int limit)
//        {
//            var exceptions = new List<Exception>();
//            while (exceptions.Count < 3)
//            {
//                try
//                {
//                    var contents = GetFeed2Private(feedPath, onlyFiles, onlyFolders, start, limit);
//                    return contents;
//                }
//                catch (Exception e)
//                {
//                    exceptions.Add(e);
//                }
//            }
//            throw exceptions.Last();
//        }

//        private Content[] GetFeed2Private(string feedPath, bool onlyFiles, bool onlyFolders, int start, int limit)
//        {
//            Node container = GetNodeById(feedPath);

//            if (container == null) throw new NodeLoadException("Error loading path");
//            IFolder folder = container as IFolder;
//            if (folder == null) return new Content[] { };

//            IEnumerable<Node> nodeList;
//            NodeQuery query;

//            if (onlyFiles || onlyFolders)
//            {
//                nodeList = onlyFiles ?
//                    from child in folder.Children where child is IFile select child :
//                    from child in folder.Children where child is IFolder select child;
//            }
//            else
//            {
//                if (start == 0 && limit == 0)
//                {
//                    nodeList = folder.Children;
//                }
//                else
//                {
//                    SmartFolder smartFolder = folder as SmartFolder;
//                    if (folder is SmartFolder)
//                    {
//                        query = new NodeQuery();
//                        string queryString = ((SmartFolder)folder).Query;

//                        ExpressionList orExp = new ExpressionList(ChainOperator.Or);

//                        if (!string.IsNullOrEmpty(queryString))
//                            orExp.Add(NodeQuery.Parse(queryString));

//                        orExp.Add(new IntExpression(IntAttribute.ParentId, ValueOperator.Equal, container.Id));
//                        query.Add(orExp);
//                    }
//                    else
//                    {
//                        query = new NodeQuery();
//                        query.Add(new IntExpression(IntAttribute.ParentId, ValueOperator.Equal, container.Id));
//                    }
//                    query.PageSize = limit;
//                    query.Skip = start == 0 ? 0 : start - 1;
//                    nodeList = query.Execute().Nodes;
//                }
//            }

//            return nodeList.Select(node => new Content(node, false, false, false, false, start, limit)).ToArray();
//        }

//        public Content Search(string searchExpression)
//        {
//            var searchString = System.Web.HttpUtility.UrlDecode(searchExpression);

//            var query = ContentQuery.CreateQuery(searchString, new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled });
//            query.AddClause(string.Format("Name:{0}*", searchString.Replace("\"", "").Replace("*", "")), ChainOperator.Or);

//            var result = query.Execute().Nodes;
//            var feed = result.Select(node => new Content(node, false, false, false, false, 0, 0)).ToArray();

//            return CreateFakeRootForFeed(feed);
//        }

//        public Content Query(string queryXml, bool withProperties)
//        {
//            var resultset = ContentRepository.Content.Query(NodeQuery.Parse(queryXml));
//            var feed = resultset.Select<ContentRepository.Content, Content>(res => new Content(res.ContentHandler, withProperties, false, false, false, 0, 0)).ToArray();

//            return CreateFakeRootForFeed(feed);
//        }

//        public object UniversalDispatcher()
//        {
//            return null;
//        }

//        // Delete ///////////////////////////////////////////////////////////////
//        public void Delete(string nodeIdOrPath)
//        {
//            Delete(GetNodeId(nodeIdOrPath));
//        }

//        public void Delete(int nodeID)
//        {
//            Node.ForceDelete(nodeID);
//        }

//        public void DeleteMore(string nodeIdList)
//        {
//            if (String.IsNullOrEmpty(nodeIdList))
//                return;

//            var errors = new List<Exception>();

//            var nodeList = GetNodeIdList(nodeIdList);

//            Node.Delete(nodeList, ref errors);
//            if (errors.Count != 0)
//                SendErrorsToUser(errors);
//        }

//        // Move /////////////////////////////////////////////////////////////////
//        public void Move(string sourceIdOrPath, string targetIdOrPath)
//        {
//            Move(GetNodeId(sourceIdOrPath), GetNodeId(targetIdOrPath));
//        }

//        public void Move(int nodeID, int targetNodeID)
//        {
//            Node node = Node.LoadNode(nodeID);
//            Node target = Node.LoadNode(targetNodeID);
//            Node.Move(node.Path, target.Path);
//        }

//        public void MoveMore(string nodeIdList, string targetNodePath)
//        {
//            if (String.IsNullOrEmpty(nodeIdList))
//                return;

//            if (String.IsNullOrEmpty(targetNodePath))
//                return;

//            var errors = new List<Exception>();

//            var nodeList = GetNodeIdList(nodeIdList);

//            Node.MoveMore(nodeList, targetNodePath, ref errors);

//            if (errors.Count != 0)
//                SendErrorsToUser(errors);
//        }

//        // Copy /////////////////////////////////////////////////////////////////
//        public void Copy(string sourceIdOrPath, string targetIdOrPath)
//        {
//            Copy(GetNodeId(sourceIdOrPath), GetNodeId(targetIdOrPath));
//        }

//        public void Copy(int nodeID, int targetNodeID)
//        {
//            Node node = Node.LoadNode(nodeID);
//            Node target = Node.LoadNode(targetNodeID);
//            Node.Copy(node.Path, target.Path);
//        }

//        public void CopyMore(string nodeIdList, string targetPath)
//        {
//            if (String.IsNullOrEmpty(nodeIdList))
//                return;

//            if (String.IsNullOrEmpty(targetPath))
//                return;

//            var errors = new List<Exception>();

//            var nodeList = GetNodeIdList(nodeIdList);

//            Node.Copy(nodeList, targetPath, ref errors);
//            if (errors.Count != 0)
//                SendErrorsToUser(errors);
//        }

//        // Internals ////////////////////////////////////////////////////////////

//        // TODO: we have to consider the notification via service call if any error raises during deleting nodes. 
//        // TODO: implement client notification

//        private static void SendErrorsToUser(IList<Exception> errors)
//        {
//            foreach (var error in errors)
//                SnLog.WriteException(error);
//            throw new ApplicationException("Error(s) occured while deleting one or more contents. Errors are logged. Contact the system administrator for details.", errors[0]);
//        }

//        private static int GetNodeId(string nodeIdOrPath)
//        {
//            int nodeId;
//            if (Int32.TryParse(nodeIdOrPath, out nodeId))
//                return nodeId;
//            return NodeHead.Get(nodeIdOrPath).Id;
//        }

//        private static string PreparePath(string path)
//        {
//            return path.StartsWith("/") ? path : string.Concat("/", path);
//        }

//        private static Content CreateFakeRootForFeed(Content[] feed)
//        {
//            var rootNode = Node.LoadNode("/Root");
//            var c = new Content(rootNode, false, false, false, false, 0, 0)
//            {
//                Children = feed
//            };
//            c.ChildCount = c.Children.Length;

//            return c;
//        }

//        /// <summary>
//        /// Gets the node id list.
//        /// </summary>
//        /// <param name="nodeIdList">Comma-separeted node id list.</param>
//        /// <returns>List dictionary with identifiers.</returns>
//        private static List<Int32> GetNodeIdList(string nodeIdList)
//        {
//            var result = new List<Int32>();
//            if (String.IsNullOrEmpty(nodeIdList))
//                return result;

//            var nodeListArray = nodeIdList.Split(';').ToList();
//            nodeListArray.ForEach(item =>
//            {
//                Int32 number;
//                if (Int32.TryParse(item, out number))
//                    result.Add(number);
//            });
//            return result;
//        }


//    }
//    [global::System.Serializable]
//    public class NodeLoadException : ApplicationException
//    {
//        public NodeLoadException() { }
//        public NodeLoadException(string message) : base(message) { }
//        public NodeLoadException(string message, Exception inner) : base(message, inner) { }
//        protected NodeLoadException(
//          System.Runtime.Serialization.SerializationInfo info,
//          System.Runtime.Serialization.StreamingContext context)
//            : base(info, context)
//        { }
//    }
//}
