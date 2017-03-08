// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Services : SnConfig
    {
        private const string SectionName = "sensenet/data";

        private static string _oDataServiceToken = GetString(SectionName, "ODataServiceToken", "odata.svc");
        public static string ODataServiceToken
        {
            get { return _oDataServiceToken; }
            internal set
            {
                _oDataServiceToken = value;
                _odataAndRoot = null;
            }
        }

        private static string _odataAndRoot;
        /// <summary>
        /// Returns with ODataServiceToken + "/('Root')"
        /// </summary>
        public static string ODataAndRoot
        {
            get
            {
                if (_odataAndRoot == null)
                    _odataAndRoot = ODataServiceToken + "/('Root')";
                return _odataAndRoot;
            }
        }
    }
}
