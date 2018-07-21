//using System;
//using System.Net;

//namespace SenseNet.TokenAuthentication
//{
//    public static class CookieHelper
//    {
//        public static void InsertSecureCookie(HttpWebResponse response, string token, string cookieName, DateTime expiration)
//        {
//            var authCookie = new Cookie(cookieName, token)
//            {
//                HttpOnly = true,
//                Secure = true,
//                Expires = expiration
//            };

//            response.Cookies.Add(authCookie);
//        }

//        public static void DeleteCookie(HttpWebResponse response, string cookieName)
//        {
//            var sessionCookie = new Cookie(cookieName, string.Empty)
//            {
//                Expires = DateTime.UtcNow.AddDays(-1)
//            };
//            response.Cookies.Add(sessionCookie);
//        }

//        public static Cookie GetCookie(HttpWebRequest request, string cookieName)
//        {
//            return request.CookieContainer?.GetCookies(null)?[cookieName];
//        }
//    }
//}