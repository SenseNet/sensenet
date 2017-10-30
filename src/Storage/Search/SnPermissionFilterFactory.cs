using System;
using System.Collections.Generic;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search.Querying
{
    internal class SnPermissionFilterFactory : IPermissionFilterFactory
    {
        public IPermissionFilter Create(SnQuery query, IQueryContext context)
        {
            return new PermissionChecker(query, context);
        }
    }

}
