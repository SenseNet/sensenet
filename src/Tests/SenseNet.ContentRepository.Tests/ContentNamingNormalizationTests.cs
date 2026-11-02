using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.Testing;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ContentNamingNormalizationTests
    {
        [TestMethod]
        public void DiacriticRemoval_IsDisabledByDefault()
        {
            Assert.IsFalse(ContentNaming.RemoveDiacritics);
        }

        [DataTestMethod]
        [DataRow(false, "Árvíz tükör")]
        [DataRow(true, "Arviz tukor")]
        public void DiacriticRemoval_IsOptional(bool enabled, string expected)
        {
            using var diacritics = new Swindler<bool>(enabled,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);

            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }

        [DataTestMethod]
        [DataRow(null, "Árvíztűrő tükörfúrógép", "Arvizturo tukorfurogep")]
        [DataRow(null, "áéíóöőúüű ÁÉÍÓÖŐÚÜŰ", "aeiooouuu AEIOOOUUU")]
        [DataRow(null, "A\u0301rvi\u0301ztu\u030Bro\u030B tu\u0308ko\u0308rfu\u0301ro\u0301ge\u0301p", "Arvizturo tukorfurogep")]
        [DataRow(null, "Árvíz.txt", "Arviz.txt")]
        [DataRow("régi.txt", "Új név", "Uj nev.txt")]
        [DataRow("régi.txt", "Új név.txt", "Uj nev.txt")]
        [DataRow("régi.txt", "Új név.doc", "Uj nev.doc.txt")]
        [DataRow("régi.dóc", "Új név.dóc", "Uj nev.dóc")]
        [DataRow(null, "Új név.dóc", "Uj nev.doc")]
        [DataRow(null, "東京 café", "東京 cafe")]
        [DataRow(null, "Straße smør", "Straße smør")]
        [DataRow(null, "😀 Árvíz", "😀 Arviz")]
        [DataRow(null, "A\U0001d185 B", "A B")]
        [DataRow(null, "  Árvíz  ", "  Arviz  ")]
        [DataRow(null, "\u0301\u0308", "")]
        [DataRow(null, "", "")]
        [DataRow("régi.txt", null, null)]
        public void DiacriticRemoval_PreservesExistingNamingRules(
            string originalName, string displayName, string expected)
        {
            using var diacritics = new Swindler<bool>(true,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);

            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(originalName, displayName));
        }

        [TestMethod]
        public void DiacriticRemoval_RunsBeforeInvalidCharacterReplacement()
        {
            using var diacritics = new Swindler<bool>(true,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var pattern = new Swindler<string>("[^a-zA-Z0-9.\\-_]",
                () => ContentNaming.InvalidNameCharsPattern, value => ContentNaming.InvalidNameCharsPattern = value);

            Assert.AreEqual("Arviz-tukor",
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }

        [TestMethod]
        public void DiacriticRemoval_DoesNotChangeTheEncodingProvider()
        {
            using var diacritics = new Swindler<bool>(true,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);

            Assert.AreEqual("Árvíz tükör",
                new Underscore5FContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }
    }
}
