using System;
using System.Web;
using SenseNet.ContentRepository;

namespace SenseNet.Services
{
    internal class CompatibilitySupport : ICompatibilitySupport
    {
        public Uri Request_UrlReferrer => HttpContext.Current?.Request.UrlReferrer;
    }
}
