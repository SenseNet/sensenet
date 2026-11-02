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
        [DataRow(false, false, null, "Árvíz tükör")]
        [DataRow(true, false, null, "Arviz tukor")]
        [DataRow(false, true, null, "Árvíz-tükör")]
        [DataRow(true, true, null, "Arviz-tukor")]
        [DataRow(false, false, "Kézi név", "Kézi név")]
        [DataRow(true, false, "Kézi név", "Kezi nev")]
        [DataRow(false, true, "Kézi név", "Kézi-név")]
        [DataRow(true, true, "Kézi név", "Kezi-nev")]
        public async Task OD_POST_NormalizationOnlyChangesName(
            bool removeDiacritics, bool replaceWhitespace, string explicitName, string expectedName)
        {
            await ODataTestAsync(async () =>
            {
                using var diacritics = new Swindler<bool>(removeDiacritics,
                    () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
                using var whitespace = new Swindler<bool>(replaceWhitespace,
                    () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);

                const string displayName = "Árvíz tükör";
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
                Assert.AreEqual(displayName, saved.DisplayName,
                    "Generating Name must not normalize the stored DisplayName.");

                // Updating DisplayName later must neither normalize its value nor rename the content.
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
