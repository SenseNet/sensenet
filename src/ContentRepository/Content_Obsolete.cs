using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SenseNet.ContentRepository
{
    public partial class Content
    {
        [Obsolete("Use Content.Create instead")]
        public Content()
        {

        }
    }
}
