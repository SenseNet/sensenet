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

namespace SenseNet.Services.Tests
{
    public class PortalAuthenticationModuleTests
    {

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
            string body;
            mockResponse.SetupGet(o => o.Cookies).Returns(cookies);
            mockResponse.Setup(o => o.Write(It.IsAny<string>())).Callback((string t) => { body = t; });
            var principal = new ClaimsPrincipal();
            var claims = new List<Claim>();
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "username"));
            principal.AddIdentity(new ClaimsIdentity(claims));

            var mockContext = new Mock<HttpContextBase>();
            var app = Mock.Of<HttpApplication>();
            mockContext.SetupGet(o => o.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(o => o.Response).Returns(mockResponse.Object);
            mockContext.SetupGet(o => o.User).Returns(principal);
            mockContext.SetupGet(o => o.ApplicationInstance).Returns(app);
            //bool complete;
            //mockContext.Setup(o => o.ApplicationInstance.CompleteRequest()).Callback(() => { complete = true; });
            //var mockApp = new Mock<HttpApplication>();
            //mockApp.SetupGet(o => o.Context).Returns(context);
            //mockApp.Raise(app => app.AuthenticateRequest+= null, new EventArgs());



            //var request = new HttpRequest(null, "https://localhost:443/","");
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
            //var app = new HttpApplication();
            var _module = new Mock<PortalAuthenticationModule>();
            _module.Object.GetRequest = (sender) => mockRequest.Object;
            _module.Object.GetResponse = (sender) => mockResponse.Object;
            _module.Object.GetContext = (sender) => mockContext.Object;
            _module.Setup(o => o.DispatchBasicAuthentication(It.IsAny<HttpApplication>())).Returns(true);
            //_module.Object.Init(app);

            _module.Object.OnAuthenticateRequest(new object(), EventArgs.Empty);
        }
    }
}