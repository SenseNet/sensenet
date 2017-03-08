using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Schema
{
    public interface ISupportsDynamicFields
    {
        IDictionary<string, FieldMetadata> GetDynamicFieldMetadata();
        object GetProperty(string name);
        void SetProperty(string name, object value);
        bool IsNewContent { get; }
        void ResetDynamicFields();
    }
}
