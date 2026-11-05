using System;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Testing;
using Settings = SenseNet.ContentRepository.Settings;

namespace SenseNet.ODataTests
{
    [TestClass]
    [DoNotParallelize]
    public class ODataContentNamingFeatureTests : ODataTestBase
    {
        [DataTestMethod]
        [DataRow(true, false, false, "Észak Straße Árvíz")]
        [DataRow(false, true, false, "Aegir Strasse Arviz")]
        [DataRow(true, true, false, "Eszak Strasse Arviz")]
        [DataRow(true, true, true, "eszak strasse arviz")]
        public async Task OD_POST_NewFeaturesPreserveDisplayName(bool replace, bool transliterate, bool lowercase,
            string expectedName)
        {
            await ODataTestAsync(async () =>
            {
                using var replacement = ConfigureOption(nameof(ContentNaming.ApplyCustomReplacements), replace);
                using var ascii = ConfigureOption(nameof(ContentNaming.Transliterate), transliterate);
                using var casing = ConfigureOption(nameof(ContentNaming.Lowercase), lowercase);
                using var sources = new Swindler<string[]>(new[] { "Ægir" },
                    () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
                using var targets = new Swindler<string[]>(new[] { "Észak" },
                    () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);

                var saved = await CreateFolderAsync("Ægir Straße Árvíz", expectedName).ConfigureAwait(false);
                const string updatedDisplayName = "Új cím — őű";
                var response = await ODataPatchAsync("/OData.svc" + saved.Path, "",
                    "models=[" + new JObject { ["DisplayName"] = updatedDisplayName }.ToString(Formatting.None) + "]")
                    .ConfigureAwait(false);
                AssertNoError(response);
                saved = Content.Load(saved.Id);
                Assert.AreEqual(expectedName, saved.Name);
                Assert.AreEqual(updatedDisplayName, saved.DisplayName);
            }).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(false, "Ægir Straße Łódź Árvíztűrő tükörfúrógép")]
        [DataRow(true, "AEgir Strasse Lodz Arvizturo tukorfurogep")]
        public async Task OD_POST_PreparedLatinMappingsPreserveDisplayName(bool enabled, string expectedName)
        {
            await ODataTestAsync(async () =>
            {
                using var replacement = ConfigureOption(nameof(ContentNaming.ApplyCustomReplacements), enabled);
                using var diacritics = ConfigureOption(nameof(ContentNaming.RemoveDiacritics), enabled);
                await CreateFolderAsync("Ægir Straße Łódź Árvíztűrő tükörfúrógép", expectedName).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(nameof(ContentNaming.RemoveDiacritics), "Straße ARVIZ")]
        [DataRow(nameof(ContentNaming.Lowercase), "straße árvíz")]
        [DataRow(nameof(ContentNaming.Transliterate), "Strasse ARVIZ")]
        [DataRow(nameof(ContentNaming.ApplyCustomReplacements), "Új ÁRVÍZ")]
        public async Task OD_Settings_CanVetoEachConfiguredOptionWithoutRestart(string option, string enabledName)
        {
            await ODataTestAsync(async () =>
            {
                Assert.IsTrue(Repository.Started(), "The Settings override must be tested on a running repository.");
                using var feature = ConfigureOption(option, true);
                using var sources = new Swindler<string[]>(new[] { "Straße" },
                    () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
                using var targets = new Swindler<string[]>(new[] { "Új" },
                    () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);
                const string original = "Straße ÁRVÍZ";

                // No settings file: use the configured feature.
                var before = await CreateFolderAsync(original, enabledName).ConfigureAwait(false);
                var settings = new Settings(Node.LoadNode(Repository.SettingsFolderPath))
                {
                    Name = "ContentNaming.settings"
                };
                settings.Binary.SetStream(RepositoryTools.GetStreamFromString("{}"));
                await settings.SaveAsync(CancellationToken.None).ConfigureAwait(false);

                // No key: still use the configured feature.
                await CreateFolderAsync(original, enabledName).ConfigureAwait(false);

                // Persist changes through the existing OData Settings API. Do not clear caches or restart.
                await WriteGlobalSettingsAsync(new JObject { [option] = false }).ConfigureAwait(false);
                await CreateFolderAsync(original, original).ConfigureAwait(false);
                Assert.AreEqual(enabledName, Content.Load(before.Id).Name, "Existing content must retain its name.");

                await WriteGlobalSettingsAsync(new JObject { [option] = true }).ConfigureAwait(false);
                await CreateFolderAsync(original, enabledName).ConfigureAwait(false);

                await WriteGlobalSettingsAsync(new JObject()).ConfigureAwait(false);
                await CreateFolderAsync(original, enabledName).ConfigureAwait(false);

                // Settings true cannot enable a feature that the host configuration disables.
                await WriteGlobalSettingsAsync(new JObject { [option] = true }).ConfigureAwait(false);
                using var disabled = ConfigureOption(option, false);
                await CreateFolderAsync(original, original).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async System.Threading.Tasks.Task<Content> CreateFolderAsync(string displayName, string expectedName)
        {
            var root = CreateTestRoot();
            var model = new JObject { ["__ContentType"] = "Folder", ["DisplayName"] = displayName };
            var response = await ODataPostAsync("/OData.svc" + root.Path, "",
                "models=[" + model.ToString(Formatting.None) + "]").ConfigureAwait(false);
            AssertNoError(response);
            var entity = GetEntity(response);
            var saved = Content.Load(entity.Id);
            Assert.AreEqual(expectedName, entity.Name);
            Assert.AreEqual(expectedName, saved.Name);
            Assert.AreEqual(displayName, saved.DisplayName);
            return saved;
        }

        private async Task WriteGlobalSettingsAsync(JObject values)
        {
            var model = new JObject { ["name"] = "ContentNaming", ["settingsData"] = values };
            var response = await ODataPostAsync("/OData.svc/Root('System')/WriteSettings", null,
                "models=[" + model.ToString(Formatting.None) + "]").ConfigureAwait(false);
            AssertNoError(response);
            Assert.AreEqual(204, response.StatusCode);
        }

        private static Swindler<bool> ConfigureOption(string name, bool enabled)
        {
            return name switch
            {
                nameof(ContentNaming.RemoveDiacritics) => new Swindler<bool>(enabled,
                    () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value),
                nameof(ContentNaming.Lowercase) => new Swindler<bool>(enabled,
                    () => ContentNaming.Lowercase, value => ContentNaming.Lowercase = value),
                nameof(ContentNaming.Transliterate) => new Swindler<bool>(enabled,
                    () => ContentNaming.Transliterate, value => ContentNaming.Transliterate = value),
                nameof(ContentNaming.ApplyCustomReplacements) => new Swindler<bool>(enabled,
                    () => ContentNaming.ApplyCustomReplacements, value => ContentNaming.ApplyCustomReplacements = value),
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
        }
    }
}
