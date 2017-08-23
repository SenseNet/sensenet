#define TEST
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using Moq;
using SenseNet.ContentRepository;
using SenseNet.Portal.Virtualization;
using Xunit;
using HttpCookie = System.Web.HttpCookie;

namespace SenseNet.Services.Tests
{
    public class PortalAuthenticationModuleTests
    {

        public PortalAuthenticationModuleTests()
        {
        }

        private class Disposable : IDisposable
        {
            public void Dispose(){}
        }

        private void InitDependecies(Mock<PortalAuthenticationModule> module, Mock<HttpContextBase> context, Mock<HttpRequestBase> request, Mock<HttpResponseBase> response)
        {
            module.Object.GetRequest = (sender) => request.Object;
            module.Object.GetResponse = (sender) => response.Object;
            module.Object.GetContext = (sender) => context.Object;
            module.Object.GetVisitorPrincipal = () =>
            {
                var principal = new ClaimsPrincipal();
                var claims = new List<Claim>();
                claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "Visitor"));
                principal.AddIdentity(new ClaimsIdentity(claims));
                return principal;
            };
            module.Object.LoadUserPrincipal = (userName) => 
            {
                var principal = new ClaimsPrincipal();
                var claims = new List<Claim>();
                claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", userName));
                principal.AddIdentity(new ClaimsIdentity(claims));
                return principal;
            };
            module.Object.IsUserValid = (userName, password) => true;
            module.Object.GetSystemAccount = () => new Disposable();
            module.Object.GetBasicAuthHeader = () => null;

    }

        [Fact]
        public void OnAuthenticateRequestTokenLogoutTest()
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
            Configuration.TokenAuthentication.Audience = "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["rs"]);
            Assert.NotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.True(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.True(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.True(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.Equal(200, responseStatus);
        }

        [Fact]
        public void OnAuthenticateRequestTokenLogoutUrlTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["rs"]);
            Assert.NotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.True(mockResponse.Object.Cookies["as"].Expires < DateTime.Today);
            Assert.True(mockResponse.Object.Cookies["rs"].Expires < DateTime.Today);
            Assert.True(mockResponse.Object.Cookies["ahp"].Expires < DateTime.Today);
            Assert.Equal(200, responseStatus);
        }


        [Fact]
        public void OnAuthenticateRequestTokenLoginTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);
            module.Object.GetBasicAuthHeader = () => "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["rs"]);
            Assert.NotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.Matches("\"access\":\".+\"", body);
            Assert.Matches("\"refresh\":\".+\"", body);
            Assert.Equal(200, responseStatus);
        }

        [Fact]
        public void OnAuthenticateRequestTokenLoginUrlTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);
            module.Object.GetBasicAuthHeader = () => "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["rs"]);
            Assert.NotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.Matches("\"access\":\".+\"", body);
            Assert.Matches("\"refresh\":\".+\"", body);
            Assert.Equal(200, responseStatus);
        }

        [Fact]
        public void OnAuthenticateRequestTokenLoginWithInvalidUserTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);
            module.Object.GetBasicAuthHeader = () => "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";
            module.Object.IsUserValid = (n, p) => false;

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.Null(mockResponse.Object.Cookies["as"]);
            Assert.Null(mockResponse.Object.Cookies["rs"]);
            Assert.Null(mockResponse.Object.Cookies["ahp"]);
            Assert.DoesNotContain("\"access\":", body);
            Assert.DoesNotContain("\"refresh\":", body);
            Assert.Equal(401, responseStatus);
        }

        [Fact]
        public void OnAuthenticateRequestTokenRefreshTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.Null(mockResponse.Object.Cookies["rs"]);
            Assert.Matches("\"access\":\".+\"", body);
            Assert.DoesNotContain("\"refresh\":", body);
            Assert.Equal(200, responseStatus);
        }

        [Fact]
        public void OnAuthenticateRequestTokenRefreshUrlTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["ahp"]);
            Assert.Null(mockResponse.Object.Cookies["rs"]);
            Assert.Matches("\"access\":\".+\"", body);
            Assert.DoesNotContain("\"refresh\":", body);
            Assert.Equal(200, responseStatus);
        }

        [Fact]
        public void OnAuthenticateRequestTokenRefreshWithInvalidTokenTest()
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
            var module = new Mock<PortalAuthenticationModule>();
            InitDependecies(module, mockContext, mockRequest, mockResponse);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.Null(mockResponse.Object.Cookies["as"]);
            Assert.Null(mockResponse.Object.Cookies["ahp"]);
            Assert.Null(mockResponse.Object.Cookies["rs"]);
            Assert.DoesNotContain("\"access\":", body);
            Assert.DoesNotContain("\"refresh\":", body);
            Assert.Equal(401, responseStatus);
        }


    }

}