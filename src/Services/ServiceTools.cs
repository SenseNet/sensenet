using System.Web;

namespace SenseNet.Services
{
    public static class ServiceTools
    {
        public static string GetClientIpAddress()
        {
            if (HttpContext.Current == null)
                return string.Empty;

            var clientIpAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(clientIpAddress))
                return clientIpAddress;

            clientIpAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            if (!string.IsNullOrEmpty(clientIpAddress))
                return clientIpAddress;

            return HttpContext.Current.Request.UserHostAddress ?? string.Empty;
        }
    }
}
