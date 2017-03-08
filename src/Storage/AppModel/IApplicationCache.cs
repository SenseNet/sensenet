using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public interface IApplicationCache
    {
        IEnumerable<string> GetPaths(string appTypeName);
        void Invalidate(string appTypeName, string path);
    }

}
