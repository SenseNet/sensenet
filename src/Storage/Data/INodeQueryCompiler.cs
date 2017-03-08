using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface INodeQueryCompiler
    {
        string Compile(NodeQuery query, out NodeQueryParameter[] parameters);
    }
}
