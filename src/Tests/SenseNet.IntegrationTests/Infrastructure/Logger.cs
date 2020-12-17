using System.Diagnostics;
using System.IO;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public class Logger //UNDONE:<?: Use a better solution for Logger
    {
        public static void ClearLog()
        {
            File.Delete(@"D:\testlog.log");
            Trace.WriteLine("SnTrace: =================================================================");
        }
        public static void Log(string msg)
        {
            using (var writer = new StreamWriter(@"D:\testlog.log", true))
                writer.WriteLine(msg);
            Trace.WriteLine("SnTrace: " + msg);
        }
    }
}
