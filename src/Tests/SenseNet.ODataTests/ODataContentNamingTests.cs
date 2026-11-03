using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Testing;

namespace SenseNet.ODataTests
{
    [TestClass]
    [DoNotParallelize]
    public class ODataContentNamingTests : ODataTestBase
    {
        [DataTestMethod]
        [DataRow(false, false, false, null, "Árvíztűrő tükörfúrógép")]
        [DataRow(true, false, false, null, "Arvizturo tukorfurogep")]
        [DataRow(false, true, false, null, "árvíztűrő tükörfúrógép")]
        [DataRow(true, true, false, null, "arvizturo tukorfurogep")]
        [DataRow(false, false, false, "Kézi név", "Kézi név")]
        [DataRow(true, false, false, "Kézi név", "Kezi nev")]
        [DataRow(false, true, false, "Kézi név", "kézi név")]
        [DataRow(true, true, false, "Kézi név", "kezi nev")]
        [DataRow(true, true, true, null, "arvizturo-tukorfurogep")]
        [DataRow(true, true, true, "Kézi név", "kezi-nev")]
        public async Task OD_POST_NormalizationOnlyChangesName(
            bool removeDiacritics, bool lowercase, bool replaceWhitespace, string explicitName, string expectedName)
        {
            await ODataTestAsync(async () =>
            {
                using var diacritics = new Swindler<bool>(removeDiacritics,
                    () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);

                using var casing = new Swindler<bool>(lowercase,
                    () => ContentNaming.Lowercase, value => ContentNaming.Lowercase = value);
                using var pattern = new Swindler<string>(replaceWhitespace
                        ? ContentNaming.InvalidNameCharsPattern.Insert(ContentNaming.InvalidNameCharsPattern.Length - 1, @"\s")
                        : ContentNaming.InvalidNameCharsPattern,
                    () => ContentNaming.InvalidNameCharsPattern, value => ContentNaming.InvalidNameCharsPattern = value);

                const string displayName = "Árvíztűrő tükörfúrógép";
                var root = CreateTestRoot("ODataNamingTestRoot");
                var model = new JObject
                {
                    ["__ContentType"] = "Folder",
                    ["DisplayName"] = displayName
                };
                if (explicitName != null)
                    model["Name"] = explicitName;

                var response = await ODataPostAsync("/OData.svc" + root.Path, "",
                    "models=[" + model.ToString(Formatting.None) + "]").ConfigureAwait(false);

                AssertNoError(response);
                var entity = GetEntity(response);
                var saved = Content.Load(entity.Id);
                Assert.AreEqual(expectedName, entity.Name);
                Assert.AreEqual(expectedName, saved.Name);
                Assert.AreEqual(displayName, saved.DisplayName);

                const string updatedDisplayName = "Új cím — őű";
                var update = new JObject { ["DisplayName"] = updatedDisplayName };
                response = await ODataPatchAsync("/OData.svc" + saved.Path, "",
                    "models=[" + update.ToString(Formatting.None) + "]").ConfigureAwait(false);

                AssertNoError(response);
                saved = Content.Load(entity.Id);
                Assert.AreEqual(expectedName, saved.Name);
                Assert.AreEqual(updatedDisplayName, saved.DisplayName);
            }).ConfigureAwait(false);
        }
    }
}
