using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.Testing;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ContentNamingFeatureTests
    {
        [TestMethod]
        public void NewOptions_AreDisabledAndMappingsEmptyByDefault()
        {
            Assert.IsFalse(ContentNaming.Transliterate);
            Assert.IsFalse(ContentNaming.ApplyCustomReplacements);
            Assert.AreEqual(0, ContentNaming.ReplacementSources.Length);
            Assert.AreEqual(0, ContentNaming.ReplacementTargets.Length);
        }

        [DataTestMethod]
        [DataRow(false, "Ægir Straße smør", "Ægir Straße smør")]
        [DataRow(true, "Ægir Straße smør", "Aegir Strasse smor")]
        [DataRow(true, "Łódź Œuvre Þing", "Lodz Oeuvre Thing")]
        [DataRow(true, "Борис", "Boris")]
        [DataRow(true, "深圳", "ShenZhen")]
        [DataRow(true, "A\u0301rvi\u0301z", "Arviz")]
        [DataRow(true, "👑", "-crown")]
        public void Transliteration_IsOptionalAndUsesAnyAscii(bool enabled, string input, string expected)
        {
            using var option = new Swindler<bool>(enabled,
                () => ContentNaming.Transliterate, value => ContentNaming.Transliterate = value);
            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, input));
        }

        [DataTestMethod]
        [DataRow("ä", "ae", "Fähre", "Faehre")]
        [DataRow("ab", "xy", "abABab", "xyABxy")]
        [DataRow("!", "", "Name!", "Name")]
        [DataRow(";", ",", "a;b", "a-b")]
        [DataRow(" ", "-", "Árvíztűrő tükörfúrógép", "Árvíztűrő-tükörfúrógép")]
        [DataRow("😀", "smile", "😀 Name", "smile Name")]
        public void CustomReplacements_AreLiteralCaseSensitiveAndRunBeforeValidation(
            string source, string target, string input, string expected)
        {
            using var option = new Swindler<bool>(true,
                () => ContentNaming.ApplyCustomReplacements, value => ContentNaming.ApplyCustomReplacements = value);
            using var sources = new Swindler<string[]>(new[] { source },
                () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
            using var targets = new Swindler<string[]>(new[] { target },
                () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);

            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, input));
        }

        [DataTestMethod]
        [DataRow(false, false, false, false, "ß Á")]
        [DataRow(false, false, false, true, "ß á")]
        [DataRow(false, true, false, false, "ß A")]
        [DataRow(false, true, false, true, "ß a")]
        [DataRow(false, false, true, false, "ss A")]
        [DataRow(false, false, true, true, "ss a")]
        [DataRow(false, true, true, false, "ss A")]
        [DataRow(false, true, true, true, "ss a")]
        [DataRow(true, false, false, false, "Ä Á")]
        [DataRow(true, false, false, true, "ä á")]
        [DataRow(true, true, false, false, "A A")]
        [DataRow(true, true, false, true, "a a")]
        [DataRow(true, false, true, false, "A A")]
        [DataRow(true, false, true, true, "a a")]
        [DataRow(true, true, true, false, "A A")]
        [DataRow(true, true, true, true, "a a")]
        public void FourOptions_AreIndependentAndCustomReplacementsRunFirst(
            bool replace, bool removeDiacritics, bool transliterate, bool lowercase, string expected)
        {
            using var replacement = new Swindler<bool>(replace,
                () => ContentNaming.ApplyCustomReplacements, value => ContentNaming.ApplyCustomReplacements = value);
            using var diacritics = new Swindler<bool>(removeDiacritics,
                () => ContentNaming.RemoveDiacritics, value => ContentNaming.RemoveDiacritics = value);
            using var ascii = new Swindler<bool>(transliterate,
                () => ContentNaming.Transliterate, value => ContentNaming.Transliterate = value);
            using var casing = new Swindler<bool>(lowercase,
                () => ContentNaming.Lowercase, value => ContentNaming.Lowercase = value);
            using var sources = new Swindler<string[]>(new[] { "ß" },
                () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
            using var targets = new Swindler<string[]>(new[] { "Ä" },
                () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);

            Assert.AreEqual(expected,
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "ß Á"));
        }

        [TestMethod]
        public void CustomReplacements_AreAppliedInConfiguredOrder()
        {
            using var option = new Swindler<bool>(true,
                () => ContentNaming.ApplyCustomReplacements, value => ContentNaming.ApplyCustomReplacements = value);
            using var sources = new Swindler<string[]>(new[] { "a", "b" },
                () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
            using var targets = new Swindler<string[]>(new[] { "b", "c" },
                () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);

            Assert.AreEqual("cc",
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName(null, "ab"));
        }

        [DataTestMethod]
        [DataRow(new[] { "a" }, new string[0])]
        [DataRow(new[] { "" }, new[] { "x" })]
        [DataRow(new string[] { null }, new[] { "x" })]
        [DataRow(new[] { "a" }, new string[] { null })]
        public void CustomReplacements_RejectInvalidPairsOnlyWhenEnabled(string[] from, string[] to)
        {
            using var sources = new Swindler<string[]>(from,
                () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
            using var targets = new Swindler<string[]>(to,
                () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);
            var provider = new CharReplacementContentNamingProvider();
            Assert.AreEqual("Name", provider.GenerateNameFromDisplayName(null, "Name"));

            using var option = new Swindler<bool>(true,
                () => ContentNaming.ApplyCustomReplacements, value => ContentNaming.ApplyCustomReplacements = value);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GenerateNameFromDisplayName(null, "Name"));
        }

        [TestMethod]
        public void NewFeatures_PreserveOriginalExtensionAndEncodingProvider()
        {
            using var replacement = new Swindler<bool>(true,
                () => ContentNaming.ApplyCustomReplacements, value => ContentNaming.ApplyCustomReplacements = value);
            using var ascii = new Swindler<bool>(true,
                () => ContentNaming.Transliterate, value => ContentNaming.Transliterate = value);
            using var sources = new Swindler<string[]>(new[] { "Æ" },
                () => ContentNaming.ReplacementSources, value => ContentNaming.ReplacementSources = value);
            using var targets = new Swindler<string[]>(new[] { "Œ" },
                () => ContentNaming.ReplacementTargets, value => ContentNaming.ReplacementTargets = value);

            Assert.AreEqual("Oegir.DÓC",
                new CharReplacementContentNamingProvider().GenerateNameFromDisplayName("old.DÓC", "Ægir.DÓC"));
            Assert.AreEqual("Ægir.DÓC",
                new Underscore5FContentNamingProvider().GenerateNameFromDisplayName(null, "Ægir.DÓC"));
        }
    }
}
