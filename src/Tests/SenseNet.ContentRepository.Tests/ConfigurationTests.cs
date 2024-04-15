using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
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
                var tracingOptions = Providers.Instance.Services.GetService<IOptions<TracingOptions>>()?.Value;
                Assert.IsNotNull(tracingOptions);
                Assert.AreEqual("Web,System", tracingOptions.StartupTraceCategories);
                var categories = tracingOptions.GetStartupTraceCategories();
                Assert.AreSame(categories, tracingOptions.GetStartupTraceCategories());
                Assert.AreEqual("Web", categories[0]);
                Assert.AreEqual("System", categories[1]);
            });
    }
}