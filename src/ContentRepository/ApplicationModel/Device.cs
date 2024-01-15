using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage;
using Task = System.Threading.Tasks.Task;

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

        [Obsolete("Use async version instead.", true)]
        public override void Save(NodeSaveSettings settings)
        {
            SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
        }
        public override async Task SaveAsync(NodeSaveSettings settings, CancellationToken cancel)
        {
            await base.SaveAsync(settings, cancel).ConfigureAwait(false);
            DeviceManager.Reset();
        }

        [Obsolete("Use async version instead", true)]
        public override void Delete(bool bypassTrash)
        {
            DeleteAsync(bypassTrash, CancellationToken.None).GetAwaiter().GetResult();
        }
        public override async Task DeleteAsync(bool bypassTrash, CancellationToken cancel)
        {
            await base.DeleteAsync(bypassTrash, cancel);
            DeviceManager.Reset();
        }

        [Obsolete("Use async version instead", true)]
        public override void ForceDelete()
        {
            ForceDeleteAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public override async Task ForceDeleteAsync(CancellationToken cancel)
        {
            await base.ForceDeleteAsync(cancel);
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
