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
            // Do nothing here because there is no any individual configurable element.
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new RichTextIndexHandler();
        }
    }
}
