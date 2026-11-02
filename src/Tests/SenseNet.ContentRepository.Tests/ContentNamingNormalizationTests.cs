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
        public void Normalization_IsDisabledByDefault()
        {
            Assert.IsFalse(ContentNaming.RemoveDiacritics);
            Assert.IsFalse(ContentNaming.ReplaceWhitespace);
        }

        [DataTestMethod]
        [DataRow(false, false, "Árvíz tükör")]
        [DataRow(true, false, "Arviz tukor")]
        [DataRow(false, true, "Árvíz-tükör")]
        [DataRow(true, true, "Arviz-tukor")]
        public void Normalization_OptionsAreIndependent(bool removeDiacritics, bool replaceWhitespace, string expected)
        {
            using var diacritics = new Swindler<bool>(removeDiacritics,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var whitespace = new Swindler<bool>(replaceWhitespace,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);

            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }

        [DataTestMethod]
        [DataRow(null, "Árvíztűrő tükörfúrógép", "Arvizturo-tukorfurogep")]
        [DataRow(null, "áéíóöőúüű ÁÉÍÓÖŐÚÜŰ", "aeiooouuu-AEIOOOUUU")]
        [DataRow(null, "A\u0301rvi\u0301ztu\u030Bro\u030B tu\u0308ko\u0308rfu\u0301ro\u0301ge\u0301p", "Arvizturo-tukorfurogep")]
        [DataRow(null, "Árvíz\t\u00a0\u2003tükör\r\n", "Arviz-tukor")]
        [DataRow(null, "Árvíz / tükör?", "Arviz-tukor")]
        [DataRow(null, "Árvíz-- tükör", "Arviz-tukor")]
        [DataRow(null, "Árvíz.txt", "Arviz.txt")]
        [DataRow("régi.txt", "Új név", "Uj-nev.txt")]
        [DataRow("régi.txt", "Új név.txt", "Uj-nev.txt")]
        [DataRow("régi.txt", "Új név.doc", "Uj-nev.doc.txt")]
        [DataRow("régi.dóc", "Új név.dóc", "Uj-nev.dóc")]
        [DataRow(null, "Új név.dóc", "Uj-nev.doc")]
        [DataRow(null, "東京 café", "東京-cafe")]
        [DataRow(null, "Straße smør", "Straße-smør")]
        [DataRow(null, "😀 Árvíz", "😀-Arviz")]
        [DataRow(null, "A\U0001d185 B", "A-B")]
        [DataRow(null, "  Árvíz  ", "-Arviz")]
        [DataRow(null, "\t\u00a0\r\n", "")]
        [DataRow(null, "\u0301\u0308", "")]
        [DataRow(null, "", "")]
        [DataRow("régi.txt", null, null)]
        public void Normalization_HandlesUnicodeAndExtensions(string originalName, string displayName, string expected)
        {
            using var diacritics = new Swindler<bool>(true,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var whitespace = new Swindler<bool>(true,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);

            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(originalName, displayName));
        }

        [TestMethod]
        public void Normalization_RunsBeforeInvalidCharacterReplacement()
        {
            using var diacritics = new Swindler<bool>(true,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var whitespace = new Swindler<bool>(true,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);
            using var pattern = new Swindler<string>("[^a-zA-Z0-9.\\-_]",
                () => ContentNaming.InvalidNameCharsPattern, value => ContentNaming.InvalidNameCharsPattern = value);

            Assert.AreEqual("Arviz-tukor",
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }

        [TestMethod]
        public void Normalization_WhitespaceOnlyKeepsDiacriticsAndTheirUnicodeForm()
        {
            using var diacritics = new Swindler<bool>(false,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var whitespace = new Swindler<bool>(true,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);

            Assert.AreEqual("A\u0301rvi\u0301z-tu\u0308ko\u0308r",
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null,
                    "A\u0301rvi\u0301z tu\u0308ko\u0308r"));
        }

        [TestMethod]
        public void Normalization_ExistingPatternCanAlreadyReplaceWhitespace()
        {
            using var diacritics = new Swindler<bool>(false,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var whitespace = new Swindler<bool>(false,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);
            using var pattern = new Swindler<string>(@"[\s/]",
                () => ContentNaming.InvalidNameCharsPattern, value => ContentNaming.InvalidNameCharsPattern = value);

            Assert.AreEqual("Árvíz-tükör",
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }

        [TestMethod]
        public void Normalization_WhitespaceOptionDoesNotChangeNameValidation()
        {
            using var whitespace = new Swindler<bool>(true,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);

            var provider = new CharReplacementContentNamingProvider();
            provider.AssertNameIsValid("Árvíz tükör");
            Assert.AreEqual("Árvíz-tükör", provider.GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }

        [TestMethod]
        public void Normalization_DoesNotChangeTheEncodingProvider()
        {
            using var diacritics = new Swindler<bool>(true,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var whitespace = new Swindler<bool>(true,
                () => ContentNaming.ReplaceWhitespace, value => ContentNaming.ReplaceWhitespace = value);

            Assert.AreEqual("Árvíz tükör",
                new Underscore5FContentNamingProvider().GenerateNameFromDisplayName(null, "Árvíz tükör"));
        }
    }
}
