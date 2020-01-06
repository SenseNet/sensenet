using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.OData.Parser
{
    internal class ODataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    internal static class Globals
    {
        public static bool substringof(string contained, string container)
        {
            if (container == null || contained == null)
                return false;
            else
                return container.IndexOf(contained, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
        public static bool startswith(string a, string b)
        {
            if (a == null || b == null)
                return false;
            else
                return a.StartsWith(b, StringComparison.InvariantCultureIgnoreCase);
        }
        public static bool endswith(string a, string b)
        {
            if (a == null || b == null)
                return false;
            else
                return a.EndsWith(b, StringComparison.InvariantCultureIgnoreCase);
        }
        public static string concat(string a, string b)
        {
            return string.Concat(a, b);
        }
        public static int indexof(string a, string b)
        {
            return a.IndexOf(b, StringComparison.InvariantCultureIgnoreCase);
        }
        public static int length(string a)
        {
            return a.Length;
        }
        public static string replace(string a, string b, string c)
        {
            return a.Replace(b, c);
        }
        public static string substring(string a, int b)
        {
            return a.Substring(b);
        }
        public static string substring(string a, int b, int c)
        {
            return a.Substring(b, c);
        }
        public static string tolower(string a)
        {
            return a.ToLower();
        }
        public static string toupper(string a)
        {
            return a.ToUpper();
        }
        public static string trim(string a)
        {
            return a.Trim();
        }

        public static int day(DateTime d)
        {
            return d.Day;
        }
        public static int hour(DateTime d)
        {
            return d.Hour;
        }
        public static int minute(DateTime d)
        {
            return d.Minute;
        }
        public static int month(DateTime d)
        {
            return d.Month;
        }
        public static int second(DateTime d)
        {
            return d.Second;
        }
        public static int year(DateTime d)
        {
            return d.Year;
        }

        public static decimal round(decimal x)
        {
            return Math.Round(x);
        }
        public static double round(double x)
        {
            return Math.Round(x);
        }
        public static decimal floor(decimal x)
        {
            return Math.Floor(x);
        }
        public static double floor(double x)
        {
            return Math.Floor(x);
        }
        public static decimal ceiling(decimal x)
        {
            return Math.Ceiling(x);
        }
        public static double ceiling(double x)
        {
            return Math.Ceiling(x);
        }

        public static bool isof(SenseNet.ContentRepository.Content c, string type)
        {
            return c.ContentType.IsInstaceOfOrDerivedFrom(type);
        }

        public static object Point(double x, double y)
        {
            return new object();//TODO: ODATA: Globals.Point() implementation
        }
        public static object Point(double x, double y, double z)
        {
            return new object();//TODO: ODATA: Globals.Point() implementation
        }
    }
}
