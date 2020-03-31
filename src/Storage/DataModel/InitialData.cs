using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    //TODO: The ContentTypeDefinitions, Blobs, Permissions are not saved and loaded in this version.
    public class InitialData
    {
        /// <summary>
        /// Gets or sets the relative filesystem path of the binary streams in the DynamicProperties.BinaryProperties
        /// If the streams are not in the filesystem, this value need to be null.
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// Gets or sets the new or modified schema items.
        /// </summary>
        public RepositorySchemaData Schema { get; set; }

        /// <summary>
        /// Gets or sets the new or modified NodeHead items.
        /// </summary>
        public IEnumerable<NodeHeadData> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the new or modified Version items.
        /// </summary>
        public IEnumerable<VersionData> Versions { get; set; }

        /// <summary>
        /// Gets or sets the new or modified dynamic property values by VersionId
        /// </summary>
        public IEnumerable<DynamicPropertyData> DynamicProperties { get; set; }

        /// <summary>
        /// Gets or sets key-value storage of the ContentTypeDefinitions when the key is the name of the ContentTypeDefinitions
        /// and value is the xml data.
        /// </summary>
        public IDictionary<string, string> ContentTypeDefinitions { get; set; }

        /// <summary>
        /// Gets or sets key-value storage of the used blobs.
        /// Key format: [PropertyType.Name]:[Path]. For example: "Binary:/Root/System/Settings/Logging.settings".
        /// Value is the blob as textual data.
        /// </summary>
        public IDictionary<string, string> Blobs { get; set; }

        public IList<string> Permissions { get; set; }

        /* ===================================================================================== SAVE */

        /// <summary>
        /// Persists the initial data from the active repository to separated files in the given directory.
        /// Initial data is the passed schema and nodes. If the schema is null,
        /// the whole current schema will be written.
        /// WARNING: The saved files are not contains the whole database but help for the C# source file generation.
        /// </summary>
        public static void Save(string tempFolderPath, SchemaEditor schema, Func<IEnumerable<int>> getNodeIds)
        {
            using (var ptw = new StreamWriter(Path.Combine(tempFolderPath, "propertyTypes.txt"), false, Encoding.UTF8))
            using (var ntw = new StreamWriter(Path.Combine(tempFolderPath, "nodeTypes.txt"), false, Encoding.UTF8))
            using (var nw = new StreamWriter(Path.Combine(tempFolderPath, "nodes.txt"), false, Encoding.UTF8))
            using (var vw = new StreamWriter(Path.Combine(tempFolderPath, "versions.txt"), false, Encoding.UTF8))
            using (var dw = new StreamWriter(Path.Combine(tempFolderPath, "dynamicData.txt"), false, Encoding.UTF8))
                InitialData.Save(ptw, ntw, nw, vw, dw, schema,
                    getNodeIds);
        }

        internal static void Save(TextWriter propertyTypeWriter, TextWriter nodeTypeWriter, TextWriter nodeWriter,
            TextWriter versionWriter, TextWriter dynamicDataWriter,
            SchemaEditor schema, Func<IEnumerable<int>> getNodeIds)
        {
            if (schema == null)
            {
                schema = new SchemaEditor();
                schema.Load();
            }
            var schemaData = schema.ToRepositorySchemaData();

            propertyTypeWriter.WriteLine("  Id| DataType  | Mapping| Name");
            propertyTypeWriter.WriteLine("----- ----------- -------- ---------------");
            foreach (var pt in schemaData.PropertyTypes)
                propertyTypeWriter.WriteLine($"{pt.Id,4:#}| {pt.DataType,-10}| {pt.Mapping,7:#0}| {pt.Name}");

            nodeTypeWriter.WriteLine("  Id| Name                          | ParentName                    | ClassName                                                   | Properties");
            nodeTypeWriter.WriteLine("----- ------------------------------- ------------------------------- ------------------------------------------------------------- ------------------------------------------");
            foreach (var nt in schemaData.NodeTypes)
                nodeTypeWriter.WriteLine(
                    $"{nt.Id,4:#}| {nt.Name,-30}| {nt.ParentName ?? "<null>",-30}| {nt.ClassName,-60}| " +
                    $"[{(string.Join(" ", nt.Properties))}]");

            nodeWriter.WriteLine("NodeId| TypeId| Parent|  Index| MinorV| MajorV| IsSystem| Creator| Modifier| Owner | Name                                    | DisplayName                                       | CreationDate              | Path");
            nodeWriter.WriteLine("------- ------- ------- ------- ------- ------- --------- -------- --------- ------- ----------------------------------------- --------------------------------------------------- ------------------------- -----------");
            versionWriter.WriteLine("VersionId| NodeId| Creator| Modifier|  Version");
            versionWriter.WriteLine("---------- ------- ------- ---------- ---------");
            foreach (var nodeId in getNodeIds())
            {
                var node = Node.LoadNode(nodeId);
                var dummy = node.PropertyTypes.Select(p => node[p]).ToArray();

                Write(nodeWriter, node.Data.GetNodeHeadData());
                Write(versionWriter, node.Data.GetVersionData());
                Write(dynamicDataWriter, node.Path, node.Data.GetDynamicData(true));
            }
        }
        private static void Write(TextWriter writer, NodeHeadData d)
        {
            writer.WriteLine($"{d.NodeId,6:#}| {d.NodeTypeId,6:#}| {d.ParentNodeId,6:#0}| {d.Index,6:#0}| " +
                             $"{d.LastMinorVersionId,6:#}| {d.LastMajorVersionId,6:#}| {d.IsSystem,8}| " +
                             $"{d.CreatedById,7:#}| {d.ModifiedById,8:#}| {d.OwnerId,6:#}| " +
                             $"{d.Name,-40}| {d.DisplayName,-50}| {d.CreationDate,25:yyyy-MM-dd HH:mm:ss.fffff} | {d.Path}");
        }
        private static void Write(TextWriter writer, VersionData d)
        {
            writer.WriteLine($"{d.VersionId,9:#}| {d.NodeId,6:#}| {d.CreatedById,7:#}| {d.ModifiedById,8:#}|  {d.Version}");
        }
        private static void Write(TextWriter writer, string path, DynamicPropertyData d)
        {
            var relevantBinaries =
                d.BinaryProperties.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);

            var transformedLongText = new Dictionary<string, string>();
            foreach (var item in d.LongTextProperties)
            {
                if (item.Value == null)
                    continue;
                var value = ValueToString(item);
                if (value != null)
                    transformedLongText.Add($"{item.Key.Name}", value);
            }
            var transformedDynamic = new Dictionary<string, string>();
            foreach (var item in d.DynamicProperties)
            {
                if (item.Value == null)
                    continue;
                var value = ValueToString(item);
                if (value != null)
                    transformedDynamic.Add($"{item.Key.Name}:{item.Key.DataType}", value);
            }
            foreach (var item in d.ReferenceProperties)
            {
                if (item.Value == null ||item.Value.Count == 0)
                    continue;
                var value = ValueToString(item);
                if (value != null)
                    transformedDynamic.Add($"{item.Key.Name}:{item.Key.DataType}", value);
            }

            if (relevantBinaries.Count + transformedLongText.Count + transformedDynamic.Count > 0)
            {
                writer.WriteLine($"VersionId: {d.VersionId}");

                if (relevantBinaries.Count > 0)
                {
                    writer.WriteLine("    BinaryProperties");
                    foreach (var item in relevantBinaries)
                    {
                        writer.WriteLine($"        {item.Key.Name}: {ValueToString(path, item.Value)}");
                    }
                }
                if (transformedLongText.Count > 0)
                {
                    writer.WriteLine("    LongTextProperties");
                    foreach (var item in transformedLongText)
                        writer.WriteLine($"        {item.Key}: {item.Value}");
                }
                if (transformedDynamic.Count > 0)
                {
                    writer.WriteLine("    DynamicProperties");
                    foreach (var item in transformedDynamic)
                        writer.WriteLine($"        {item.Key}: {item.Value}");
                }
            }
        }
        private static string ValueToString(string path, BinaryDataValue d)
        {
            //var streamValue = d.Stream?.GetType().Name ?? "null";
            // 2, 2, 31386, GenericContent.ContentType, text/xml, 
            return $"#{d.Id}, F{d.FileId}, {d.Size}L, {d.FileName}, {d.ContentType}, {path}";
        }
        private static string ValueToString(KeyValuePair<PropertyType, string> item)
        {
            // LongText property transformation (e.g. character escape (\t \r\n etc.) in the future)
            return item.Value.Replace("\r", " ").Replace("\n", " ");
        }
        private static string ValueToString(KeyValuePair<PropertyType, List<int>> item)
        {
            // Reference property transformation (id array)
            return "[" + string.Join(",", ((IEnumerable<int>)item.Value).Select(x => x.ToString())) + "]";
        }
        private static string ValueToString(KeyValuePair<PropertyType, object> item)
        {
            switch (item.Key.DataType)
            {
                case DataType.String:
                //case DataType.Text:
                    return (string)item.Value;
                case DataType.Int:
                    var intValue = (int)item.Value;
                    if (intValue == default(int))
                        return null;
                    return item.Value.ToString();
                case DataType.Currency:
                    var decimalValue = (decimal)item.Value;
                    if (decimalValue == default(decimal))
                        return null;
                    return ((decimal)item.Value).ToString(CultureInfo.InvariantCulture);
                case DataType.DateTime:
                    var dateTimeValue = (DateTime)item.Value;
                    if (dateTimeValue == default(DateTime))
                        return null;
                    return dateTimeValue.ToString("O");
                // ReSharper disable once RedundantCaseLabel
                case DataType.Binary:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /* ===================================================================================== LOAD */

        /// <summary>
        /// Loads the initial data from the given <see cref="IRepositoryDataFile"/> instance.
        /// </summary>
        public static InitialData Load(IRepositoryDataFile dataFile)
        {
            InitialData initialData;

            using (var ptr = new StringReader(dataFile.PropertyTypes))
            using (var ntr = new StringReader(dataFile.NodeTypes))
            using (var nr = new StringReader(dataFile.Nodes))
            using (var vr = new StringReader(dataFile.Versions))
            using (var dr = new StringReader(dataFile.DynamicData))
                initialData = InitialData.Load(ptr, ntr, nr, vr, dr);

            initialData.ContentTypeDefinitions = dataFile.ContentTypeDefinitions;
            initialData.Blobs = dataFile.Blobs;
            initialData.Permissions = dataFile.Permissions;

            return initialData;
        }

        /* ------------------------------------------------------------------------------------------ */

        /// <summary>
        /// Loads the initial data from the given directory.
        /// WARNING: The saved files are not contains the whole database but help for the C# source file generation.
        /// </summary>
        public static InitialData Load(string tempFolderPath)
        {
            using (var ptr = new StreamReader(Path.Combine(tempFolderPath, "propertyTypes.txt")))
            using (var ntr = new StreamReader(Path.Combine(tempFolderPath, "nodeTypes.txt")))
            using (var nr = new StreamReader(Path.Combine(tempFolderPath, "nodes.txt")))
            using (var vr = new StreamReader(Path.Combine(tempFolderPath, "versions.txt")))
            using (var dr = new StreamReader(Path.Combine(tempFolderPath, "dynamicData.txt")))
                return Load(ptr, ntr, nr, vr, dr);
        }

        private static InitialData Load(TextReader propertyTypeReader, TextReader nodeTypeReader,
            TextReader nodeReader, TextReader versionReader, TextReader dynamicDataReader)
        {
            var propertyTypes = ParseDatamodels<PropertyTypeData>(ReadLines(propertyTypeReader));
            var nodeTypes = ParseDatamodels<NodeTypeData>(ReadLines(nodeTypeReader));
            var nodes = ParseDatamodels<NodeHeadData>(ReadLines(nodeReader));
            var versions = ParseDatamodels<VersionData>(ReadLines(versionReader));
            var dynamicProperties = ParseDynamicProperties(ReadLines(dynamicDataReader));

            return new InitialData
            {
                Schema = new RepositorySchemaData
                {
                    PropertyTypes = propertyTypes,
                    NodeTypes = nodeTypes
                },
                Nodes = nodes,
                Versions = versions,
                DynamicProperties = dynamicProperties
            };
        }
        private static IEnumerable<string> ReadLines(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }
        private static List<T> ParseDatamodels<T>(IEnumerable<string> lines) where T : class, IDataModel, new()
        {
            var result = new List<T>();

            var isHead = true;
            var headLines = new List<string>();
            string[] head = null;
            foreach (var line in lines)
            {
                if (isHead)
                {
                    if ((line.StartsWith("-") || line.StartsWith("=")) && !line.Contains('|'))
                    {
                        isHead = false;
                        head = ParseLine(headLines.Last());
                    }
                    else
                    {
                        headLines.Add(line);
                    }
                    continue;
                }
                if(line.Trim().Length == 0)
                    continue;
                result.Add(Parse<T>(line, head));
            }

            return result;
        }
        private static T Parse<T>(string line, string[] head) where T : class, IDataModel, new()
        {
            var result = new T();

            var values = ParseLine(line);
            if (values.Length != head.Length)
                throw new ApplicationException($"The line is not valid: expected head: {string.Join("| ", head)}. " +
                                               $"Actual line: {line}");
            for (int i = 0; i < values.Length; i++)
            {
                var name = head[i];
                var value = values[i];
                if (value == "<null>")
                    value = null;
                result.SetProperty(name, value);
            }

            return result;
        }
        private static string[] ParseLine(string line)
        {
            return line.Split('|').Select(x => x.Trim()).ToArray();
        }
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static IEnumerable<DynamicPropertyData> ParseDynamicProperties(IEnumerable<string> lines)
        {
            var result = new List<DynamicPropertyData>();

            var parsingBinaryProperties = false;
            var parsingLongTextProperties = false;
            var parsingDynamicProperties = false;
            DynamicPropertyData data = null;
            var lineNumber = 0;
            foreach (var line in lines)
            {
                lineNumber++;
                if (line.Trim().Length == 0)
                    continue;
                if (line.StartsWith("VersionId: "))
                {
                    parsingBinaryProperties = false;
                    parsingLongTextProperties = false;
                    parsingDynamicProperties = false;
                    var versionId = int.Parse(line.Substring(11));
                    data = new DynamicPropertyData { VersionId = versionId };
                    result.Add(data);
                }
                else if (line == "    BinaryProperties")
                {
                    parsingBinaryProperties = true;
                    parsingLongTextProperties = false;
                    parsingDynamicProperties = false;
                    data.BinaryProperties = new Dictionary<PropertyType, BinaryDataValue>();
                }
                else if (line == "    LongTextProperties")
                {
                    parsingBinaryProperties = false;
                    parsingLongTextProperties = true;
                    parsingDynamicProperties = false;
                    data.LongTextProperties = new Dictionary<PropertyType, string>();
                }
                else if (line == "    DynamicProperties")
                {
                    parsingBinaryProperties = false;
                    parsingLongTextProperties = false;
                    parsingDynamicProperties = true;
                    data.DynamicProperties = new Dictionary<PropertyType, object>();
                }
                else if (line.StartsWith("        "))
                {
                    if (parsingBinaryProperties)
                    {
                        // name    id, file, size, filename,    mime,                     stream reference
                        // Binary: #82, F82, 0L, OAuth.settings, application/octet-stream, /Root/System/Settings/OAuth.settings
                        var src = line.Split(':', ',');
                        var propertyType = data.EnsurePropertyType(src[0].Trim(), DataType.Binary);
                        data.BinaryProperties.Add(propertyType, new BinaryDataValue
                        {
                            Id = int.Parse(src[1].Trim().Substring(1)),
                            FileId = int.Parse(src[2].Trim().Substring(1)),
                            Size = long.Parse(src[3].Trim().TrimEnd('L')),
                            FileName = src[4].Trim(),
                            ContentType = src[5].Trim(),
                            BlobProviderData = src[6].Trim()
                        });
                    }
                    else if (parsingLongTextProperties)
                    {
                        // OldPasswords: <?xml version="1.0" encoding="utf-16"?>  <ArrayOfOldPasswordData ....
                        var p = line.IndexOf(':');
                        var name = line.Substring(0, p).Trim();

                        var value = line.Substring(p + 1).Trim();
                        var propertyType = data.EnsurePropertyType(name, DataType.Text);

                        data.LongTextProperties.Add(propertyType, value);
                    }
                    else if (parsingDynamicProperties)
                    {
                        // LastLoggedOut:DateTime: 2018-11-14T02:54:03.0000000Z
                        var p = line.IndexOf(':');
                        var name = line.Substring(0, p).Trim();

                        var p1 = line.IndexOf(':', p + 1);
                        var dataType = (DataType)Enum.Parse(typeof(DataType), line.Substring(p + 1, p1 - p - 1));

                        var value = ParseDynamicValue(line.Substring(p1 + 1).Trim(), dataType);
                        var propertyType = data.EnsurePropertyType(name, dataType);

                        data.DynamicProperties.Add(propertyType, value);
                    }
                    else
                        throw GetCannotParseDynamicPropertiesException(lineNumber, line);
                }
                else
                    throw GetCannotParseDynamicPropertiesException(lineNumber, line);
            }

            return result;
        }
        private static object ParseDynamicValue(string src, DataType dataType)
        {
            switch (dataType)
            {
                case DataType.String:
                case DataType.Text:
                    return src;
                case DataType.Int:
                    return int.Parse(src);
                case DataType.Currency:
                    return decimal.Parse(src, CultureInfo.InvariantCulture);
                case DataType.DateTime:
                    return DateTime.Parse(src, CultureInfo.InvariantCulture);
                case DataType.Reference:
                    return src.Substring(1, src.Length - 2).Split(',').Select(int.Parse).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
        private static Exception GetCannotParseDynamicPropertiesException(int lineNumber, string line)
        {
            return new ApplicationException(
                $"Cannot parse the dynamic properties because the line {lineNumber} is invalid: " + line);
        }

        /* ===================================================================================== TOOLS */

        public byte[] GetBlobBytes(string repositoryPath, string propertyTypeName = null)
        {
            string fileContent = null;
            if (repositoryPath.StartsWith("/Root/System/Schema/ContentTypes/", StringComparison.OrdinalIgnoreCase))
            {
                var ctdName = RepositoryPath.GetFileName(repositoryPath);
                ContentTypeDefinitions.TryGetValue(ctdName, out fileContent);
            }
            else
            {
                var key = $"{propertyTypeName}:{repositoryPath}";
                Blobs.TryGetValue(key, out fileContent);
            }
            if (fileContent == null)
                return new byte[0];

            if (fileContent.StartsWith("[bytes]:\r\n"))
            {
                // bytes
                return ParseHexDump(fileContent.Substring(10));
            }

            // text
            if (fileContent.StartsWith("[text]:\r\n"))
                fileContent = fileContent.Substring(9);
            var byteCount = Encoding.UTF8.GetByteCount(fileContent);
            var bom = Encoding.UTF8.GetPreamble();
            var bytes = new byte[bom.Length + byteCount];

            bom.CopyTo(bytes, 0);
            Encoding.UTF8.GetBytes(fileContent, 0, fileContent.Length, bytes, bom.Length);

            return bytes;
        }

        public static string GetHexDump(Stream stream)
        {
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            return GetHexDump(bytes);
        }
        public static string GetHexDump(byte[] bytes)
        {
            var sb = new StringBuilder();
            var chars = new char[16];
            var nums = new string[16];

            void AddLine()
            {
                sb.Append(string.Join(" ", nums));
                sb.Append(" ");
                sb.AppendLine(string.Join("", chars));
            }

            // add lines
            var col = 0;
            foreach (var @byte in bytes)
            {
                nums[col] = (@byte.ToString("X2"));
                chars[col] = @byte < 32 || @byte >= 127 || @byte == '"' || @byte == '\\' ? '.' : (char)@byte;
                if (++col == 16)
                {
                    AddLine();
                    col = 0;
                }
            }

            // rest
            if (col > 0)
            {
                for (var i = col; i < 16; i++)
                {
                    nums[i] = "  ";
                    chars[i] = ' ';
                }
                AddLine();
            }

            return sb.ToString();
        }

        public static byte[] ParseHexDump(string src)
        {
            var lines = src.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var bytes = new List<byte>(lines.Length * 16);
            foreach (var line in lines)
            {
                var nums = line.Substring(0, 16 * 3).Trim().Split(' ');
                foreach (var num in nums)
                {
                    var @byte = byte.Parse(num, NumberStyles.HexNumber);
                    bytes.Add(@byte);
                }
            }

            return bytes.ToArray();
        }
    }
}