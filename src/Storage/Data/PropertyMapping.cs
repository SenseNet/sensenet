using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Data
{
    public enum PropertyStorageSchema
    {
        SingleColumn, MultiColumn, MultiTable
    }

    public class PropertyMapping
    {
        internal PropertyMapping() { }

        public PropertyStorageSchema StorageSchema { get; internal set; }
        public string TableName { get; internal set; }
        public string ColumnName { get; internal set; }
        public bool UsePageIndex { get; internal set; }
        public int PageIndex { get; internal set; }
    }
}
