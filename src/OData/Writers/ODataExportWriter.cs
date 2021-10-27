using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.OData.Writers
{
    public class ODataExportWriter : ODataJsonWriter
    {
        public override string FormatName => "export";
    }
}
