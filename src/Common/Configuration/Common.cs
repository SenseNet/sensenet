using System;

namespace SenseNet.Configuration
{
    public class Common
    {
        /// <summary>
        /// Gets a value indicating whether there is an HttpContext available or not.
        /// This property always returns false.
        /// </summary>
        [Obsolete("Always returns false. Use a different solution for checking the web environment.", true)]
        public static bool IsWebEnvironment => false;
    }
}
