using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Data.Replication;
using SenseNet.Services.Core.Operations;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class ReplicationTests : TestBase
{
    [TestMethod]
    public void Replication_Parser_Defaults()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.AreEqual(10, descriptor.CountMax);
        Assert.AreEqual(100, descriptor.MaxItemsPerFolder);
        Assert.AreEqual(100, descriptor.MaxFoldersPerFolder);
        Assert.AreEqual(0, descriptor.FirstFolderIndex);
        Assert.IsNull(descriptor.DiversityControl);
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(0, descriptor.Diversity.Count);
    }
    [TestMethod]
    public void Replication_Parser_EmptyDiversity()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""CountMax"": 42,
  ""MaxItemsPerFolder"": 5,
  ""MaxFoldersPerFolder"": 4,
  ""FirstFolderIndex"": 1,
  ""DiversityControl"": { }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.AreEqual(42, descriptor.CountMax);
        Assert.AreEqual(5, descriptor.MaxItemsPerFolder);
        Assert.AreEqual(4, descriptor.MaxFoldersPerFolder);
        Assert.AreEqual(1, descriptor.FirstFolderIndex);
        Assert.IsNotNull(descriptor.DiversityControl);
        Assert.AreEqual(0, descriptor.DiversityControl.Count);
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(0, descriptor.Diversity.Count);
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""0 TO 99 RANDOM""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(1, descriptor.Diversity.Count);
    }
}