using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Querying
{
    internal class PermissionFilterFactory : IPermissionFilterFactory
    {
        public IPermissionFilter Create(SnQuery query, IQueryContext context)
        {
            return new PermissionFilter(query, context);
        }
    }

}
