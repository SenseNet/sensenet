#define TEST
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web;
using Moq;
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

        [Fact]
        public void OnAuthenticateRequestTokenLoginTest()
        {

            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Type", "Token");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            //var app = Mock.Of<HttpApplication>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            //mockContext.SetupGet(o => o.ApplicationInstance).Returns(app);
            //bool complete;
            //mockContext.Setup(o => o.ApplicationInstance.CompleteRequest()).Callback(() => { complete = true; });

            //var request = new HttpRequest("", "https://localhost:443/","");
            //request.AddHeader("X-Authentication-Type", "Token");
            //var writer = new StringWriter();
            //var response = new HttpResponse(writer);
            //var context = new HttpContext(request, response);
            //var principal = new ClaimsPrincipal();
            //var claims = new List<Claim>();
            //claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            //principal.AddIdentity(new ClaimsIdentity(claims));
            //context.User = principal;
            //HttpContext.Current = context;
            ////PortalContext.Create(context);
            Configuration.TokenAuthentication.Audience= "audience";
            Configuration.TokenAuthentication.Issuer = "issuer";
            Configuration.TokenAuthentication.Subject = "subject";
            Configuration.TokenAuthentication.EncriptionAlgorithm = "HS512";
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 5;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 1440;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 5;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            //application.Init(context);
            var module = new Mock<PortalAuthenticationModule>();
            module.Object.GetRequest = (sender) => mockRequest.Object;
            module.Object.GetResponse = (sender) => mockResponse.Object;
            module.Object.GetContext = (sender) => mockContext.Object;
            module.Setup(o => o.DispatchBasicAuthentication(It.IsAny<HttpApplication>())).Returns(true);
            //_module.Object.Init(app);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.NotNull(mockResponse.Object.Cookies["rs"]);
            Assert.Matches("\"access\":\".+\"", body);
            Assert.Matches("\"refresh\":\".+\"", body);
        }

        [Fact]
        public void OnAuthenticateRequestTokenRefreshTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Type", "Token");
            headers.Add("X-Refresh-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIwOTA1NzczOTcsImlhdCI6MTQ5MDU3NzA5NywibmJmIjoxNDkwNTc3Mzk3LCJuYW1lIjoiTXlOYW1lIn0");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var inCookies = new HttpCookieCollection();
            inCookies.Add(new HttpCookie("rs", "_xdP6wc_Z4WgiIph-EC7O5Hh2f_aaGGZTidnK33ss-hdyw1ss7soTs7lVKrYQvmA4zSNRQ632Y-kR4TYyWdJiw"));;
            mockRequest.SetupGet(o => o.Cookies).Returns(inCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
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
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 10000000;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 1;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            var module = new Mock<PortalAuthenticationModule>();
            module.Object.GetRequest = (sender) => mockRequest.Object;
            module.Object.GetResponse = (sender) => mockResponse.Object;
            module.Object.GetContext = (sender) => mockContext.Object;
            module.Setup(o => o.DispatchBasicAuthentication(It.IsAny<HttpApplication>())).Returns(false);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.NotNull(mockResponse.Object.Cookies["as"]);
            Assert.Null(mockResponse.Object.Cookies["rs"]);
            Assert.Matches("\"access\":\".+\"", body);
            Assert.DoesNotContain("\"refresh\":", body);
        }

        [Fact]
        public void OnAuthenticateRequestTokenAuthenticationTest()
        {
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(o => o.IsSecureConnection).Returns(true);
            var headers = new NameValueCollection();
            headers.Add("X-Authentication-Type", "Token");
            headers.Add("X-Access-Data", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJpc3N1ZXIiLCJzdWIiOiJzdWJqZWN0IiwiYXVkIjoiYXVkaWVuY2UiLCJleHAiOjIwOTA1Nzc1MDEsImlhdCI6MTQ5MDU3NzUwMSwibmJmIjoxNDkwNTc3NTAxLCJuYW1lIjoiTXlOYW1lIiwicm9sZSI6Ik15Um9sZSJ9");
            mockRequest.SetupGet(o => o.Headers).Returns(headers);
            var inCookies = new HttpCookieCollection();
            inCookies.Add(new HttpCookie("as", "u1ESlK6iUZmX8T5a_AB4U74vtH4x2GGoQ-rNdN-JG6UTQQFsfFrhSX6bIq1S_pFq-Qd4Y__VRMJC31hHbdie6g")); ;
            mockRequest.SetupGet(o => o.Cookies).Returns(inCookies);
            var mockResponse = new Mock<HttpResponseBase>();
            var cookies = new HttpCookieCollection();
            string body = "";
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
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
            Configuration.TokenAuthentication.AccessLifeTimeInMinutes = 10000000;
            Configuration.TokenAuthentication.RefreshLifeTimeInMinutes = 0;
            Configuration.TokenAuthentication.ClockSkewInMinutes = 1;
            Configuration.TokenAuthentication.SymmetricKeySecret = "very secrety secret";
            var application = new HttpApplication();
            var module = new Mock<PortalAuthenticationModule>();
            module.Object.GetRequest = (sender) => mockRequest.Object;
            module.Object.GetResponse = (sender) => mockResponse.Object;
            module.Object.GetContext = (sender) => mockContext.Object;
            module.Setup(o => o.DispatchBasicAuthentication(It.IsAny<HttpApplication>())).Returns(false);

            module.Object.OnAuthenticateRequest(application, EventArgs.Empty);

            Assert.Null(mockResponse.Object.Cookies["as"]);
            Assert.Null(mockResponse.Object.Cookies["rs"]);
            Assert.DoesNotContain("\"access\":", body);
            Assert.DoesNotContain("\"refresh\":", body);
            Assert.True(mockContext.Object.User is ClaimsPrincipal);
        }
    }

}