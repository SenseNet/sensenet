using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderTests : TestBase
    {
        private string _initialPropertyTypePath = @"D:\propertyTypes.txt";
        private string _initialNodeTypePath = @"D:\nodeTypes.txt";
        private string _initialNodesPath = @"D:\nodes.txt";
        private string _initialVersionsPath = @"D:\versions.txt";
        private string _initialDynamicDataPath = @"D:\dynamicData.txt";

        [TestMethod]
        public void InitialData_ToString()
        {
            DPTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var schemaData = ed.ToRepositorySchemaData();

                using (var writer = new StreamWriter(_initialPropertyTypePath, false))
                {
                    writer.WriteLine("  Id| DataType  | Mapping| Name");
                    writer.WriteLine("----- ----------- -------- ---------------");
                    foreach (var pt in schemaData.PropertyTypes)
                        writer.WriteLine($"{pt.Id,4:#}| {pt.DataType,-10}| {pt.Mapping,7:#0}| {pt.Name}");
                }

                using (var writer = new StreamWriter(_initialNodeTypePath, false))
                {
                    writer.WriteLine("  Id| Name                          | ParentName                    | ClassName                                                   | Properties");
                    writer.WriteLine("----- ------------------------------- ------------------------------- ------------------------------------------------------------- ------------------------------------------");
                    foreach (var nt in schemaData.NodeTypes)
                        writer.WriteLine($"{nt.Id,4:#}| {nt.Name,-30}| {nt.ParentName ?? "<null>",-30}| {nt.ClassName,-60}| " +
                                         $"[{(string.Join(" ", nt.Properties))}]");
                }

                using (var nodeWriter = new StreamWriter(_initialNodesPath, false))
                using (var versionWriter = new StreamWriter(_initialVersionsPath, false))
                using (var dynamicDataWriter = new StreamWriter(_initialDynamicDataPath, false))
                {
                    nodeWriter.WriteLine("NodeId| TypeId| Parent|  Index| MinorV| MajorV| IsSystem| Owner | Name                                    | DisplayName                                       | Path");
                    nodeWriter.WriteLine("------- ------- -------  ------ ------- ------- --------- ------- ----------------------------------------- --------------------------------------------------- -------------------------------------");
                    versionWriter.WriteLine("VersionId| NodeId|  Version");
                    versionWriter.WriteLine("---------- ------- ---------");
                    foreach (var nodeId in ((InMemoryDataProvider)DataProvider.Current).DB.Nodes.Select(x => x.NodeId))
                    {
                        var node = Node.LoadNode(nodeId);
                        var dummy = node.PropertyTypes.Select(p => node[p]).ToArray();

                        Write(nodeWriter, node.Data.GetNodeHeadData());
                        Write(versionWriter, node.Data.GetVersionData());
                        Write(dynamicDataWriter, node.Path, node.Data.GetDynamicData(true));
                    }
                }

                Assert.Inconclusive();
            });
        }
        private void Write(TextWriter writer, NodeHeadData d)
        {
            writer.WriteLine($"{d.NodeId,6:#}| {d.NodeTypeId,6:#}| {d.ParentNodeId,6:#0}| {d.Index,6:#0}| " +
                             $"{d.LastMinorVersionId,6:#}| {d.LastMajorVersionId,6:#}| {d.IsSystem,8}| " +
                             $"{d.OwnerId,6:#}| {d.Name,-40}| {d.DisplayName,-50}| {d.Path}");
        }
        private void Write(TextWriter writer, VersionData d)
        {
            writer.WriteLine($"{d.VersionId,9:#}| {d.NodeId,6:#}|  {d.Version}");
        }
        private void Write(TextWriter writer, string path, DynamicPropertyData d)
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
        private string ValueToString(string path, BinaryDataValue d)
        {
            //var streamValue = d.Stream?.GetType().Name ?? "null";
            // 2, 2, 31386, GenericContent.ContentType, text/xml, 
            return $"#{d.Id}, F{d.FileId}, {d.Size}L, {d.FileName}, {d.ContentType}, {path}";
        }
        private string ValueToString(KeyValuePair<PropertyType, object> item)
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
                    var dateTimeValue = (DateTime) item.Value;
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

        [TestMethod]
        public void InitialData_Parse()
        {
            var propertyTypes = ParseDatamodels<PropertyTypeData>(ReadLines(_initialPropertyTypePath));
            var nodeTypes = ParseDatamodels<NodeTypeData>(ReadLines(_initialNodeTypePath));
            var nodes = ParseDatamodels<NodeHeadData>(ReadLines(_initialNodesPath));
            var versions = ParseDatamodels<VersionData>(ReadLines(_initialVersionsPath));
            var dynamicProperties = ParseDynamicProperties(ReadLines(_initialDynamicDataPath));

            var dataPackage = new DataPackage
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

            Assert.Inconclusive();
        }
        private IEnumerable<string> ReadLines(string path)
        {
            using (var reader = new StreamReader(path))
                foreach (var item in ReadLines(reader))
                    yield return item;
        }
        private IEnumerable<string> ReadLines(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }
        private List<T> ParseDatamodels<T>(IEnumerable<string> lines) where T: class, IDataModel, new()
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
        private T Parse<T>(string line, string[] head) where T : class, IDataModel, new()
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
        private string[] ParseLine(string line)
        {
            return line.Split('|').Select(x => x.Trim()).ToArray();
        }
        private IEnumerable<DynamicPropertyData> ParseDynamicProperties(IEnumerable<string> lines)
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
                    data = new DynamicPropertyData{VersionId = versionId};
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
                        var propertyType = data.EnsurePropertyType(src[0], DataType.Binary);
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
                        var dataType = (DataType) Enum.Parse(typeof(DataType), line.Substring(p + 1, p1 - p - 1));

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
        private object ParseDynamicValue(string src, DataType dataType)
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

        private Exception GetCannotParseDynamicPropertiesException(int lineNumber, string line)
        {
            return new ApplicationException(
                $"Cannot parse the dynamic properties because the line {lineNumber} is invalid: " + line);
        }



        // The prefix DP_AB_ means: DataProvider A-B comparative test when A is the 
        //     old in-memory DataProvider implementation and B is the new one.

        //[TestMethod]
        //public void DP_AB_Schema_Save()
        //{
        //    DPTest(() =>
        //    {
        //        var storedSchema = GetStoredSchema();
        //        Assert.AreEqual(0L, storedSchema.Timestamp);
        //        Assert.IsNull(storedSchema.PropertyTypes);
        //        Assert.IsNull(storedSchema.NodeTypes);
        //        Assert.IsNull(storedSchema.ContentListTypes);

        //        var ed = new SchemaEditor();
        //        ed.Load();
        //        var xml = new XmlDocument();
        //        xml.LoadXml(ed.ToXml());

        //        DataStore.Enabled = true;
        //        var ed2 = new SchemaEditor();
        //        ed2.Load(xml);
        //        ed2.Register();

        //        storedSchema = GetStoredSchema();

        //        Assert.IsTrue(0L < storedSchema.Timestamp);
        //        Assert.AreEqual(ActiveSchema.PropertyTypes.Count, storedSchema.PropertyTypes.Count);
        //        Assert.AreEqual(ActiveSchema.NodeTypes.Count, storedSchema.NodeTypes.Count);
        //        Assert.AreEqual(ActiveSchema.ContentListTypes.Count, storedSchema.ContentListTypes.Count);
        //        //UNDONE:DB ----Deep check: storedSchema
        //    });
        //}
        //private RepositorySchemaData GetStoredSchema()
        //{
        //    return ((InMemoryDataProvider2) Providers.Instance.DataProvider2).DB.Schema;
        //}

        [TestMethod]
        public void DP_AB_Create()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                // ACTION-A
                DataStore.SnapshotsEnabled = true;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                
                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();

                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DP_AB_Create_TextProperty()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                var description = "text property value.";

                // ACTION-A
                DataStore.SnapshotsEnabled = true;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Description = description;
                folderA.Save();

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Description = description;
                folderB.Save();

                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DP_AB_CreateFile()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                var filecontent = "File content.";

                // ACTION-A
                DataStore.SnapshotsEnabled = false;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                DataStore.SnapshotsEnabled = false;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                var fileBId = fileB.Id;
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileBId);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent, reloadedFileContentA);
                Assert.AreEqual(filecontent, reloadedFileContentB);
            });
        }

        [TestMethod]
        public void DP_AB_Update()
        {
            // TESTED: DataProvider2: UpdateNodeAsync(NodeData nodeData, NodeSaveSettings settings, IEnumerable<int> versionIdsToDelete)

            DPTest(() =>
            {
                // PROVIDER-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                folderA = Node.Load<SystemFolder>(folderA.Id);
                folderA.Index++;
                DataStore.SnapshotsEnabled = true;
                folderA.Save();
                DataStore.SnapshotsEnabled = false;

                // PROVIDER-B
                DataStore.Enabled = true;
                DistributedApplication.Cache.Reset();
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                folderB = Node.Load<SystemFolder>(folderB.Id);
                folderB.Index++;
                DataStore.SnapshotsEnabled = true;
                folderB.Save();
                DataStore.SnapshotsEnabled = false;

                // Check
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;

                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                Assert.IsTrue(nodeDataBeforeB.NodeTimestamp < nodeDataAfterB.NodeTimestamp);
                Assert.IsTrue(nodeDataBeforeB.VersionTimestamp < nodeDataAfterB.VersionTimestamp);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DP_AB_UpdateFile_SameVersion()
        {
            // TESTED: DataProvider2: UpdateNodeAsync(NodeData nodeData, NodeSaveSettings settings, IEnumerable<int> versionIdsToDelete)

            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent2, reloadedFileContentA);
                Assert.AreEqual(filecontent2, reloadedFileContentB);
            });
        }
        [TestMethod]
        public void DP_AB_UpdateFile_NewVersion()
        {
            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1",VersioningMode = VersioningType.MajorAndMinor };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1", VersioningMode = VersioningType.MajorAndMinor};
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent2, reloadedFileContentA);
                Assert.AreEqual(filecontent2, reloadedFileContentB);
            });
        }
        [TestMethod]
        public void DP_AB_UpdateFile_ExpectedVersion()
        {
            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA.CheckOut();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                fileA.Save();
                DataStore.SnapshotsEnabled = true;
                fileA.CheckIn();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB.CheckOut();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                fileB.Save();
                DataStore.SnapshotsEnabled = true;
                fileB.CheckIn();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent2, reloadedFileContentA);
                Assert.AreEqual(filecontent2, reloadedFileContentB);
            });
        }
        [TestMethod]
        public void DP_AB_Update_HeadOnly()
        {
            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                // ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA.CheckOut();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                fileA.Save();
                DataStore.SnapshotsEnabled = true;
                fileA.UndoCheckOut();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB.CheckOut();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                fileB.Save();
                DataStore.SnapshotsEnabled = true;
                fileB.UndoCheckOut();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent1, reloadedFileContentA);
                Assert.AreEqual(filecontent1, reloadedFileContentB);
            });
        }

        [TestMethod]
        public void DP_HandleAllDynamicProps()
        {
            var contentTypeName = "TestContent";
            var ctd = $"<ContentType name='{contentTypeName}' parentType='GenericContent'" + @"
             handler='SenseNet.ContentRepository.GenericContent'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <Fields>
    <Field name='ShortText1' type='ShortText'/>
    <Field name='LongText1' type='LongText'/>
    <Field name='Integer1' type='Integer'/>
    <Field name='Number1' type='Number'/>
    <Field name='DateTime1' type='DateTime'/>
    <Field name='Reference1' type='Reference'/>
  </Fields>
</ContentType>
";
            DPTest(() =>
            {
                try
                {
                    ContentTypeInstaller.InstallContentType(ctd);
                    var unused = ContentType.GetByName(contentTypeName); // preload schema
                    DataStore.Enabled = true;

                    var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                    folderB.Save();

                    var db = GetDb();

                    // ACTION-1 CREATE
                    // Create all kind of dynamic properties
                    var nodeB = new GenericContent(folderB, contentTypeName)
                    {
                        Name = $"{contentTypeName}1",
                        ["ShortText1"] = "ShortText value 1",
                        ["LongText1"] = "LongText value 1",
                        ["Integer1"] = 42,
                        ["Number1"] = 42.56m,
                        ["DateTime1"] = new DateTime(1111, 11, 11)
                    };
                    nodeB.AddReference("Reference1", Repository.Root);
                    nodeB.AddReference("Reference1", folderB);
                    nodeB.Save();

                    // ASSERT-1
                    var storedProps = db.Versions[nodeB.VersionId].DynamicProperties;
                    Assert.AreEqual("ShortText value 1", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 1", storedProps["LongText1"]);
                    Assert.AreEqual(42, storedProps["Integer1"]);
                    Assert.AreEqual(42.56m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 11), storedProps["DateTime1"]);
                    Assert.AreEqual($"{Repository.Root.Id},{folderB.Id}", ArrayToString((int[])storedProps["Reference1"]));

                    // ACTION-2 UPDATE-1
                    nodeB = Node.Load<GenericContent>(nodeB.Id);
                    // Update all kind of dynamic properties
                    nodeB["ShortText1"] = "ShortText value 2";
                    nodeB["LongText1"] = "LongText value 2";
                    nodeB["Integer1"] = 43;
                    nodeB["Number1"] = 42.099m;
                    nodeB["DateTime1"] = new DateTime(1111, 11, 22);
                    nodeB.RemoveReference("Reference1", Repository.Root);
                    nodeB.Save();

                    // ASSERT-2
                    storedProps = db.Versions[nodeB.VersionId].DynamicProperties;
                    Assert.AreEqual("ShortText value 2", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 2", storedProps["LongText1"]);
                    Assert.AreEqual(43, storedProps["Integer1"]);
                    Assert.AreEqual(42.099m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 22), storedProps["DateTime1"]);
                    Assert.AreEqual($"{folderB.Id}", ArrayToString((int[])storedProps["Reference1"]));

                    // ACTION-3 UPDATE-2
                    nodeB = Node.Load<GenericContent>(nodeB.Id);
                    // Remove existing references
                    nodeB.RemoveReference("Reference1", folderB);
                    nodeB.Save();

                    // ASSERT-3
                    storedProps = db.Versions[nodeB.VersionId].DynamicProperties;
                    Assert.AreEqual("ShortText value 2", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 2", storedProps["LongText1"]);
                    Assert.AreEqual(43, storedProps["Integer1"]);
                    Assert.AreEqual(42.099m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 22), storedProps["DateTime1"]);
                    Assert.IsFalse(storedProps.ContainsKey("Reference1"));
                }
                finally
                {
                    DataStore.Enabled = false;
                    ContentTypeInstaller.RemoveContentType(contentTypeName);
                }
            });
        }

        [TestMethod]
        public void DP_Rename()
        {
            DPTest(() =>
            {
                // Create a small subtree
                DataStore.Enabled = true;
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var f1 = new SystemFolder(root) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(root) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();

                // ACTION: Rename root
                root = Node.Load<SystemFolder>(root.Id);
                root.Name = "RENAMED";
                root.Save();

                // ASSERT
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual("/Root/RENAMED", root.Path);
                Assert.AreEqual("/Root/RENAMED/F1", f1.Path);
                Assert.AreEqual("/Root/RENAMED/F2", f2.Path);
                Assert.AreEqual("/Root/RENAMED/F1/F3", f3.Path);
                Assert.AreEqual("/Root/RENAMED/F1/F4", f4.Path);
            });
        }

        //UNDONE:DB TEST: DP_AB_Create and Rollback
        //UNDONE:DB TEST: DP_AB_Update and Rollback

        /* ================================================================================================== */

        private InMemoryDataBase2 GetDb()
        {
            return ((InMemoryDataProvider2)Providers.Instance.DataProvider2).DB;
        }
        private string ArrayToString(int[] array) //UNDONE:DB --------Move to TestBase
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }

        private void CheckDynamicDataByVersionId(int versionId)
        {
            DataStore.SnapshotsEnabled = false;
            DataStore.Snapshots.Clear();

            DataStore.Enabled = false;
            DistributedApplication.Cache.Reset();
            var nodeA = Node.LoadNodeByVersionId(versionId);
            var unused1 = nodeA.PropertyTypes.Select(p => $"{p.Name}:{nodeA[p]}").ToArray();

            DataStore.Enabled = true;
            DistributedApplication.Cache.Reset();
            var nodeB = Node.LoadNodeByVersionId(versionId);
            var unused2 = nodeB.PropertyTypes.Select(p => $"{p.Name}:{nodeB[p]}").ToArray();

            DataProviderChecker.Assert_AreEqual(nodeA.Data, nodeB.Data);
        }

        private void DPTest(Action callback)
        {
            DataStore.Enabled = false;
            DataStore.SnapshotsEnabled = false;

            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            var dp2 = new InMemoryDataProvider2();
            Providers.Instance.DataProvider2 = dp2;
            Providers.Instance.BlobMetaDataProvider2 = new InMemoryBlobStorageMetaDataProvider2(dp2);

            using (Repository.Start(builder))
            {
                DataStore.InstallDataPackage(GetInitialStructure());
                new SnMaintenance().Shutdown();
                using (new SystemAccount())
                    callback();
            }
        }

        private DataPackage GetInitialStructure()
        {
            /*
            */
            return new DataPackage
            {
                RootPath = @"..\..\..\..\nuget\snadmin\install-services\import",
                Schema = new RepositorySchemaData
                {
                    PropertyTypes = new List<PropertyTypeData>
                    {
                        new PropertyTypeData {Id = 1, DataType = DataType.Binary, Name = "Binary", Mapping = 0}
                    },
                    NodeTypes = new List<NodeTypeData>
                    {
                        new NodeTypeData
                        {
                           
                        }
                    },
                    //ContentListTypes = new List<ContentListTypeData>()
                },
                Nodes = new[]
                {
                    new NodeHeadData
                    {

                    }
                }
            };
            //InstallNode(1, 1, 3, 5, "Admin", "/Root/IMS/BuiltIn/Portal/Admin");
            //InstallNode(2, 2, 4, 0, "Root", "/Root");
            //InstallNode(3, 3, 6, 2, "IMS", "/Root/IMS");
            //InstallNode(4, 4, 7, 3, "BuiltIn", "/Root/IMS/BuiltIn");
            //InstallNode(5, 5, 8, 4, "Portal", "/Root/IMS/BuiltIn/Portal");
            //InstallNode(6, 6, 3, 5, "Visitor", "/Root/IMS/BuiltIn/Portal/Visitor");
            //InstallNode(7, 7, 2, 5, "Administrators", "/Root/IMS/BuiltIn/Portal/Administrators");
            //InstallNode(8, 8, 2, 5, "Everyone", "/Root/IMS/BuiltIn/Portal/Everyone");
            //InstallNode(9, 9, 2, 5, "Owners", "/Root/IMS/BuiltIn/Portal/Owners");
            //InstallNode(10, 10, 3, 5, "Somebody", "/Root/IMS/BuiltIn/Portal/Somebody");
            //InstallNode(11, 11, 2, 5, "Operators", "/Root/IMS/BuiltIn/Portal/Operators");
            //InstallNode(12, 12, 3, 5, "Startup", "/Root/IMS/BuiltIn/Portal/Startup");
        }
    }
}
