#define TEST
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SenseNet.ContentRepository.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Services.Virtualization;
using HttpCookie = System.Web.HttpCookie;

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class TokenAuthenticationTests
    {
        private class Disposable : IDisposable
        {
            public void Dispose(){}
        }

        private static void InitDependecies(Mock<HttpContextBase> context, Mock<HttpRequestBase> request,
            Mock<HttpResponseBase> response)
        {
            AuthenticationHelper.GetRequest = (sender) => request.Object;
            AuthenticationHelper.GetResponse = (sender) => response.Object;
            AuthenticationHelper.GetContext = (sender) => context.Object;
            AuthenticationHelper.GetVisitorPrincipal = () =>
            {
                var principal = new ClaimsPrincipal();
                var claims = new List<Claim>();
                claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "Visitor"));
                principal.AddIdentity(new ClaimsIdentity(claims));
                return principal;
            };
            AuthenticationHelper.LoadPortalPrincipal = (userName) =>
            {
                var user = ServicesTestUser.Create("", userName);
                user.LastLoggedOut = DateTime.MinValue;
                var principal = new PortalPrincipal(user);
                return principal;
            };
            //AuthenticationHelper.LoadUserPrincipal = (userName) => 
            //{
            //    var principal = new ClaimsPrincipal();
            //    var claims = new List<Claim>();
            //    claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", userName));
            //    principal.AddIdentity(new ClaimsIdentity(claims));
            //    return principal;
            //};
            AuthenticationHelper.IsUserValid = (userName, password) => true;
            AuthenticationHelper.GetSystemAccount = () => new Disposable();
            AuthenticationHelper.GetBasicAuthHeader = () => null;
        }

        [TestMethod]
        public void TokenUltimateLogoutUrlTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/logout"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var queryStringCollection = new NameValueCollection();
            queryStringCollection.Add("ultimateLogout", "true");
            mockRequest.SetupGet(o => o.Params).Returns(queryStringCollection);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            bool ultimateLogout = false;
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.Setup(o => o.Logout(It.IsAny<bool>())).Callback((bool u) =>{ultimateLogout = u;});
            mockLogoutProvider.SetupGet(o => o.LoadPortalPrincipalForLogout).Returns( u =>
            {
                var user = ServicesTestUser.Create("", u);
                user.LastLoggedOut = new DateTime(2017,1,1,0,0,0);
                return new PortalPrincipal(user);
            });

            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsTrue(ultimateLogout);
            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }


        [TestMethod]
        public void TokenUltimateLogoutTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenLogout");
            headers.Add("X-Access-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var queryStringCollection = new NameValueCollection();
            queryStringCollection.Add("ultimateLogout", "true");
            mockRequest.SetupGet(o => o.Params).Returns(queryStringCollection);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            bool ultimateLogout = false;
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.Setup(o => o.Logout(It.IsAny<bool>())).Callback((bool u) =>{ultimateLogout = u;});
            mockLogoutProvider.SetupGet(o => o.LoadPortalPrincipalForLogout).Returns( u =>
            {
                var user = ServicesTestUser.Create("", u);
                user.LastLoggedOut = new DateTime(2017,1,1,0,0,0);
                return new PortalPrincipal(user);
            });

            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsTrue(ultimateLogout);
            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenUltimateLogoutUrlWithExpiredTokenTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/logout"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var queryStringCollection = new NameValueCollection();
            queryStringCollection.Add("ultimateLogout", "true");
            mockRequest.SetupGet(o => o.Params).Returns(queryStringCollection);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            bool ultimateLogout = false;
            var user = ServicesTestUser.Create("", "userName");
            user.LastLoggedOut = new DateTime(2017, 12, 31, 0, 0, 0);
            var portalPrincipal = new PortalPrincipal(user);
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.Setup(o => o.Logout(It.IsAny<bool>())).Callback((bool u) =>{ultimateLogout = u;});
            mockLogoutProvider.SetupGet(o => o.LoadPortalPrincipalForLogout).Returns(u => portalPrincipal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsFalse(ultimateLogout);
            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenUltimateLogoutWithExpiredTokenTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenLogout");
            headers.Add("X-Access-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var queryStringCollection = new NameValueCollection();
            queryStringCollection.Add("ultimateLogout", "true");
            mockRequest.SetupGet(o => o.Params).Returns(queryStringCollection);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            bool ultimateLogout = false;
            var user = ServicesTestUser.Create("", "userName");
            user.LastLoggedOut = new DateTime(2017, 12, 31, 0, 0, 0);
            var portalPrincipal = new PortalPrincipal(user);
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.Setup(o => o.Logout(It.IsAny<bool>())).Callback((bool u) =>{ultimateLogout = u;});
            mockLogoutProvider.SetupGet(o => o.LoadPortalPrincipalForLogout).Returns(u => portalPrincipal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsFalse(ultimateLogout);
            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }


        [TestMethod]
        public void TokenLogoutTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenLogout");
            headers.Add("X-Access-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047,8,24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            bool ultimateLogout = false;
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.Setup(o => o.Logout(It.IsAny<bool>())).Callback((bool u) =>{ultimateLogout = u;});

            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsFalse(ultimateLogout);
            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenLogoutUrlTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/logout"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenLogoutWithExpiredTokenTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/logout"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjE0ODMyMjkxMDAsImlhdCI6MTQ4MzIyODgwMCwibmJmIjoxNDgzMjI4ODAwLCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2017, 1, 1)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "egnUVGaLt_kfj_i12z7_sOSXMNByuz7p2OEEULUanpcJk4ySYebnuNc1v7XRT2ZYALc0FQEwgtrAN_uQ4779bg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2017, 1, 1)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "tpOjo49W0S0Dlt89-8AmYWiV2D4cBT9A5wdh8Rt0s37ZKbPt1aSN-oCywjpKlJ3nLC-Zqnd11IJ-B4dtDOsU-g"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2017, 1, 1)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.IsTrue(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenLogoutWithInvalidTokenTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/logout"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhFB"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.IsNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNull(mockResponse.Object.Cookies["ahp"]);
            Assert.AreEqual(401, responseStatus);
        }


        [TestMethod]
        public void TokenAccessTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenAccess");
            headers.Add("X-Access-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.AreEqual("username", mockContext.Object.User.Identity.Name);
            Assert.IsNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNull(mockResponse.Object.Cookies["ahp"]);
            Assert.AreEqual(0, responseStatus);
        }

        [TestMethod]
        public void TokenAccessUrlTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/odata.svc/content(1)"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhEA"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            IPrincipal user = null;
            mockContext.SetupSet(o => o.User = It.IsAny<IPrincipal>()).Callback((IPrincipal p) => user = p);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.AreEqual("username", user.Identity.Name);
            Assert.IsNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNull(mockResponse.Object.Cookies["ahp"]);
            Assert.AreEqual(0, responseStatus);
        }


        [TestMethod]
        public void TokenAccessWithInvalidTokenTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/odata.svc/content(1)"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var requestCookies = new HttpCookieCollection();
            requestCookies.Add(new HttpCookie("ahp")
            {
                Value = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIxMzQyMDQzMTgsImlhdCI6MTUwMzQ4NDMxOCwibmJmIjoxNTAzNDg0MzE4LCJuYW1lIjoidXNlcm5hbWUifQ"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("as")
            {
                Value = "4UN3ajbm74CKeTAk3VpiJR2f0VAiKydjZg0BWwpbBcWZM5uqVJ_YbazPboItaAliH5eepgvMfNbwwk9W8UIhFB"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 23)
            });
            requestCookies.Add(new HttpCookie("rs")
            {
                Value = "lL54nsEOfzZVD6vBCwMB4AxO1fwRgXadNBh9XtduaIygv_HJARlOXBISpC-sxG_96RSQ_fbnU-PcxWvj7w67Rg"
                ,HttpOnly = true
                ,Secure = true
                ,Expires = new DateTime(2047, 8, 24)
            });
            mockRequest.SetupGet(o => o.Cookies).Returns(requestCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            IPrincipal user = null;
            mockContext.SetupSet(o => o.User = It.IsAny<IPrincipal>()).Callback((IPrincipal p) => user = p);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies(mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.AreEqual("Visitor", user.Identity.Name);
            Assert.IsNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNull(mockResponse.Object.Cookies["ahp"]);
            Assert.AreEqual(0, responseStatus);
        }

        [TestMethod]
        public void TokenLoginTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenLogin");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience= "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);
            AuthenticationHelper.GetBasicAuthHeader = () => "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";

            new TokenAuthentication().Authenticate(application, true, false);

            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(body.Contains("\"access\":\""));
            Assert.IsTrue(body.Contains("\"refresh\":\""));
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenLoginUrlTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/login"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);
            AuthenticationHelper.GetBasicAuthHeader = () => "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";

            new TokenAuthentication().Authenticate(application, true, false);

            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsTrue(body.Contains("\"access\":\""));
            Assert.IsTrue(body.Contains("\"refresh\":\""));
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenLoginWithInvalidUserTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenLogin");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);
            AuthenticationHelper.GetBasicAuthHeader = () => "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";
            AuthenticationHelper.IsUserValid = (n, p) => false;

            new TokenAuthentication().Authenticate(application, true, true);

            Assert.IsNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsFalse(body.Contains("\"access\":"));
            Assert.IsFalse(body.Contains("\"refresh\":"));
            Assert.AreEqual(401, responseStatus);
        }

        [TestMethod]
        public void TokenRefreshTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenRefresh");
            headers.Add("X-Refresh-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIwOTA1NzczOTcsImlhdCI6MTQ5MDU3NzA5NywibmJmIjoxNDkwNTc3Mzk3LCJuYW1lIjoiTXlOYW1lIn0");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var inCookies = new HttpCookieCollection();
            inCookies.Add(new HttpCookie("rs", "_xdP6wc_Z4WgiIph-EC7O5Hh2f_aaGGZTidnK33ss-hdyw1ss7soTs7lVKrYQvmA4zSNRQ632Y-kR4TYyWdJiw"));;
            mockRequest.SetupGet(o => o.Cookies).Returns(inCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.SetupGet(o => o.LoadPortalPrincipalForLogout).Returns(u =>
            {
                var user = ServicesTestUser.Create("", u);
                user.LastLoggedOut = new DateTime(2017, 1, 1, 0, 0, 0);
                return new PortalPrincipal(user);
            });

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 10000000;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 1;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsTrue(body.Contains("\"access\":\""));
            Assert.IsFalse(body.Contains("\"refresh\":"));
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenRefreshUrlTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com/sn-token/refresh"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Refresh-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIwOTA1NzczOTcsImlhdCI6MTQ5MDU3NzA5NywibmJmIjoxNDkwNTc3Mzk3LCJuYW1lIjoiTXlOYW1lIn0");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var inCookies = new HttpCookieCollection();
            inCookies.Add(new HttpCookie("rs", "_xdP6wc_Z4WgiIph-EC7O5Hh2f_aaGGZTidnK33ss-hdyw1ss7soTs7lVKrYQvmA4zSNRQ632Y-kR4TYyWdJiw")); ;
            mockRequest.SetupGet(o => o.Cookies).Returns(inCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => { responseStatus = s; });
            var mockLogoutProvider = new Mock<ILogoutExecutor>();
            mockLogoutProvider.SetupGet(o => o.LoadPortalPrincipalForLogout).Returns(u =>
            {
                var user = ServicesTestUser.Create("", u);
                user.LastLoggedOut = new DateTime(2017, 1, 1, 0, 0, 0);
                return new PortalPrincipal(user);
            });

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 10000000;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 1;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);

            new TokenAuthentication(mockLogoutProvider.Object).Authenticate(application, false, false);

            Assert.IsNotNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsTrue(body.Contains("\"access\":\""));
            Assert.IsFalse(body.Contains("\"refresh\":"));
            Assert.AreEqual(200, responseStatus);
        }

        [TestMethod]
        public void TokenRefreshWithInvalidTokenTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.Url).Returns(new Uri("https://sensenet.com"));
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Action", "TokenRefresh");
            headers.Add("X-Refresh-Data",
                "yJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIwOTA1NzczOTcsImlhdCI6MTQ5MDU3NzA5NywibmJmIjoxNDkwNTc3Mzk3LCJuYW1lIjoiTXlOYW1lIn0");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var inCookies = new HttpCookieCollection();
            inCookies.Add(new HttpCookie("rs",
                "_xdP6wc_Z4WgiIph-EC7O5Hh2f_aaGGZTidnK33ss-hdyw1ss7soTs7lVKrYQvmA4zSNRQ632Y-kR4TYyWdJiw"));
            ;
            mockRequest.SetupGet(o => o.Cookies).Returns(inCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            int responseStatus = 0;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            mockResponse.SetupSet(o => o.StatusCode = It.IsAny<int>()).Callback((int s) => {responseStatus = s;});

            var mockContext = new Mock<HttpContextBase>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 10000000;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 1;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            InitDependecies( mockContext, mockRequest, mockResponse);

            new TokenAuthentication().Authenticate(application, false, false);

            Assert.IsNull(mockResponse.Object.Cookies["as"]);
            Assert.IsNull(mockResponse.Object.Cookies["ahp"]);
            Assert.IsNull(mockResponse.Object.Cookies["rs"]);
            Assert.IsFalse(body.Contains("\"access\":"));
            Assert.IsFalse(body.Contains("\"refresh\":"));
            Assert.AreEqual(401, responseStatus);
        }
    }
}