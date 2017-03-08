using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging.Steps
{
    internal class DisableIndexing : Step
    {
        public override void Execute(ExecutionContext context)
        {
            SenseNet.ContentRepository.Storage.StorageContext.Search.DisableOuterEngine();
        }
    }
}
