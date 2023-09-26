using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data.Replication;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class ReplicationTests : TestBase
{
    [TestMethod]
    public void Replication_Parser_Error_NullDescriptor()
    {
        try
        {
            // ACTION
            var parsed = ReplicationDescriptor.Parse(null);
            Assert.Fail("ArgumentException was not thrown");
        }
        catch (ArgumentNullException e)
        {
            // ASSERT
            Assert.AreEqual("Value cannot be null. (Parameter 'descriptor')", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_EmptyDescriptor()
    {
        try
        {
            // ACTION
            var parsed = ReplicationDescriptor.Parse(string.Empty);
            Assert.Fail("ArgumentException was not thrown");
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("The 'descriptor' argument cannot be empty.", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_DescriptorBadFormat()
    {
        try
        {
            // ACTION
            var parsed = ReplicationDescriptor.Parse("unrecognizable data");
            Assert.Fail("ArgumentException was not thrown");
        }
        catch (ArgumentException e)
        {
            // ASSERT
            Assert.AreEqual("The value of the 'descriptor' argument cannot be recognized as a valid ReplicationDescriptor.", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_NoDiversity()
    {
        var descriptor = @"{
""CountMax"": 42,
""MaxItemsPerFolder"": 5,
""MaxFoldersPerFolder"": 4,
""FirstFolderIndex"": 1
}";
        // ACTION
        var parsed = ReplicationDescriptor.Parse(descriptor);

        // ASSERT
        Assert.AreEqual(42, parsed.CountMax);
        Assert.AreEqual(5, parsed.MaxItemsPerFolder);
        Assert.AreEqual(4, parsed.MaxFoldersPerFolder);
        Assert.AreEqual(1, parsed.FirstFolderIndex);
        Assert.IsNull(parsed.DiversityControl);
        Assert.IsNull(parsed.Diversity);
    }
    [TestMethod]
    public void Replication_Parser_EmptyDiversity()
    {
        var descriptor = @"{
""CountMax"": 42,
""MaxItemsPerFolder"": 5,
""MaxFoldersPerFolder"": 4,
""FirstFolderIndex"": 1,
""DiversityControl"": { }
}";

        // ACTION
        var parsed = ReplicationDescriptor.Parse(descriptor);

        // ASSERT
        Assert.AreEqual(42, parsed.CountMax);
        Assert.AreEqual(5, parsed.MaxItemsPerFolder);
        Assert.AreEqual(4, parsed.MaxFoldersPerFolder);
        Assert.AreEqual(1, parsed.FirstFolderIndex);
        Assert.IsNotNull(parsed.DiversityControl);
        Assert.AreEqual(0, parsed.DiversityControl.Count);
        Assert.IsNull(parsed.Diversity);
    }

    [TestMethod]
    public void Replication_Parser_Defaults()
    {
        var descriptor = @"{
  ""CountMax"": 42,
}";

        // ACTION
        var parsed = ReplicationDescriptor.Parse(descriptor);

        // ASSERT
        Assert.AreEqual(42, parsed.CountMax);
        Assert.AreEqual(100, parsed.MaxItemsPerFolder);
        Assert.AreEqual(100, parsed.MaxFoldersPerFolder);
        Assert.AreEqual(0, parsed.FirstFolderIndex);
        Assert.IsNull(parsed.DiversityControl);
        Assert.IsNull(parsed.Diversity);
    }

}