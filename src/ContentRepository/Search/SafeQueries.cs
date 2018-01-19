using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search
{
    /// <summary>
    /// A marker interface for classes that hold safe queries in static readonly string properties. The visibility of these properties are irrelevant.
    /// In a solution can be more ISafeQueryHolder implementations. The property values from these classes will be collected
    ///   in order to build the white list of queries that can be accepted in elevated mode.
    /// Implementation classes can be anywhere in the solution. Property name can be anything because only the values will be collected.
    /// </summary>
    /// <example>Here is an example that explains a full implementation of some safe queries
    /// <code>
    /// public class SafeQueries : ISafeQueryHolder
    /// {
    ///     public static string AllDevices { get { return "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"; } }
    ///     public static string InFolderAndSomeType { get { return "+InFolder:@0 +TypeIs:(@1)"; } }
    /// }
    /// </code>
    /// </example>
    public interface ISafeQueryHolder { }

    /// <summary>
    /// Provides a method for check safety of a CQL query.
    /// </summary>
    public class SafeQueries
    {
        private static readonly string[] Queries;
        static SafeQueries()
        {
            var genuineQueries = new List<string>();
            foreach (Type t in TypeResolver.GetTypesByInterface(typeof(ISafeQueryHolder)))
            {
                genuineQueries.AddRange(
                    t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.GetSetMethod() == null)
                    .Select(x => x.GetGetMethod(true).Invoke(null, null) as string)
                    .Where(y => y != null).Distinct().ToArray());
            }
            Queries = genuineQueries.ToArray();
        }

        /// <summary>
        /// Returns with true if the given CQL query is safe.
        /// </summary>
        public static bool IsSafe(string queryText)
        {
            return Queries.Contains(queryText);
        }
    }
}
