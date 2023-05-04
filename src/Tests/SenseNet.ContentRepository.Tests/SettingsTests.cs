using System;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests.Core;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Schema;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMethodReturnValue.Local

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class SettingsTests : TestBase
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

    private async STT.Task<T> EnsureSettingsAsync<T>(string contentPath, object settingValues, CancellationToken cancel) where T : Settings
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
        var constructor = typeof(T).GetConstructor(new [] { typeof(Node) });
        if (constructor == null)
            throw new InvalidOperationException("Type " + typeof(T).Name + " does not contain an appropriate constructor");
        var settings = (T)constructor.Invoke(new object[] { parent });
        settings.Name = typeof(T).Name + ".settings";
        return settings;
    }
    private async STT.Task<Node> EnsureNodeAsync(string path, CancellationToken cancel)
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
    #endregion

    [TestMethod]
    public async STT.Task Settings_GetPropertiesThroughTheSieve()
    {
        await Test(async () =>
        {
            ContentTypeInstaller.InstallContentType(Settings1.Ctd);
            await EnsureSettingsAsync<Settings1>("/Root/System",
                new { P1 = "V1" },
                _cancel).ConfigureAwait(false);
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1",
                new { P1 = "V11", P3 = "V3" },
                _cancel).ConfigureAwait(false);
            await EnsureSettingsAsync<Settings1>("/Root/Content/Folder1/Folder2",
                new { P2 = "V2", P3 = "V33" },
                _cancel).ConfigureAwait(false);
            var folder3 = await EnsureNodeAsync("/Root/Content/Folder1/Folder2/Folder3", _cancel).ConfigureAwait(false);
            var folder1 = await Node.LoadNodeAsync("/Root/Content/Folder1", _cancel).ConfigureAwait(false);
            var contents = await Node.LoadNodeAsync("/Root/Content", _cancel).ConfigureAwait(false);

            // Default (without context path)
            Assert.AreEqual("V1", Settings.GetValue<string>(nameof(Settings1), "P1"));
            Assert.AreEqual(null, Settings.GetValue<string>(nameof(Settings1), "P2"));
            Assert.AreEqual(null, Settings.GetValue<string>(nameof(Settings1), "P3"));

            // No local settings, nearest: default
            Assert.AreEqual("V1", Settings.GetValue<string>(nameof(Settings1), "P1", contents.Path));
            Assert.AreEqual(null, Settings.GetValue<string>(nameof(Settings1), "P2", contents.Path));
            Assert.AreEqual(null, Settings.GetValue<string>(nameof(Settings1), "P3", contents.Path));

            // Has explicit settings
            Assert.AreEqual("V11", Settings.GetValue<string>(nameof(Settings1), "P1", folder1.Path));
            Assert.AreEqual(null, Settings.GetValue<string>(nameof(Settings1), "P2", folder1.Path));
            Assert.AreEqual("V3", Settings.GetValue<string>(nameof(Settings1), "P3", folder1.Path));

            // No local settings, nearest: on the parent chain
            Assert.AreEqual("V11", Settings.GetValue<string>(nameof(Settings1), "P1", folder3.Path));
            Assert.AreEqual("V2", Settings.GetValue<string>(nameof(Settings1), "P2", folder3.Path));
            Assert.AreEqual("V33", Settings.GetValue<string>(nameof(Settings1), "P3", folder3.Path));
        }).ConfigureAwait(false);
    }
}