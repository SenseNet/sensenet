using System;

namespace SenseNet.Configuration
{
    public class ErrorStatusCode
    {
        public int StatusCode;
        public int SubStatusCode;

        public ErrorStatusCode(int statusCode) : this(statusCode, 0) {}
        public ErrorStatusCode(int statusCode, int subStatusCode)
        {
            StatusCode = statusCode;
            SubStatusCode = subStatusCode;
        }

        public static ErrorStatusCode Parse(string statusCode)
        {
            if (string.IsNullOrEmpty(statusCode))
                return null;

            var numParts = statusCode.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (numParts.Length > 2)
                return null;

            int mainStatusCode;
            int subStatusCode;

            if (!int.TryParse(numParts[0], out mainStatusCode))
                return null;
            if (mainStatusCode < 100 || mainStatusCode > 600)
                return null;

            if (numParts.Length == 1)
                return new ErrorStatusCode(mainStatusCode);
            if (!int.TryParse(numParts[1], out subStatusCode))
                return null;
            if (subStatusCode < 1 || subStatusCode > 10)
                return null;

            return new ErrorStatusCode(mainStatusCode, subStatusCode);
        }
    }
}
