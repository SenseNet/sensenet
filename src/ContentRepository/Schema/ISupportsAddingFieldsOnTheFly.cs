using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Schema
{
    public interface ISupportsAddingFieldsOnTheFly
    {
        bool AddFields(IEnumerable<FieldMetadata> fields);
    }
}
