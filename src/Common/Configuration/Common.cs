namespace SenseNet.Configuration
{
    public class Common
    {
        /// <summary>
        /// Gets a value indicating whether there is an HttpContext available or not.
        /// </summary>
        public static bool IsWebEnvironment => System.Web.HttpContext.Current != null;
    }
}
