using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client;
using SenseNet.Tools;

namespace SenseNet.RestTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TestBase.InitAllTests(args);
            var emptyParams = new object[0];
            foreach (var testClass in TypeResolver.GetTypesByBaseType(typeof(TestBase)))
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
            TestBase.CleanupAllTests();

            if (Debugger.IsAttached)
            {
                Console.Write("Press <enter> to exit...");
                Console.ReadLine();
            }
        }
    }
}
