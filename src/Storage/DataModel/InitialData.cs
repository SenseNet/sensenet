﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class InitialData
    {        /// <summary>
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

        /* ===================================================================================== SAVE */

        /// <summary>
        /// Persists the initial data from the active repository to the given readers.
        /// Initial data is the passed schema and nodes. If the schema is null,
        /// the whole current schema will be written.
        /// </summary>
        public static void Save(TextWriter propertyTypeWriter, TextWriter nodeTypeWriter, TextWriter nodeWriter,
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

            nodeWriter.WriteLine("NodeId| TypeId| Parent|  Index| MinorV| MajorV| IsSystem| Owner | Name                                    | DisplayName                                       | Path");
            nodeWriter.WriteLine("------- ------- -------  ------ ------- ------- --------- ------- ----------------------------------------- --------------------------------------------------- -------------------------------------");
            versionWriter.WriteLine("VersionId| NodeId|  Version");
            versionWriter.WriteLine("---------- ------- ---------");
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
                             $"{d.OwnerId,6:#}| {d.Name,-40}| {d.DisplayName,-50}| {d.Path}");
        }
        private static void Write(TextWriter writer, VersionData d)
        {
            writer.WriteLine($"{d.VersionId,9:#}| {d.NodeId,6:#}|  {d.Version}");
        }
        private static void Write(TextWriter writer, string path, DynamicPropertyData d)
        {
            var relevantBinaries =
                d.BinaryProperties.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);

            var transformedDynamic = new Dictionary<string, string>();
            foreach (var item in d.DynamicProperties)
            {
                if (item.Value == null)
                    continue;
                var value = ValueToString(item);
                if (value != null)
                    transformedDynamic.Add($"{item.Key.Name}:{item.Key.DataType}", value);
            }

            if (relevantBinaries.Count > 0 || transformedDynamic.Count > 0)
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
        private static string ValueToString(KeyValuePair<PropertyType, object> item)
        {
            switch (item.Key.DataType)
            {
                case DataType.String:
                case DataType.Text:
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
                case DataType.Reference:
                    return "[" + string.Join(",", ((IEnumerable<int>)item.Value).Select(x => x.ToString())) + "]";
                // ReSharper disable once RedundantCaseLabel
                case DataType.Binary:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /* ===================================================================================== LOAD */

        /// <summary>
        /// Loads the initial data from the given readers.
        /// </summary>
        public static InitialData Load(TextReader propertyTypeReader, TextReader nodeTypeReader,
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
                        if (headLines.Count > 1)
                            throw new NotImplementedException();
                        head = ParseLine(headLines[0]);
                    }
                    else
                    {
                        headLines.Add(line);
                    }
                    continue;
                }
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
                result.SetProperty(name, value);
            }

            return result;
        }
        private static string[] ParseLine(string line)
        {
            return line.Split('|').Select(x => x.Trim()).ToArray();
        }
        private static IEnumerable<DynamicPropertyData> ParseDynamicProperties(IEnumerable<string> lines)
        {
            var result = new List<DynamicPropertyData>();

            var parsingBinaryProperties = false;
            var parsingDynamicProperties = false;
            int versionId;
            DynamicPropertyData data = new DynamicPropertyData();
            var lineNumber = 0;
            foreach (var line in lines)
            {
                lineNumber++;
                if (line.StartsWith("VersionId: "))
                {
                    parsingBinaryProperties = false;
                    parsingDynamicProperties = false;
                    versionId = int.Parse(line.Substring(11));
                    data = new DynamicPropertyData { VersionId = versionId };
                    result.Add(data);
                }
                else if (line == "    BinaryProperties")
                {
                    parsingBinaryProperties = true;
                    parsingDynamicProperties = false;
                    data.BinaryProperties = new Dictionary<PropertyType, BinaryDataValue>();
                }
                else if (line == "    DynamicProperties")
                {
                    parsingBinaryProperties = false;
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
                    return src.Substring(1, src.Length - 2).Split(',').Select(int.Parse).ToArray();
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
        private static Exception GetCannotParseDynamicPropertiesException(int lineNumber, string line)
        {
            return new ApplicationException(
                $"Cannot parse the dynamic properties because the line {lineNumber} is invalid: " + line);
        }
    }
}
