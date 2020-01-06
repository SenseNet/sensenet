namespace SenseNet.ODataTests.Responses
{
    public class ODataRaw : IODataResponse
    {
        private string _raw;

        public ODataRaw(string raw)
        {
            _raw = raw;
        }

        public override string ToString()
        {
            return _raw;
        }
    }
}
