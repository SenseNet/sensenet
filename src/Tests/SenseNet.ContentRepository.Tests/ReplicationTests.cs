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
    public void Replication_Parser_IntDiversity_Constant()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""42""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(1, descriptor.Diversity.Count);
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity_Constant_Error()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""FortyTwo""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        try
        {
            descriptor.Initialize();
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("Invalid constant value in the IntDiversity of the 'Index' field: 'FortyTwo'", e.Message);
        }
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity_Sequence()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""1 TO 99""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(1, descriptor.Diversity.Count);
        Assert.IsTrue(descriptor.Diversity.ContainsKey("Index"));
        var diversity = (IntDiversity) descriptor.Diversity["Index"];
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(1, diversity.MinValue);
        Assert.AreEqual(99, diversity.MaxValue);
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity_SequenceExplicit()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""1 TO 99 Sequence""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(1, descriptor.Diversity.Count);
        Assert.IsTrue(descriptor.Diversity.ContainsKey("Index"));
        var diversity = (IntDiversity) descriptor.Diversity["Index"];
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(1, diversity.MinValue);
        Assert.AreEqual(99, diversity.MaxValue);
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity_Random()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""1 TO 99 RANDOM""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        descriptor.Initialize();

        // ASSERT
        Assert.IsNotNull(descriptor.Diversity);
        Assert.AreEqual(1, descriptor.Diversity.Count);
        Assert.IsTrue(descriptor.Diversity.ContainsKey("Index"));
        var diversity = (IntDiversity) descriptor.Diversity["Index"];
        Assert.AreEqual(DiversityType.Random, diversity.Type);
        Assert.AreEqual(1, diversity.MinValue);
        Assert.AreEqual(99, diversity.MaxValue);
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity_InvalidType()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""1 TO 99 INVALID""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        try
        {
            descriptor.Initialize();
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("Invalid qualifier in the IntDiversity of the 'Index' field: '1 TO 99 INVALID'. " +
                            "Expected last word: 'Random' or 'Sequence'", e.Message);
        }
    }

    [TestMethod]
    public void Replication_Parser_IntDiversity_InvalidRange()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""1 TOOO 99""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        try
        {
            descriptor.Initialize();
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("Invalid range definition in the IntDiversity of the 'Index' field: '1 TOOO 99'." +
                            " Expected format: <min> TO <max> RANDOM|SEQUENCE", e.Message);
        }

    }
    [TestMethod]
    public void Replication_Parser_IntDiversity_InvalidMinValue()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""one TO 99""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        try
        {
            descriptor.Initialize();
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("Invalid minimum value in the IntDiversity of the 'Index' field: 'one TO 99'", e.Message);
        }

    }
    [TestMethod]
    public void Replication_Parser_IntDiversity_InvalidMaxValue()
    {
        var descriptor = JsonConvert.DeserializeObject<ReplicationDescriptor>(@"{
  ""DiversityControl"": {
    ""Index"": ""1 TO ten""
  }
}");
        Assert.IsNotNull(descriptor);

        // ACTION
        try
        {
            descriptor.Initialize();
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("Invalid maximum value in the IntDiversity of the 'Index' field: '1 TO ten'", e.Message);
        }

    }
}