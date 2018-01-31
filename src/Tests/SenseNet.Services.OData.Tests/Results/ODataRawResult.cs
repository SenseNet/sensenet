namespace SenseNet.Services.OData.Tests.Results
{
    public class ODataRaw : IODataResult
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
