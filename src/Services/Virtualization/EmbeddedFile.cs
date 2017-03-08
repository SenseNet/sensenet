using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Hosting;

namespace SenseNet.Portal.Virtualization
{
    internal class EmbeddedFile : VirtualFile
    {
        private readonly string _asmFullName;
        private readonly string _resourceFullName;
        public EmbeddedFile(string virtualPath):base(virtualPath)
        {
            
            var pathElements = virtualPath.Split('/');
            int idx = 0;
            int asmIdx = -1;
            while (idx < pathElements.Length && asmIdx < 0)
            {
                if (pathElements[idx].ToLower().Equals("!assembly"))
                    asmIdx = idx;
                idx++;
            }
            

            _asmFullName = pathElements[asmIdx + 1];
            _resourceFullName = pathElements[asmIdx + 2];

           
        }
        public override System.IO.Stream Open()
        {
             var asm = Assembly.Load(_asmFullName);
            if (asm == null)
                throw new ApplicationException(string.Format("{0} not found. EmbeddedFile cannot be served.", VirtualPath));

            var stream = asm.GetManifestResourceStream(_resourceFullName);
            if(stream == null)
                throw new ApplicationException(string.Format("{0} not found. EmbeddedFile cannot be served.", VirtualPath));
            return stream;
        }
    }
}
