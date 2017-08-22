using System;
using System.Web;

namespace SenseNet.TokenAuthentication
{
    public static class CookieHelper
    {
        public static void InsertSecureCookie(HttpResponseBase response, string token, string cookieName, DateTime expiration)
        {
            var authCookie = new HttpCookie(cookieName, token)
            {
                HttpOnly = true,
                Secure = true,
                Expires = expiration
            };

            response.Cookies.Add(authCookie);
        }

        public static void DeleteCookie(HttpResponseBase response, string cookieName)
        {
            var sessionCookie = new HttpCookie(cookieName, string.Empty)
            {
                Expires = DateTime.UtcNow.AddDays(-1)
            };
            response.Cookies.Add(sessionCookie);
        }

        public static HttpCookie GetCookie(HttpRequestBase request, string cookieName)
        {
            return request.Cookies?[cookieName];
        }
    }
}