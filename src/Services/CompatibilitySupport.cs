using System;
using System.Web;
using SenseNet.ContentRepository;

namespace SenseNet.Services
{
    internal class CompatibilitySupport : ICompatibilitySupport
    {
        public Uri Request_Url => HttpContext.Current?.Request.Url;
        public Uri Request_UrlReferrer => HttpContext.Current?.Request.UrlReferrer;
        public bool Response_IsClientConnected => HttpContext.Current?.Response?.IsClientConnected ?? true;
    }
}
