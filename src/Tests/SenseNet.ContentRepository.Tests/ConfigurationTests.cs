using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class ConfigurationTests : TestBase
{
    [TestMethod]
    public void Options_TracingOptions()
    {
        Test3(
            initializeConfig: builder =>
            {
                var configData = new Dictionary<string, string>
                    {{"sensenet:tracing:StartupTraceCategories", "Web,System"}};
                builder.AddInMemoryCollection(configData);
            },
            initializeServices: services => { },
            callback: () =>
            {
                // Test: loaded
                var tracingOptions = Providers.Instance.Services.GetService<IOptions<TracingOptions>>()?.Value;
                Assert.IsNotNull(tracingOptions);
                Assert.AreEqual("Web,System", tracingOptions.StartupTraceCategories);
                var categories = tracingOptions.GetStartupTraceCategories();
                Assert.AreSame(categories, tracingOptions.GetStartupTraceCategories());
                Assert.AreEqual("Web", categories[0]);
                Assert.AreEqual("System", categories[1]);
            });
    }
    [TestMethod]
    public void Options_LoggingOptions()
    {
        Test3(
            initializeConfig: builder =>
            {
                var configData = new Dictionary<string, string>
                    {{"sensenet:logging:AuditEnabled", "false"}}; // default: true
                builder.AddInMemoryCollection(configData);
            },
            initializeServices: services => { },
            callback: () =>
            {
                // Test: loaded
                var loggingOptions = Providers.Instance.Services.GetService<IOptions<LoggingOptions>>()?.Value;
                Assert.IsNotNull(loggingOptions);
                Assert.IsFalse(loggingOptions.AuditEnabled);

                // Test usage
                var node = new Folder(Repository.Root);
                var isAuditEnabled = node.IsAuditEnabled();
                Assert.IsFalse(isAuditEnabled);
            });
    }
    [TestMethod]
    public void Options_CacheOptions()
    {
        Test3(
            initializeConfig: builder =>
            {
                var configData = new Dictionary<string, string>
                {
                    {"sensenet:cache:CacheContentAfterSaveMode", "Containers"}, // None, Containers, All (default)
                    {"sensenet:cache:NodeIdDependencyEventPartitions", "333"},
                    {"sensenet:cache:NodeTypeDependencyEventPartitions", "555"},
                };
                builder.AddInMemoryCollection(configData);
            },
            initializeServices: services => { },
            callback: () =>
            {
                // Test: loaded
                var cacheOptions = Providers.Instance.Services.GetService<IOptions<CacheOptions>>()?.Value;
                Assert.IsNotNull(cacheOptions);
                Assert.AreEqual(CacheContentAfterSaveOption.Containers, cacheOptions.CacheContentAfterSaveMode);
                Assert.AreEqual(333, cacheOptions.NodeIdDependencyEventPartitions);
                Assert.AreEqual(555, cacheOptions.NodeTypeDependencyEventPartitions);
                Assert.AreEqual(400, cacheOptions.PathDependencyEventPartitions);

                // Test usage
                var node = new Folder(Repository.Root);
                var cacheMode = node.GetCacheContentAfterSaveMode();
                Assert.AreEqual(CacheContentAfterSaveOption.Containers, cacheMode);
            });
    }
    [TestMethod]
    public void Options_PackagingOptions()
    {
        Test3(
            initializeConfig: builder =>
            {
                var configData = new Dictionary<string, string>
                {
                    {"sensenet:packaging:NetworkTargets:0", "\\\\NetworkTarget-1\\Folder1"},
                    {"sensenet:packaging:NetworkTargets:1", "\\\\NetworkTarget-2\\Folder1"},
                    {"sensenet:packaging:TargetDirectory", "T:\\Packaging\\Target1\\"},
                };
                builder.AddInMemoryCollection(configData);
            },
            initializeServices: services => { },
            callback: () =>
            {
                // Test: loaded
                var options = Providers.Instance.Services.GetService<IOptions<PackagingOptions>>()?.Value;
                Assert.IsNotNull(options);
                Assert.IsNotNull(options.NetworkTargets);
                Assert.AreEqual(2, options.NetworkTargets.Length);
                Assert.AreEqual("\\\\NetworkTarget-1\\Folder1", options.NetworkTargets[0]);
                Assert.AreEqual("\\\\NetworkTarget-2\\Folder1", options.NetworkTargets[1]);
                Assert.AreEqual("T:\\Packaging\\Target1\\", options.TargetDirectory);
                Assert.AreEqual(null, options.PackageDirectory);
            });
    }
    [TestMethod]
    public void Options_VersioningOptions()
    {
        Test3(
            initializeConfig: builder =>
            {
                var configData = new Dictionary<string, string>
                    {{"sensenet:versioning:CheckInCommentsMode", "Compulsory"}}; // default: Recommended
                builder.AddInMemoryCollection(configData);
            },
            initializeServices: services => { },
            callback: () =>
            {
                // Test: loaded
                var options = Providers.Instance.Services.GetService<IOptions<VersioningOptions>>()?.Value;
                Assert.IsNotNull(options);
                Assert.AreEqual(CheckInCommentsMode.Compulsory, options.CheckInCommentsMode);

                // Test usage
                var file = new File(Repository.Root);
                Assert.AreEqual(CheckInCommentsMode.None, file.CheckInCommentsMode);
                file.VersioningMode = VersioningType.MajorAndMinor;
                Assert.AreEqual(CheckInCommentsMode.Compulsory, file.CheckInCommentsMode);
            });
    }
}