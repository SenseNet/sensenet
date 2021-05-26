using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Fields
{
    public class RichTextFieldSetting : FieldSetting
    {
        protected override void WriteConfiguration(XmlWriter writer)
        {
            // Do nothing because there are no configurable properties for this field.
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new RichTextIndexHandler();
        }
    }
}
