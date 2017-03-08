using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SenseNet.ContentRepository.Schema
{
    public class FieldMetadata
    {
        public PropertyInfo PropertyInfo { get; set; }
        public string FieldName { get; set; }
        public Type FieldType { get; set; }
        public Type PropertyType { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public FieldSetting FieldSetting { get; set; }

        public FieldMetadata() { }

        public FieldMetadata(bool canRead, bool canWrite, string fieldName, string fieldDisplayName, FieldSetting fieldSetting)
        {
            this.CanRead = canRead;
            this.CanWrite = canWrite;
            this.FieldName = fieldName;
            this.DisplayName = fieldDisplayName;
            this.FieldSetting = fieldSetting;
        }
    }
}
