using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.OData.Formatters
{
    /// <summary>
    /// Defines an inherited <see cref="ODataFormatter"/> class for writing any OData response in verbose JSON format.
    /// </summary>
    public class VerbodeJsonFormatter : JsonFormatter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "verbosejson" in this case.</remarks>
        public override string FormatName => "verbosejson";

        /// <inheritdoc />
        /// <remarks>Returns with "application/json;odata=verbose" in this case.</remarks>
        public override string MimeType => "application/json;odata=verbose";
    }
}
