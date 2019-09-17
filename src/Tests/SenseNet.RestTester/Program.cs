using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools;

namespace SenseNet.RestTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RestTestBase.InitAllTests(args);
            var emptyParams = new object[0];
            foreach (var testClass in TypeResolver.GetTypesByBaseType(typeof(RestTestBase)))
            {
                var testObject = Activator.CreateInstance(testClass);
                foreach (var testMethod in testClass.GetMethods().Where(m => m.GetCustomAttribute(typeof(TestMethodAttribute)) != null))
                {
                    try
                    {
                        testMethod.Invoke(testObject, emptyParams);
                        Console.WriteLine($"{testMethod.Name}: Ok.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{testMethod.Name}: FAILED. {e.InnerException?.Message ?? e.Message}");
                    }
                }
            }
            RestTestBase.CleanupAllTests();

            if (Debugger.IsAttached)
            {
                Console.Write("Press <enter> to exit...");
                Console.ReadLine();
            }
        }
    }
}
