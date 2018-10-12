using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SharingTests : TestBase
    {
        [TestMethod]
        public void Indexing_IndexFields()
        {
            // ARRANGE
            var sd1 = new SharingData
            {
                Token = "abc1@example.com",
                Identity = 0,
                Level = "Open",
                Mode = "Private",
                ShareDate = DateTime.UtcNow.AddHours(-1),
                CreatorId = 1
            };

            var sharingItems = new List<SharingData> { sd1 };

            // ACTION
            var ih = new SharingInfoIndexHandler { OwnerIndexingInfo = new PerFieldIndexingInfo() };
            var indexFields = ih.GetIndexFields(sharingItems).ToArray();

            // ASSERT
            Assert.IsNotNull(indexFields.FirstOrDefault(f => f.StringValue == sd1.Token),
                "Sharing data indexfield not found by email.");
            Assert.IsNotNull(indexFields.FirstOrDefault(f => f.IntegerValue == sd1.Identity),
                "Sharing data indexfield not found by identity.");
            Assert.IsNotNull(indexFields.FirstOrDefault(f => f.IntegerValue == sd1.CreatorId),
                "Sharing data indexfield not found by creator.");
        }
    }
}
