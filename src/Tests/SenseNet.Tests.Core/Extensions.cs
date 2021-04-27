using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Testing;

namespace SenseNet.Tests.Core
{
    public static class Extensions
    {
        public static void StartTest(this TestContext testContext, bool traceToFile = false)
        {
            if (traceToFile)
            {
                var tracers = SnTrace.SnTracers.ToArray();
                testContext.Properties["SnTrace.Operation.Writers"] = tracers;
                if (!tracers.Any(x => x is SnFileSystemTracer))
                    SnTrace.SnTracers.Add(new SnFileSystemTracer());
            }
            StartTestPrivate(testContext);
        }
        private static void StartTestPrivate(TestContext testContext)
        {
            Providers.Instance = new Providers();
            using (new Swindler<bool>(true, () => SnTrace.Test.Enabled, x => SnTrace.Test.Enabled = x))
                testContext.Properties["SnTrace.Operation"] =
                    SnTrace.Test.StartOperation($"TESTMETHOD: {testContext.FullyQualifiedTestClassName}.{testContext.TestName}" );
        }
        public static void FinishTestTest(this TestContext testContext)
        {
            using (new Swindler<bool>(true, () => SnTrace.Test.Enabled, x => SnTrace.Test.Enabled = x))
            {
                var op = (SnTrace.Operation)testContext.Properties["SnTrace.Operation"];
                SnTrace.Test.Write("TESTMETHOD: {0}.{1}: {2}", 
                    testContext.FullyQualifiedTestClassName, testContext.TestName, testContext.CurrentTestOutcome);
                if (op != null)
                {
                    op.Successful = true;
                    op.Dispose();
                }
                SnTrace.Flush();
            }
            var originalTracers = (ISnTracer[])testContext.Properties["SnTrace.Operation.Writers"];
            if (originalTracers != null)
            {
                SnTrace.SnTracers.Clear();
                SnTrace.SnTracers.AddRange(originalTracers);
            }
        }

        /// <summary>
        /// Returns with a new child of the given parent by the specified content type and name.
        /// The output and return values are reference equal.
        /// This method is helps to create a content chain and uses this link in a local variable:
        /// .CreateChild("MyType", "Content-1", , out Node localNode);
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name">Name of the new content.</param>
        /// <param name="typeName">Name of the existing content type.</param>
        /// <param name="child">The new child content. Copy of the return value.</param>
        /// <returns>The new child content.</returns>
        public static Node CreateChild(this Node parent, string name, string typeName, out Node child)
        {
            child = parent.CreateChild(name, typeName);
            return child;
        }
        /// <summary>
        /// Returns with a new child of the given parent by the specified content type and name.
        /// The output and return values are reference equal.
        /// This method is helps to create a content chain.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name">Name of the new content.</param>
        /// <param name="typeName">Name of the existing content type.</param>
        /// <returns>The new child content.</returns>
        public static Node CreateChild(this Node parent, string name, string typeName)
        {
            var content = Content.CreateNew(typeName, parent, name);
            content.Save();
            return content.ContentHandler;
        }

        public static T CreateChild<T>(this Node parent, string name, out T child) where T : Node
        {
            child = parent.CreateChild<T>(name);
            return child;
        }
        public static T CreateChild<T>(this Node parent, string name) where T : Node
        {
            var content = Content.CreateNew(typeof(T).Name, parent, name);
            content.Save();
            return (T)content.ContentHandler;
        }
    }
}
