using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public class ExecutableIndexingActivitiesResult
    {
        public IIndexingActivity[]  Activities { get; set; }
        public int[] FinishedActivitiyIds { get; set; }
    }
}
