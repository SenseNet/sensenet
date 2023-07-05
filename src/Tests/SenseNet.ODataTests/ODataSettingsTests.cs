﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Diagnostics;
using SenseNet.OData;
using SenseNet.ODataTests.Responses;
using SenseNet.Security;
using SenseNet.Testing;
using Settings = SenseNet.ContentRepository.Settings;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataSettingsTests : ODataTestBase
{
    #region Infrastructure
    private readonly CancellationToken _cancel = new CancellationTokenSource().Token;

    private async Task AddToLocalGroup(Workspace workspace, IUser user, string groupName, CancellationToken cancel)
    {
        var group = await EnsureLocalGroupAsync(workspace, groupName, cancel);
        group.AddMember(user);
    }
    private async Task<Group> EnsureLocalGroupAsync(Workspace workspace, string groupName, CancellationToken cancel)
    {
        var group = await Node.LoadAsync<Group>($"{workspace.Path}/Groups/{groupName}", cancel).ConfigureAwait(false);
        if (group == null)
        {
            var groupsNode = await EnsureNodeAsync($"{workspace.Path}/Groups", cancel).ConfigureAwait(false);
            var groupNode = new Group(groupsNode) { Name = groupName };
            await groupNode.SaveAsync(cancel).ConfigureAwait(false);
            group = groupNode;
        }
        return group;
    }
    private Task<Settings> EnsureSettingsAsync(string contentPath, string name, object settingValues, CancellationToken cancel)
    {
        return EnsureSettingsAsync(contentPath, name, JsonConvert.SerializeObject(settingValues), cancel);
    }
    private async Task<Settings> EnsureSettingsAsync(string contentPath, string name, string json, CancellationToken cancel)
    {
        var settingsFolder = await EnsureNodeAsync(contentPath + "/Settings", cancel);
        var settings = new Settings(settingsFolder) { Name = name + ".settings" };
        settings.Binary.SetStream(RepositoryTools.GetStreamFromString(json));
        await settings.SaveAsync(cancel);
        return settings;
    }

    private async Task<Node> EnsureNodeAsync(string path, CancellationToken cancel)
    {
        var node = await Node.LoadNodeAsync(path, cancel).ConfigureAwait(false);
        if (node != null)
            return node;

        var parentPath = RepositoryPath.GetParentPath(path);
        var name = RepositoryPath.GetFileName(path);
        var parent = await EnsureNodeAsync(parentPath, cancel).ConfigureAwait(false);

        node = name.ToLowerInvariant() is "settings" or "groups" ? new SystemFolder(parent) : new Folder(parent);
        node.Name = name;
        await node.SaveAsync(cancel).ConfigureAwait(false);

        return node;
    }

    private class SettingsData1 : IEquatable<SettingsData1>
    {
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }

        public bool Equals(SettingsData1 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return P1 == other.P1 && P2 == other.P2 && P3 == other.P3;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SettingsData1) obj);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(P1, P2, P3);
        }

        public override string ToString()
        {
            return $"P1={P1},P2={P2},P3={P3}";
        }
    }

    protected override void InitializeTest()
    {
        base.InitializeTest();
        if (Providers.Instance?.NodeObservers != null)
        {
            SettingsCache.Reset();
            Providers.Instance = null;
        }
    }

    #endregion

    [TestMethod]
    public async Task OD_Settings_Strict_Effective()
    {
        async Task<List<string>> GetResponsesAsync(IUser user, List<(string Url, string Expectation)> controlData)
        {
            var responses = new List<string>();
            using (new CurrentUserBlock(user))
            {
                foreach (var item in controlData)
                {
                    var response = await ODataGetAsync(item.Url, "?name=Settings1").ConfigureAwait(false);
                    responses.Add(response.Result.Replace("\r", "").Replace("\n", "")
                        .Replace(" ", "").Replace("\"", ""));
                }
            }

            return responses;
        }

        await ODataTestAsync(async () =>
        {
            var user1 = new User(User.Administrator.Parent) { Name = "U1" };
            await user1.SaveAsync(_cancel);
            Group.Administrators.AddMember(user1);

            var ws0 = await Node.LoadAsync<Workspace>("/Root/Content", _cancel).ConfigureAwait(false);
            var ws1 = new Workspace(ws0) { Name = "WS1" };
            await ws1.SaveAsync(_cancel).ConfigureAwait(false);
            var ws2 = new Workspace(ws1) { Name = "WS2" };
            await ws2.SaveAsync(_cancel).ConfigureAwait(false);
            var ws3 = new Workspace(ws2) { Name = "WS3" };
            await ws3.SaveAsync(_cancel).ConfigureAwait(false);

            List<(string Url, string Expectation)> controlData;
            List<string> responses;

            // ================ TESTS-1

            // -------- Visible Global settings
            var settings0 = @"{A:""root""}";
            var globalSettingsNode = await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            // -------- Visible local settings on /Root/Content
            var settings1 = @"{B:""content""}";
            await EnsureSettingsAsync("/Root/Content", "Settings1", settings1, _cancel)
                .ConfigureAwait(false);

            // -------- Visible local settings on /Root/Content/WS1
            var settings2 = @"{C:""WS1""}";
            await EnsureSettingsAsync("/Root/Content/WS1", "Settings1", settings2, _cancel)
                .ConfigureAwait(false);

            // -------- Visible local settings on /Root/Content/WS1/WS2
            var settings3 = @"{D:""WS2""}";
            await EnsureSettingsAsync("/Root/Content/WS1/WS2", "Settings1", settings3, _cancel)
                .ConfigureAwait(false);

            controlData = new List<(string Url, string Expectation)>
            {
                new("/OData.svc/('Root')/GetSettings",                     "{A:root}"),                        // accessible
                new("/OData.svc/Root/('Content')/GetSettings",             "{A:root,B:content}"),              // accessible
                new("/OData.svc/Root/Content/('WS1')/GetSettings",         "{A:root,B:content,C:WS1}"),        // accessible
                new("/OData.svc/Root/Content/WS1/('WS2')/GetSettings",     "{A:root,B:content,C:WS1,D:WS2}"),  // accessible
                new("/OData.svc/Root/Content/WS1/WS2/('WS3')/GetSettings", "{A:root,B:content,C:WS1,D:WS2}"),  // accessible
            };

            // ACT-1
            responses = await GetResponsesAsync(user1, controlData);

            // ASSERT-1
            for (int i = 0; i < responses.Count; i++)
                Assert.AreEqual(controlData[i].Expectation, responses[i], $"url:{controlData[i].Url}");

            // ================ TESTS-2

            // -------- Make global settings invisible (Open is denied or not defined)
            Group.Administrators.RemoveMember(user1);
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(globalSettingsNode.Id, user1.Id, false, PermissionType.See)
                .ApplyAsync(_cancel);

            // -------- Make local settings visible on /Root/Content
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(Identifiers.PortalRootId, user1.Id, false, PermissionType.Open)
                .Allow(ws0.Id, user1.Id, false, PermissionType.Open)
                .ApplyAsync(_cancel);
            var _ = await EnsureLocalGroupAsync(ws0, "Settings1Readers", _cancel).ConfigureAwait(false);
            await AddToLocalGroup(ws0, user1, "Settings1Readers", _cancel).ConfigureAwait(false);

            // -------- Make local settings invisible on /Root/Content/WS1
            var __ = await EnsureLocalGroupAsync(ws1, "Settings1Readers", _cancel).ConfigureAwait(false);

            // -------- Make local settings visible on /Root/Content/WS1/WS2
            var ___ = await EnsureLocalGroupAsync(ws2, "Settings1Readers", _cancel).ConfigureAwait(false);
            await AddToLocalGroup(ws2, user1, "Settings1Readers", _cancel).ConfigureAwait(false);

            controlData = new List<(string Url, string Expectation)>
            {
                new("/OData.svc/('Root')/GetSettings",                     "{}"),                 // not accessible
                new("/OData.svc/Root/('Content')/GetSettings",             "{B:content}"),        // accessible
                new("/OData.svc/Root/Content/('WS1')/GetSettings",         "{B:content}"),        // not accessible
                new("/OData.svc/Root/Content/WS1/('WS2')/GetSettings",     "{B:content,D:WS2}"),  // accessible
                new("/OData.svc/Root/Content/WS1/WS2/('WS3')/GetSettings", "{B:content,D:WS2}"),  // accessible
                // Note that the the local settings on WS3 is visible to the user1 because there is no local readers group
                // and she is a member of the parent workspace's readers group.
            };

            // ACT-2
            responses = await GetResponsesAsync(user1, controlData);

            // ASSERT-2
            for (int i = 0; i < responses.Count; i++)
                Assert.AreEqual(controlData[i].Expectation, responses[i], $"url:{controlData[i].Url}");

            // ================ TESTS-3

            // -------- Re-enable global settings
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(globalSettingsNode.Id, user1.Id, false, PermissionType.Open)
                .ApplyAsync(_cancel);

            // -------- TESTS-3
            controlData = new List<(string Url, string Expectation)>
            {
                new("/OData.svc/('Root')/GetSettings",                     "{A:root}"),                  // accessible
                new("/OData.svc/Root/('Content')/GetSettings",             "{A:root,B:content}"),        // accessible
                new("/OData.svc/Root/Content/('WS1')/GetSettings",         "{A:root,B:content}"),        // not accessible
                new("/OData.svc/Root/Content/WS1/('WS2')/GetSettings",     "{A:root,B:content,D:WS2}"),  // accessible
                new("/OData.svc/Root/Content/WS1/WS2/('WS3')/GetSettings", "{A:root,B:content,D:WS2}"),  // accessible
            };

            // ACT-3
            responses = await GetResponsesAsync(user1, controlData);

            // ASSERT-3
            for (int i = 0; i < responses.Count; i++)
                Assert.AreEqual(controlData[i].Expectation, responses[i], $"url:{controlData[i].Url}");
        });
    }

    [TestMethod]
    public async Task OD_Settings_GetSettings_Cache()
    {
        await ODataTestAsync(async () =>
        {
            var folder3 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2/Folder3", _cancel).ConfigureAwait(false);
            List<(string Url, string Expectation)> controlData;
            List<string> responses;

            var settings0 = "{P:{A:1}}";
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            // -------- ADD LOCAL SETTINGS to /Root/Content/Folder1
            var settings1 = "{P:{A:11,B:2}}";
            await EnsureSettingsAsync("/Root/Content/Folder1", "Settings1", settings1, _cancel)
                .ConfigureAwait(false);

            controlData = new List<(string Url, string Expectation)>
            {
                // Access to a deeper first
                new("/OData.svc/Root/Content/('Folder1')/GetSettings",                "{P:{A:11,B:2}}"),
                new("/OData.svc/('Root')/GetSettings",                                "{P:{A:1}}"),
                new("/OData.svc/Root/('Content')/GetSettings",                        "{P:{A:1}}"),
                new("/OData.svc/Root/Content/Folder1/Folder2('Folder3')/GetSettings", "{P:{A:11,B:2}}"),
                new("/OData.svc/Root/(Localization)/GetSettings",                     ""),
            };
            // ACT 1..N
            responses = new List<string>();
            foreach (var item in controlData)
            {
                var response = await ODataGetAsync(item.Url, "?name=Settings1").ConfigureAwait(false);
                responses.Add(response.Result.Replace("\r", "").Replace("\n", "")
                    .Replace(" ", "").Replace("\"", ""));
            }
            // ASSERT 1..N
            for (int i = 0; i < responses.Count; i++)
                Assert.AreEqual(controlData[i].Expectation, responses[i], $"url:{controlData[i].Url}");

            // -------- ADD LOCAL SETTINGS to /Root/Content/Folder1/Folder2
            var settings2 = "{P:{A:111,B:22,C:3}}";
            await EnsureSettingsAsync("/Root/Content/Folder1/Folder2", "Settings1", settings2, _cancel)
                .ConfigureAwait(false);

            controlData = new List<(string Url, string Expectation)>
            {
                // Access to a deeper first
                new("/OData.svc/Root/Content/Folder1/Folder2('Folder3')/GetSettings", "{P:{A:111,B:22,C:3}}"),
                new("/OData.svc/('Root')/GetSettings",                                "{P:{A:1}}"),
                new("/OData.svc/Root/('Content')/GetSettings",                        "{P:{A:1}}"),
                new("/OData.svc/Root/Content/('Folder1')/GetSettings",                "{P:{A:11,B:2}}"),
                new("/OData.svc/Root/(Localization)/GetSettings",                     ""),
            };
            // ACT 1..N
            responses = new List<string>();
            foreach (var item in controlData)
            {
                var response = await ODataGetAsync(item.Url, "?name=Settings1").ConfigureAwait(false);
                responses.Add(response.Result.Replace("\r", "").Replace("\n", "")
                    .Replace(" ", "").Replace("\"", ""));
            }
            // ASSERT 1..N
            for (int i = 0; i < responses.Count; i++)
                Assert.AreEqual(controlData[i].Expectation, responses[i], $"url:{controlData[i].Url}");

        }).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task OD_Settings_GetSettings_Root()
    {
        await ODataTestAsync(async () =>
        {
            var settings0 = new SettingsData1 { P1 = "V1"};
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            // ACT
            var response = await ODataGetAsync(
                    $"/OData.svc/('Root')/GetSettings", "?name=Settings1")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.AreEqual(settings0, JsonConvert.DeserializeObject<SettingsData1>(response.Result));

        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_Settings_GetSettings()
    {
        await ODataTestAsync(async () =>
        {
            var settings0 = new SettingsData1 { P1 = "V1" };
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            var settings1 = new SettingsData1 { P1 = "V11", P3 = "V3" };
            await EnsureSettingsAsync("/Root/Content/Folder1", "Settings1", settings1, _cancel)
                .ConfigureAwait(false);

            var settings2 = new SettingsData1 { P2 = "V2", P3 = "V33" };
            await EnsureSettingsAsync("/Root/Content/Folder1/Folder2", "Settings1", settings2, _cancel)
                .ConfigureAwait(false);

            var folder3 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2/Folder3", _cancel).ConfigureAwait(false);
            var folder1 = await Node.LoadNodeAsync("/Root/Content/Folder1", _cancel).ConfigureAwait(false);
            var contents = await Node.LoadNodeAsync("/Root/Content", _cancel).ConfigureAwait(false);

            // ACT
            ODataResponse response0;
            response0 = await ODataGetAsync(
                    $"/OData.svc/Root('System')/GetSettings", "?name=Settings1")
                .ConfigureAwait(false);

            ODataResponse response1;
            response1 = await ODataGetAsync(
                    $"/OData.svc/Root/Content('Folder1')/GetSettings", "?name=Settings1")
                .ConfigureAwait(false);

            ODataResponse response2;
            response2 = await ODataGetAsync(
                    $"/OData.svc/Root/Content/Folder1/Folder2('Folder3')/GetSettings", "?name=Settings1")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response0);
            Assert.AreEqual(settings0, JsonConvert.DeserializeObject<SettingsData1>(response0.Result));

            AssertNoError(response1);
            Assert.AreEqual(settings1, JsonConvert.DeserializeObject<SettingsData1>(response1.Result));

            AssertNoError(response2);
            Assert.AreEqual(
                new SettingsData1 { P1 = "V11", P2 = "V2", P3 = "V33" },
                JsonConvert.DeserializeObject<SettingsData1>(response2.Result));

        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_Settings_GetSettings_Properties()
    {
        await ODataTestAsync(async () =>
        {
            var settings0 = new SettingsData1 { P1 = "V1" };
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            var settings1 = new SettingsData1 { P1 = "V11", P3 = "V3" };
            await EnsureSettingsAsync("/Root/Content/Folder1", "Settings1", settings1, _cancel)
                .ConfigureAwait(false);

            var settings2 = new SettingsData1 { P2 = "V2", P3 = "V33" };
            await EnsureSettingsAsync("/Root/Content/Folder1/Folder2", "Settings1", settings2, _cancel)
                .ConfigureAwait(false);

            var folder3 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2/Folder3", _cancel).ConfigureAwait(false);
            var folder1 = await Node.LoadNodeAsync("/Root/Content/Folder1", _cancel).ConfigureAwait(false);
            var contents = await Node.LoadNodeAsync("/Root/Content", _cancel).ConfigureAwait(false);

            // ACT
            var resourceUrl = "/OData.svc/Root/Content/Folder1/Folder2('Folder3')/GetSettings";
            var responseP1 = await ODataGetAsync(resourceUrl, "?name=Settings1&property=P1").ConfigureAwait(false);
            var responseP2 = await ODataGetAsync(resourceUrl, "?name=Settings1&property=P2").ConfigureAwait(false);
            var responseP3 = await ODataGetAsync(resourceUrl, "?name=Settings1&property=P3").ConfigureAwait(false);
            var responseP4 = await ODataGetAsync(resourceUrl, "?name=Settings1&property=P4").ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("\"V11\"", responseP1.Result);
            Assert.AreEqual("\"V2\"", responseP2.Result);
            Assert.AreEqual("\"V33\"", responseP3.Result);
            Assert.AreEqual(string.Empty, responseP4.Result);

        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OD_Settings_GetSettings_Admin_DenyOnGlobal()
    {
        await ODataTestAsync(async () =>
        {
            var user1 = new User(User.Administrator.Parent) { Name = "U1" };
            await user1.SaveAsync(_cancel);
            Group.Administrators.AddMember(user1);

            var settings0 = new SettingsData1 { P1 = "V1" };
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            var globalSettings = await Node.LoadNodeAsync("/Root/System/Settings/Settings1.settings", _cancel);
            Assert.IsTrue(globalSettings.Security.HasPermission(user1, PermissionType.Open));

            var folder2 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2", _cancel).ConfigureAwait(false);

            var settings1 = new SettingsData1 { P1 = "V11", P2 = "V2" };
            await EnsureSettingsAsync("/Root/Content/Folder1", "Settings1", settings1, _cancel)
                .ConfigureAwait(false);


            var resourceUrl = "/OData.svc/Root/Content/Folder1/('Folder2')/GetSettings";
            var queryString = "?name=Settings1";
            ODataResponse response;

            // Test-1: Get settings by an administrator 
            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);
            Assert.AreEqual(settings1, JsonConvert.DeserializeObject<SettingsData1>(response.Result));

            // Test-2: Get settings with Open permission on global
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(globalSettings.Id, user1.Id, true, PermissionType.OpenMinor)
                .ApplyAsync(_cancel);

            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);
            Assert.AreEqual(settings1, JsonConvert.DeserializeObject<SettingsData1>(response.Result));

            // Test-3: Get settings without Open permission on global
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(globalSettings.Id, user1.Id, true, PermissionType.Open)
                .ApplyAsync(_cancel);

            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);
            Assert.AreEqual(settings1, JsonConvert.DeserializeObject<SettingsData1>(response.Result));

        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_Settings_GetSettings_UserIsReader()
    {
        await ODataTestAsync(async () =>
        {
            // Create a new user.
            var user1 = new User(User.Administrator.Parent) { Name = "U1" };
            await user1.SaveAsync(_cancel);

            // Create a global settings
            var settings0 = new SettingsData1 { P1 = "V1", P2 = "V2" };
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            // Check that the user cannot see the global settings
            var globalSettings = await Node.LoadNodeAsync("/Root/System/Settings/Settings1.settings", _cancel);
            Assert.IsFalse(globalSettings.Security.HasPermission(user1, PermissionType.See));

            // Create a local settings in the workspace.
            var settings1 = new SettingsData1 { P1 = "V11", P3 = "V3" };
            await EnsureSettingsAsync("/Root/Content/Folder1", "Settings1", settings1, _cancel)
                .ConfigureAwait(false);

            // Create a deep folder.
            var folder2 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2", _cancel).ConfigureAwait(false);

            // Permit the user to open the deep folder
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(folder2.Id, user1.Id, true, PermissionType.Open)
                .ApplyAsync(_cancel);

            var resourceUrl = "/OData.svc/Root/Content/Folder1/('Folder2')/GetSettings";
            var queryString = "?name=Settings1";
            ODataResponse response;

            // ACT-1: The user gets the settings from the deep folder.
            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);

            // ASSERT-1: settings is empty because there is no readers group and
            // user has not enough permission on the global settings.
            Assert.AreEqual("{}", response.Result);

            /* --------------------------------- */

            // ALIGN-2: Permit Open on the global settings.
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(globalSettings.Id, user1.Id, false, PermissionType.Open)
                .ApplyAsync(_cancel);

            // ACT-2: The user gets the settings from the deep folder.
            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);

            // ASSERT-2: settings solved because there is no readers group and
            // user has enough permission on the global settings.
            Assert.AreEqual(new SettingsData1 { P1 = "V11", P2 = "V2", P3 = "V3" },
                JsonConvert.DeserializeObject<SettingsData1>(response.Result));

            /* --------------------------------- */

            // ALIGN-3: Create the settings readers group.
            var workspace = await Node.LoadAsync<Workspace>("/Root/Content", _cancel).ConfigureAwait(false);
            // Path: /Root/Content/Groups/Settings1Readers
            var group = await EnsureLocalGroupAsync(workspace, "Settings1Readers", _cancel).ConfigureAwait(false);
            // Check that the user is not a settings reader.
            Assert.IsFalse(user1.IsInGroup(group));

            // ACT-3: The user gets settings from the deep folder.
            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);

            // ASSERT-3: settings is the global because the user is not reader but the global is accessible.
            Assert.AreEqual(new SettingsData1 { P1 = "V1", P2 = "V2", P3 = null },
                JsonConvert.DeserializeObject<SettingsData1>(response.Result));

            /* --------------------------------- */

            // ALIGN-4: Add the user to the settings readers group.
            await AddToLocalGroup(workspace, user1, "Settings1Readers", _cancel).ConfigureAwait(false);
            // Check that the user is settings reader.
            group = await Node.LoadAsync<Group>("/Root/Content/Groups/Settings1Readers", _cancel).ConfigureAwait(false);
            Assert.IsTrue(user1.IsInGroup(group));

            // ------------- ACT-4: get settings from the deep folder.
            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);

            // ASSERT-4: settings solved
            Assert.AreEqual(new SettingsData1 { P1 = "V11", P2 = "V2", P3 = "V3" },
                JsonConvert.DeserializeObject<SettingsData1>(response.Result));

        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OD_Settings_WriteSettings()
    {
        await ODataTestAsync(async () =>
        {
            var settings0 = new SettingsData1 { P1 = "V1", P2 = "V2" };
            await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            await EnsureNodeAsync("/Root/Content/Folder1", _cancel).ConfigureAwait(false);

            // ACT
            var response = await ODataPostAsync(
                    $"/OData.svc/Root/Content('Folder1')/WriteSettings", null,
                    "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V11\",\"P3\":\"V3\"}}]")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.AreEqual(204, response.StatusCode);
            var loadedSettings = await Node.LoadAsync<Settings>("/Root/Content/Folder1/Settings/Settings1.settings", _cancel)
                .ConfigureAwait(false);
            var loadedJsonData = RepositoryTools.GetStreamString(loadedSettings.Binary.GetStream());
            Assert.AreEqual("{\"P1\":\"V11\",\"P3\":\"V3\"}", loadedJsonData);
        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_Settings_WriteSettings_UserIsEditor()
    {
        await ODataTestAsync(async () =>
        {
            // Create a new developer user (can execute the WriteSettings OData action).
            var user1 = new User(User.Administrator.Parent) { Name = "U1" };
            await user1.SaveAsync(_cancel);
            var developers = await Node.LoadAsync<Group>("/Root/IMS/BuiltIn/Portal/Editors", _cancel).ConfigureAwait(false);
            developers.AddMember(user1);

            // Create a global settings.
            var settings0 = new SettingsData1 { P1 = "V1", P2 = "V2" };
            var globalSettings = await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            // Create a local folder.
            var folder1 = await EnsureNodeAsync("/Root/Content/Folder1", _cancel).ConfigureAwait(false);

            // Permit the user to open the deep folder
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(folder1.Id, user1.Id, true, PermissionType.Open)
                .ApplyAsync(_cancel);

            var resourceUrl = "/OData.svc/Root/Content('Folder1')/WriteSettings";
            var requestBody = "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V11\",\"P3\":\"V3\"}}]";
            ODataResponse response;
            Settings loadedSettings;
            string loadedJsonData;

            // ACT-1
            using (new CurrentUserBlock(user1))
                response = await ODataPostAsync(resourceUrl, null, requestBody).ConfigureAwait(false);

            // ASSERT-1: There is no settings editors group
            var error = GetError(response);
            Assert.AreEqual("InvalidContentActionException", error.ExceptionType);
            Assert.AreEqual("Not enough permission for write local settings Settings1 " +
                            "for the requested path: /Root/Content/Folder1", error.Message);

            /* --------------------------------- */

            // ALIGN-2: Permit Save on the global settings.
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(globalSettings.Id, user1.Id, false, PermissionType.Save)
                .ApplyAsync(_cancel);

            // ACT-2
            using (new CurrentUserBlock(user1))
                response = await ODataPostAsync(resourceUrl, null, requestBody).ConfigureAwait(false);

            // ASSERT-2: There is no settings editors group
            AssertNoError(response);
            Assert.AreEqual(204, response.StatusCode);
            loadedSettings = await Node.LoadAsync<Settings>("/Root/Content/Folder1/Settings/Settings1.settings", _cancel)
                .ConfigureAwait(false);
            loadedJsonData = RepositoryTools.GetStreamString(loadedSettings.Binary.GetStream());
            Assert.AreEqual("{\"P1\":\"V11\",\"P3\":\"V3\"}", loadedJsonData);

            /* --------------------------------- */

            // ALIGN-3: Create the settings readers group.
            var workspace = await Node.LoadAsync<Workspace>("/Root/Content", _cancel).ConfigureAwait(false);
            // Path: /Root/Content/Groups/Settings1Editors
            var group = await EnsureLocalGroupAsync(workspace, "Settings1Editors", _cancel).ConfigureAwait(false);
            // Check that the user is not a settings reader.
            Assert.IsFalse(user1.IsInGroup(group));

            // ACT-3
            using (new CurrentUserBlock(user1))
                response = await ODataPostAsync(resourceUrl, null, requestBody).ConfigureAwait(false);

            // ASSERT-3: The user is not a settings editor.
            error = GetError(response);
            Assert.AreEqual("InvalidContentActionException", error.ExceptionType);
            Assert.AreEqual("Not enough permission for write local settings Settings1 " +
                            "for the requested path: /Root/Content/Folder1", error.Message);

            /* --------------------------------- */

            // ALIGN-4: Add the user to the settings editors group.
            await AddToLocalGroup(workspace, user1, "Settings1Editors", _cancel).ConfigureAwait(false);
            // Check that the user is settings editor.
            group = await Node.LoadAsync<Group>("/Root/Content/Groups/Settings1Editors", _cancel).ConfigureAwait(false);
            Assert.IsTrue(user1.IsInGroup(group));

            // ACT-4
            using (new CurrentUserBlock(user1))
                response = await ODataPostAsync(resourceUrl, null, requestBody).ConfigureAwait(false);

            // ASSERT-4: The settings is writen.
            AssertNoError(response);
            Assert.AreEqual(204, response.StatusCode);
            loadedSettings = await Node.LoadAsync<Settings>("/Root/Content/Folder1/Settings/Settings1.settings", _cancel)
                .ConfigureAwait(false);
            loadedJsonData = RepositoryTools.GetStreamString(loadedSettings.Binary.GetStream());
            Assert.AreEqual("{\"P1\":\"V11\",\"P3\":\"V3\"}", loadedJsonData);

        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_Settings_WriteSettings_Error_NotWorkspace()
    {
        await ODataTestAsync(async () =>
        {
            var requestBody = "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V1\",\"P2\":\"V2\"}}]";
            var expectedErrorType = "InvalidOperationException";
            var expectedMessage = "Local settings cannot be written outside a workspace.";
            ODataResponse response;
            ODataErrorResponse error;

            // ACT-1
            response = await ODataPostAsync($"/OData.svc/('Root')/WriteSettings", null, requestBody)
                .ConfigureAwait(false);
            // ASSERT-1
            error = GetError(response);
            Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
            Assert.AreEqual(expectedErrorType, error.ExceptionType);
            Assert.AreEqual(expectedMessage, error.Message);

            // ACT-2
            response = await ODataPostAsync($"/OData.svc/Root('System')/WriteSettings", null, requestBody)
                .ConfigureAwait(false);
            // ASSERT-2
            error = GetError(response);
            Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
            Assert.AreEqual(expectedErrorType, error.ExceptionType);
            Assert.AreEqual(expectedMessage, error.Message);

            // ACT-2
            response = await ODataPostAsync($"/OData.svc/Root/System('Schema')/WriteSettings", null, requestBody)
                .ConfigureAwait(false);
            // ASSERT-2
            error = GetError(response);
            Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
            Assert.AreEqual(expectedErrorType, error.ExceptionType);
            Assert.AreEqual(expectedMessage, error.Message);
        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_Settings_WriteSettings_Error_MissingGlobal()
    {
        await ODataTestAsync(async () =>
        {
            var folder3 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2/Folder3", _cancel).ConfigureAwait(false);

            // ACT
            var response = await ODataPostAsync(
                    $"/OData.svc/Root/Content('Folder1')/WriteSettings", null,
                    "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V1\",\"P2\":\"V2\"}}]")
                .ConfigureAwait(false);

            // ASSERT
            var error = GetError(response);
            Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
            Assert.AreEqual("InvalidOperationException", error.ExceptionType);
            Assert.AreEqual("Cannot write local settings Settings1 if it is not created " +
                            "in the the global settings folder (/Root/System/Settings)", error.Message);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OD_Settings_WriteSettings_Update_RemoveProperty()
    {
        await ODataTestAsync(async () =>
        {
            var settings0 = new {P1 = "V1"}; // SettingsData1 { P1 = "V1" };
            var globalSettings = await EnsureSettingsAsync("/Root/System", "Settings1", settings0, _cancel)
                .ConfigureAwait(false);

            await EnsureNodeAsync("/Root/Content/Folder1", _cancel).ConfigureAwait(false);

            // create local
            var creationResponse = await ODataPostAsync(
                    $"/OData.svc/Root/Content('Folder1')/WriteSettings", null,
                    "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V11\",\"P2\":\"V2\"}}]")
                .ConfigureAwait(false);
            // reload global to cache it
            globalSettings = Node.Load<Settings>(globalSettings.Id);
            // check local via OData
            AssertNoError(creationResponse);
            var gettingResponse = await ODataGetAsync(
                $"/OData.svc/Root/Content('Folder1')/GetSettings", "?name=Settings1").ConfigureAwait(false);
            Assert.AreEqual("{\"P1\":\"V11\",\"P2\":\"V2\"}", gettingResponse.Result
                .Replace(" ", "").Replace("\r", "").Replace("\n", ""));
            Assert.AreEqual(204, creationResponse.StatusCode);
            // check local via Node
            var loadedSettings1 = await Node.LoadAsync<Settings>("/Root/Content/Folder1/Settings/Settings1.settings", _cancel)
                .ConfigureAwait(false);
            var loadedJsonData1 = RepositoryTools.GetStreamString(loadedSettings1.Binary.GetStream());
            Assert.AreEqual("{\"P1\":\"V11\",\"P2\":\"V2\"}", loadedJsonData1);

            // ACT: Update P1 and remove P2
            var updatingResponse = await ODataPostAsync(
                    $"/OData.svc/Root/Content('Folder1')/WriteSettings", null,
                    "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V12\"}}]")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(updatingResponse);
            Assert.AreEqual(204, updatingResponse.StatusCode);
            var loadedSettings2 = await Node.LoadAsync<Settings>("/Root/Content/Folder1/Settings/Settings1.settings", _cancel)
                .ConfigureAwait(false);
            var loadedJsonData2 = RepositoryTools.GetStreamString(loadedSettings2.Binary.GetStream());
            Assert.AreEqual("{\"P1\":\"V12\"}", loadedJsonData2);
            // get settings
            gettingResponse = await ODataGetAsync(
                    $"/OData.svc/Root/Content('Folder1')/GetSettings", "?name=Settings1").ConfigureAwait(false);
            Assert.AreEqual("{\"P1\":\"V12\"}", gettingResponse.Result
                .Replace(" ", "").Replace("\r", "").Replace("\n", ""));

        }).ConfigureAwait(false);
    }

}