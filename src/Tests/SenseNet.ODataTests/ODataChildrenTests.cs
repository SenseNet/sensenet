﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.OData;
using SenseNet.ODataTests.Responses;
using SenseNet.Portal;
using SenseNet.Search;
using File = SenseNet.ContentRepository.File;
using Task = System.Threading.Tasks.Task;
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataChildrenTests : ODataTestBase
    {
        #region Infrastructure
        private async Task ODataChildrenTest(Func<Task> callback)
        {
            await ODataTestAsync(async () =>
            {
                EnsureTestStructure();
                await callback().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private void EnsureTestStructure()
        {
            //  root
            //      WSRoot
            //          Workspace1
            //              F0
            //                  F00
            //                      F000
            //                  SF01(SYSTEM)
            //              SF1(SYSTEM)
            //                  F10

            Cache.Reset();

            var testRoot = Node.Load<Folder>("/Root/WSRoot");
            if (testRoot != null)
                return;
            testRoot = new Folder(Repository.Root) { Name = "WSRoot" };
            testRoot.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var testFolder1 = new Workspace(testRoot) { Name = "Workspace1" };
            testFolder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var folder0 = new Folder(testFolder1) { Name = "F0" };
            folder0.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var folder00 = new Folder(folder0) { Name = "F00" };
            folder00.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var folder000 = new Folder(folder00) { Name = "F000" };
            folder000.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var systemFolder01 = new SystemFolder(folder0) { Name = "SF01" };
            systemFolder01.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var systemFolder1 = new SystemFolder(testFolder1) { Name = "SF1" };
            systemFolder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var folder10 = new Folder(systemFolder1) { Name = "F10" };
            folder10.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        #endregion

        [TestMethod]
        public async Task OD_GET_Children_Entity_SelectChildren_NoExpand()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')",
                    "?metadata=no&$select=Id,Name,Children")
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                // ODataReference will be serialized as "__deferred"
                var propertyValue = (JObject)entity.AllProperties["Children"];
                var children = ODataEntityResponse.Create(propertyValue);
                Assert.IsTrue(children.IsDeferred);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Entity_SelectChildren_Expand()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')",
                    "?metadata=no&$select=Id,Name,Children&$expand=Children")
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                var children = entity.Children;
                Assert.AreEqual(2, children.Length);
                Assert.IsTrue(children[0].AllPropertiesSelected);
                Assert.AreEqual("F0", children[0].Name);
                Assert.AreEqual("SystemFolder", children[1].ContentType.Name);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Entity_SelectChildren_ExpandAndSelect()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')",
                    "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children")
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                var children = entity.Children;
                Assert.AreEqual(2, children.Length);
                Assert.AreEqual(2, children[0].AllProperties.Count);
                Assert.AreEqual("/Root/WSRoot/Workspace1/F0", children[0].Path);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Entity_SelectChildren_Filtered()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION: switch on autofilters
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')",
                    "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children&enableautofilters=true")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                var children = entity.Children;
                Assert.IsNotNull(children);
                Assert.AreEqual(1, children.Length);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Children_Property_Expand()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')/Children",
                    "?metadata=no&$select=Id,Name,Children&$expand=Children")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                Assert.AreEqual(2, entities.Length);
                var f0 = entities.FirstOrDefault(e => e.Name == "F0");
                Assert.IsNotNull(f0);
                Assert.AreEqual(3, f0.AllProperties.Count);
                var f0Children = f0.Children;
                Assert.IsNotNull(f0Children);
                Assert.AreEqual(2, f0Children.Length);
                Assert.IsTrue(f0Children[0].AllPropertiesSelected);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Property_ExpandAndSelect()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')/Children",
                    "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                Assert.AreEqual(2, entities.Length);
                var f0 = entities.FirstOrDefault(e => e.Name == "F0");
                Assert.IsNotNull(f0);
                Assert.AreEqual(3, f0.AllProperties.Count);
                Assert.AreEqual(2, f0.Children.Length);
                var f00Path = $"/Root/WSRoot/Workspace1/F0/F00";
                var f00Node = Node.Load<Folder>(f00Path);
                var f00 = f0.Children.FirstOrDefault(e => e.Id == f00Node.Id);
                Assert.IsNotNull(f00);
                Assert.AreEqual(null, f00.Name); // Name is not selected
                Assert.AreEqual(f00Path, f00.Path);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Property_ExpandAndSelect_Deep()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')/Children",
                    "?metadata=no&$select=Id,Name,Children/Id,Children/Path,Children/Children/Id,Children/Children/Path&$expand=Children,Children/Children")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                Assert.AreEqual(2, entities.Length);
                var f00Path = $"/Root/WSRoot/Workspace1/F0/F00";
                var f000Path = $"/Root/WSRoot/Workspace1/F0/F00/F000";
                var f00Node = Node.Load<Folder>(f00Path);
                var f000Node = Node.Load<Folder>(f000Path);
                var f000 = entities
                    .First(e => e.Name == "F0").Children
                    .First(e => e.Id == f00Node.Id).Children
                    .First(e => e.Id == f000Node.Id);
                Assert.IsNotNull(f000);
                Assert.AreEqual(2, f000.AllProperties.Count);
                Assert.IsNull(f000.Children); // Children property is not selected on the 3rd level
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Property_Filtered()
        {
            await ODataChildrenTest(async () =>
            {
                // ACTION-1: switch on autofilters
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')/Children",
                    "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children&enableautofilters=true")
                    .ConfigureAwait(false);

                // ASSERT-1
                var entities = GetEntities(response);
                Assert.AreEqual(1, entities.Length);

                // ACTION-2: add a query filter
                response = await ODataGetAsync(
                    $"/OData.svc/Root/WSRoot('Workspace1')/Children",
                    "?metadata=no&$select=Id,Name&$filter=startswith(Name, 'SF') eq true")
                    .ConfigureAwait(false);

                // ASSERT-2
                entities = GetEntities(response);
                Assert.AreEqual(1, entities.Length);
                Assert.IsTrue(entities[0].Name.StartsWith("SF"));
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Children_Binary_Expand_RejectedByCharacter()
        {
            await ODataChildrenTest(async () =>
            {
                var contentName = "non-text.settings";
                WriteNonTextSettings(contentName, new byte[] {0x1F, 0x20});

                var expectedTexts = GetTextContents();

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root/System/Settings",
                        "?metadata=no&$select=Id,Name,Binary&$expand=Binary")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                var messages = entities.ToDictionary(x => x.Name,
                    x => ((JObject) x.AllProperties["Binary"])["Message"]?.ToString() ?? "null");
                foreach (var name in messages.Keys)
                {
                    if (name == contentName)
                        Assert.IsTrue(messages[contentName].StartsWith("Not a text file."));
                    else
                        Assert.AreEqual(name + ":", name + ":" + messages[name]);
                }
                var texts = entities.ToDictionary(x => x.Name, x => ((JObject)x.AllProperties["Binary"])["Text"]?.ToString() ?? "null");
                foreach (var name in expectedTexts.Keys)
                {
                    if (name == contentName)
                        Assert.IsTrue(texts[contentName] == string.Empty);
                    else
                        Assert.AreEqual(name + ":" + expectedTexts[name], name + ":" + texts[name]);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod, TestCategory("Services")]
        public async Task OD_GET_Children_Binary_Expand_EnabledByExtension_CSrv()
        {
            await ODataChildrenTest(async () =>
            {
                WriteTextFileSettings(@"{ ""MaxExpandableSize"": 100000,  ""Extensions"": [ ""md"", ""txt"", ""js"", ""settings"" ] }");
                var expectedTexts = GetTextContents();

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root/System/Settings",
                        "?metadata=no&$select=Id,Name,Binary&$expand=Binary")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                var messages = entities.ToDictionary(x => x.Name, x => ((JObject)x.AllProperties["Binary"])["Message"]?.ToString() ?? "null");
                foreach (var name in messages.Keys)
                    Assert.AreEqual(name + ":", name + ":" + messages[name]);
                var texts = entities.ToDictionary(x => x.Name, x => ((JObject)x.AllProperties["Binary"])["Text"]?.ToString() ?? "null");
                foreach (var name in expectedTexts.Keys)
                    Assert.AreEqual(name + ":" + expectedTexts[name], name + ":" + texts[name]);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Binary_Expand_RejectedByExtension()
        {
            await ODataChildrenTest(async () =>
            {
                WriteTextFileSettings(@"{ ""MaxExpandableSize"": 100000,  ""Extensions"": [ ""md"", ""txt"", ""js"" ] }");

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root/System/Settings",
                        "?metadata=no&$select=Id,Name,Binary&$expand=Binary")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                var messages = entities.ToDictionary(x => x.Name, x => ((JObject)x.AllProperties["Binary"])["Message"]?.ToString() ?? "null");
                Assert.IsTrue(messages.Any(x=>x.Value.Contains("not a text file", StringComparison.OrdinalIgnoreCase)));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Children_Binary_Expand_RejectedBySize()
        {
            await ODataChildrenTest(async () =>
            {
                WriteTextFileSettings(@"{ ""MaxExpandableSize"": 10,  ""Extensions"": [ ""md"", ""txt"", ""js"", ""settings"" ] }");

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root/System/Settings",
                        "?metadata=no&$select=Id,Name,Binary&$expand=Binary")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                var messages = entities.ToDictionary(x => x.Name, x => ((JObject)x.AllProperties["Binary"])["Message"]?.ToString() ?? "null");
                Assert.IsTrue(messages.Any(x => x.Value.StartsWith("Size limit exceed.")));
            }).ConfigureAwait(false);
        }

        private void WriteNonTextSettings(string name, byte[] buffer)
        {
            var settings = new SenseNet.ContentRepository.Settings(Node.LoadNode(Repository.SettingsFolderPath))
            {
                Name = name
            };
            settings.Binary.ContentType = "application/octet-stream";
            settings.Binary.SetStream(new MemoryStream(buffer));
            settings.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private void WriteTextFileSettings(string settingsJson)
        {
            var settings = new SenseNet.ContentRepository.Settings(Node.LoadNode(Repository.SettingsFolderPath))
            {
                Name = "TextFiles.settings"
            };
            settings.Binary.SetStream(RepositoryTools.GetStreamFromString(settingsJson));
            settings.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        private Dictionary<string, string> GetTextContents()
        {
            var settingContent = Content.Load(Repository.SettingsFolderPath);
            settingContent.ChildrenDefinition.EnableAutofilters = FilterStatus.Disabled;
            var settingContents = settingContent.Children.ToArray();
            return settingContents
                .ToDictionary(
                    x => x.Name,
                    x => RepositoryTools.GetStreamString(((File)x.ContentHandler).Binary.GetStream()));
        }
    }
}
