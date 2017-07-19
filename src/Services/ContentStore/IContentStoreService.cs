//using System;
//using System.ServiceModel;
//using System.ServiceModel.Web;

//namespace SenseNet.Services.ContentStore
//{
//    [ServiceContract(Namespace = "http://schemas.sensenet.com/services/contentstoreservice")]
//    [ServiceKnownType(typeof(Content))]
//    [ServiceKnownType(typeof(Content[]))]
//    [ServiceKnownType(typeof(EntityReference))]
//    public interface IContentStoreService
//    {
//        // queries //////////////////////////////////////////////////////////////
//		[OperationContract]
//		[WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetItem2?node={path}&onlyFileChildren={onlyFileChildren}&start={start}&limit={limit}")]
//		[FaultContract(typeof(NodeLoadException))]
//		Content GetItem2(string path, bool onlyFileChildren, int start, int limit);

//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetItem3?node={path}&withProperties={withProperties}&onlyFileChildren={onlyFileChildren}&start={start}&limit={limit}")]
//        [FaultContract(typeof(NodeLoadException))]
//        Content GetItem3(string path, bool withProperties, bool onlyFileChildren, int start, int limit);

//        // delete ///////////////////////////////////////////////////////////////
//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/Delete?node={nodeID}")]
//        [FaultContract(typeof(NodeLoadException))]
//        void Delete(int nodeID);

//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/DeleteMore?nodeList={nodeIdList}")]
//        [FaultContract(typeof(NodeLoadException))]
//        void DeleteMore(string nodeIdList);

//        // move /////////////////////////////////////////////////////////////////
//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/Move?node={nodeID}&target={targetNodeID}")]
//        [FaultContract(typeof (NodeLoadException))]
//        void Move(int nodeID, int targetNodeID);

//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json,
//            UriTemplate = "/MoveMore?nodeList={nodeIdList}&target={targetNodeId}")]
//        [FaultContract(typeof (NodeLoadException))]
//        void MoveMore(string nodeIdList, string targetNodePath);

//        // copy /////////////////////////////////////////////////////////////////
//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/Copy?node={nodeID}&target={targetNodeID}")]
//        [FaultContract(typeof(NodeLoadException))]
//        void Copy(int nodeID, int targetNodeID);

//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/CopyMore?nodeList={nodeIdList}&targetPath={targetPath}")]
//        [FaultContract(typeof (NodeLoadException))]
//        void CopyMore(string nodeIdList, string targetPath);


//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetContentTypes")]
//        [FaultContract(typeof(NodeLoadException))]
//        Content[] GetContentTypes();

//		[OperationContract]
//		[WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetFeed2?node={path}&onlyFiles={onlyFiles}&onlyFolders={onlyFolders}&start={start}&limit={limit}")]
//		[FaultContract(typeof(NodeLoadException))]
//		Content[] GetFeed2(string path, bool onlyFiles, bool onlyFolders, int start, int limit);

//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/Search?expr={searchExpression}")]
//        [FaultContract(typeof(NodeLoadException))]
//        Content Search(string searchExpression);

//        [OperationContract]
//        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/Query?expr={QueryXml}&withProperties={withProperties}")]
//        [FaultContract(typeof(NodeLoadException))]
//        Content Query(string queryXml, bool withProperties);
//    }

//}