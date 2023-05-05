using System;
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
using SenseNet.Security;
using Settings = SenseNet.ContentRepository.Settings;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataSettingsTests : ODataTestBase
{
    #region Infrastructure
    [ContentHandler]
    private class Settings1 : Settings
    {
        public static readonly string Ctd = $@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{nameof(Settings1)}' parentType='Settings' handler='{typeof(Settings1).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <Fields/>
</ContentType>";

        public Settings1(Node parent) : base(parent) { }
        public Settings1(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Settings1(NodeToken nt) : base(nt) { }
    }

    private readonly CancellationToken _cancel = new CancellationTokenSource().Token;

    private async Task<T> EnsureSettingsAsync<T>(string contentPath, object settingValues, CancellationToken cancel) where T : Settings
    {
        var settingsFolder = await EnsureNodeAsync(contentPath + "/Settings", cancel);
        var settings = CreateSettings<T>(settingsFolder);
        var json = JsonConvert.SerializeObject(settingValues);
        settings.Binary.SetStream(RepositoryTools.GetStreamFromString(json));
        await settings.SaveAsync(cancel);
        return settings;
    }
    T CreateSettings<T>(Node parent) where T : Settings
    {
        var constructor = typeof(T).GetConstructor(new[] { typeof(Node) });
        if (constructor == null)
            throw new InvalidOperationException("Type " + typeof(T).Name + " does not contain an appropriate constructor");
        var settings = (T)constructor.Invoke(new object[] { parent });
        settings.Name = typeof(T).Name + ".settings";
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

        node = name.ToLowerInvariant() == "settings" ? new SystemFolder(parent) : new Folder(parent);
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
    #endregion

    [TestMethod]
    public async Task OD_Settings_GetSettings_Root()
    {
        await ODataTestAsync(async () =>
        {
            ContentTypeInstaller.InstallContentType(Settings1.Ctd);

            var settings0 = new SettingsData1 { P1 = "V1"};
            await EnsureSettingsAsync<Settings1>("/Root/System", settings0, _cancel)
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
            ContentTypeInstaller.InstallContentType(Settings1.Ctd);

            var settings0 = new SettingsData1 { P1 = "V1" };
            await EnsureSettingsAsync<Settings1>("/Root/System", settings0, _cancel)
                .ConfigureAwait(false);

            var settings1 = new SettingsData1 { P1 = "V11", P3 = "V3" };
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1", settings1, _cancel)
                .ConfigureAwait(false);

            var settings2 = new SettingsData1 { P2 = "V2", P3 = "V33" };
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1/Folder2", settings2, _cancel)
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
            ContentTypeInstaller.InstallContentType(Settings1.Ctd);

            var settings0 = new SettingsData1 { P1 = "V1" };
            await EnsureSettingsAsync<Settings1>("/Root/System", settings0, _cancel)
                .ConfigureAwait(false);

            var settings1 = new SettingsData1 { P1 = "V11", P3 = "V3" };
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1", settings1, _cancel)
                .ConfigureAwait(false);

            var settings2 = new SettingsData1 { P2 = "V2", P3 = "V33" };
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1/Folder2", settings2, _cancel)
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
    public async Task OD_Settings_GetSettings_Permissions()
    {
        await ODataTestAsync(async () =>
        {
            var user1 = new User(User.Administrator.Parent) { Name = "U1" };
            await user1.SaveAsync(_cancel);
            Group.Administrators.AddMember(user1);

            ContentTypeInstaller.InstallContentType(Settings1.Ctd);

            var settings0 = new SettingsData1 { P1 = "V1" };
            await EnsureSettingsAsync<Settings1>("/Root/System", settings0, _cancel)
                .ConfigureAwait(false);

            var globalSettings = await Node.LoadNodeAsync("/Root/System/Settings/Settings1.settings", _cancel);
            Assert.IsTrue(globalSettings.Security.HasPermission(user1, PermissionType.Open));

            var settings1 = new SettingsData1 { P1 = "V11", P2 = "V2" };
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1", settings1, _cancel)
                .ConfigureAwait(false);

            var folder2 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2", _cancel).ConfigureAwait(false);

            var resourceUrl = "/OData.svc/Root/Content/Folder1/('Folder2')/GetSettings";
            var queryString = "?name=Settings1";
            ODataResponse response;

            // Test-1: Get settings by an administrator 
            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);
            Assert.AreEqual(settings1, JsonConvert.DeserializeObject<SettingsData1>(response.Result));

            // Test-2: Get settings with Open permission
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(globalSettings.Id, user1.Id, true, PermissionType.OpenMinor)
                .ApplyAsync(_cancel);

            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);
            Assert.AreEqual(settings1, JsonConvert.DeserializeObject<SettingsData1>(response.Result));

            // Test-3: Get settings without Open permission
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(globalSettings.Id, user1.Id, true, PermissionType.Open)
                .ApplyAsync(_cancel);

            using (new CurrentUserBlock(user1))
                response = await ODataGetAsync(resourceUrl, queryString).ConfigureAwait(false);
            Assert.AreEqual("{}", response.Result);

        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OD_Settings_WriteSettings()
    {
        await ODataTestAsync(async () =>
        {
            ContentTypeInstaller.InstallContentType(Settings1.Ctd);
            var folder3 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2/Folder3", _cancel).ConfigureAwait(false);

            // ACT
            var response = await ODataPostAsync(
                    $"/OData.svc/Root/Content('Folder1')/WriteSettings", null,
                    "models=[{\"name\":\"Settings1\",\"settingsData\":{\"P1\":\"V1\",\"P2\":\"V2\"}}]")
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual(204, response.StatusCode);
            var loadedSettings = await Node.LoadAsync<Settings>("/Root/Content/Folder1/Settings/Settings1.settings", _cancel)
                .ConfigureAwait(false);
            var loadedJsonData = RepositoryTools.GetStreamString(loadedSettings.Binary.GetStream());
            Assert.AreEqual("{\"P1\":\"V1\",\"P2\":\"V2\"}", loadedJsonData);
        }).ConfigureAwait(false);
    }
}