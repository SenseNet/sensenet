using System.Collections.Concurrent;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Internal class responsible mainly for managing transaction-related context.
    /// </summary>
    public static class ContextHandler
    {
        /// <summary>
        /// Provides a way to set contextual data that flows with the call and 
        /// async context of a test or invocation.
        /// </summary>
        private static class CallContext
        {
            private static readonly ConcurrentDictionary<string, AsyncLocal<object>> State =
                new ConcurrentDictionary<string, AsyncLocal<object>>();

            /// <summary>
            /// Stores a given object and associates it with the specified name.
            /// </summary>
            /// <param name="name">The name with which to associate the new item in the call context.</param>
            /// <param name="data">The object to store in the call context.</param>
            public static void SetData(string name, object data) =>
                State.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;
            /// <summary>
            /// Retrieves an object with the specified name from the <see cref="CallContext"/>.
            /// </summary>
            /// <param name="name">The name of the item in the call context.</param>
            /// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
            public static object GetData(string name) =>
                State.TryGetValue(name, out var data) ? data.Value : null;
        }

        /// <summary>
        /// Gets a context object by its id from the context store of the current environment 
        /// (e.g. HttpContext or logical CallContext).
        /// </summary>
        public static object GetObject(string ident)
        {
            return CallContext.GetData(ident);
        }
        /// <summary>
        /// Puts a context object by its id into the context store of the current environment 
        /// (e.g. HttpContext or logical CallContext).
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="value"></param>
        public static void SetObject(string identifier, object value)
        {
            CallContext.SetData(identifier, value);
        }

        public static void Reset()
        {
        }
    }
}