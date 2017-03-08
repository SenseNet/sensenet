using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository
{
    [global::System.Serializable]
    public class FieldParsingException : ApplicationException
    {
        public FieldParsingException() { }
        public FieldParsingException(string message) : base(message) { }
        public FieldParsingException(string message, Exception inner) : base(message, inner) { }
        protected FieldParsingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        internal static FieldParsingException GetException(Field field)
        {
            return GetException(field, null);
        }
        internal static FieldParsingException GetException(Field field, Exception innerException)
        {
            string msg = String.Concat(
                "Field parsing error. Content: ", field.Content.Path,
                ", Field: ", field.Name,
                ", FieldType: ", field.FieldSetting.ShortName,
                ", field value type: ", field.FieldSetting.HandlerSlots[0][field.FieldSetting.HandlerSlotIndices[0]].FullName);
            return innerException == null ? new FieldParsingException(msg) : new FieldParsingException(msg, innerException);
        }
    }
}
