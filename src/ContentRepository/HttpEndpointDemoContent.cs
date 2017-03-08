using System;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class HttpEndpointDemoContent : Application, IHttpHandler
    {

        public HttpEndpointDemoContent(Node parent) : this(parent, null) { }
        public HttpEndpointDemoContent(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected HttpEndpointDemoContent(NodeToken nt) : base(nt) { }


		[RepositoryProperty(RepositoryDataType.Int, PropertyName = "A")]
		public int A
		{
			get { return this.GetProperty<int>("A"); }
			set { this["A"] = value; }
		}

        [RepositoryProperty(RepositoryDataType.Int, PropertyName = "B")]
        public int B
        {
			get { return this.GetProperty<int>("B"); }
			set { this["B"] = value; }
		}

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "A":
                    return A;
                case "B":
                    return B;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "A":
                    A = (int)value;
                    break;
                case "B":
                    B = (int)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string responseString = string.Format("<html><head><title>Repository Path: {0}</title></head><body><div><h2>{0}</h2><br />A = {1}<br />B = {2}<br /><b>A + B = {3}</div></body></html>", this.Path, A, B, A + B);
            context.Response.Clear();
            context.Response.ContentType = "text/html";
            context.Response.Write(responseString);
            context.Response.End();
        }

    }

}
