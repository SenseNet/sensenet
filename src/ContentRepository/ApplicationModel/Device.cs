using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Search;
using System.Web;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ApplicationModel
{
    [ContentHandler]
    public class Device : GenericContent, IFolder
    {
        internal Device Fallback { get; set; }

        public const string USERAGENTPATTERN = "UserAgentPattern";
        [RepositoryProperty(USERAGENTPATTERN, RepositoryDataType.String)]
        public string UserAgentPattern
        {
            get { return this.GetProperty<string>(USERAGENTPATTERN); }
            set { this[USERAGENTPATTERN] = value; }
        }

        public Device(Node parent) : this(parent, null) { }
        public Device(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Device(NodeToken nt) : base(nt) { }

        public virtual bool Identify(string userAgent)
        {
            var pattern = UserAgentPattern;
            if (String.IsNullOrEmpty(pattern))
                return false;
            if(String.IsNullOrEmpty(userAgent))
                return false;
            try
            {
                var expr = new System.Text.RegularExpressions.Regex(pattern);
                return expr.IsMatch(userAgent);
            }
            catch
            {
                SnLog.WriteWarning($"Invalid regular expression: {pattern} Device: {this.Path}");
            }

            return false;
        }

        public override void Save(NodeSaveSettings settings)
        {
            base.Save(settings);
            DeviceManager.Reset();
        }
        public override void Delete(bool bypassTrash)
        {
            base.Delete(bypassTrash);
            DeviceManager.Reset();
        }
        public override void ForceDelete()
        {
            base.ForceDelete();
            DeviceManager.Reset();
        }

        // ================================================ IFolder

        public virtual IEnumerable<Node> Children
        {
            get { return this.GetChildren(); }
        }
        public virtual int ChildCount
        {
            get { return this.GetChildCount(); }
        }

    }

}
