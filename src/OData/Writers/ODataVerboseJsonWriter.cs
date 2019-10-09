// ReSharper disable StringLiteralTypo
namespace SenseNet.OData.Writers
{
    /// <summary>
    /// Defines an inherited <see cref="ODataWriter"/> class for writing any OData response in verbose JSON format.
    /// </summary>
    public class ODataVerboseJsonWriter : ODataJsonWriter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "verbosejson" in this case.</remarks>
        public override string FormatName => "verbosejson";

        /// <inheritdoc />
        /// <remarks>Returns with "application/json;odata=verbose" in this case.</remarks>
        public override string MimeType => "application/json;odata=verbose";
    }
}
