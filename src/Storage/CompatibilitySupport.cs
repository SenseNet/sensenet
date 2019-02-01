using System;
using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// Supports some old-school features.
    /// </summary>
    public static class CompatibilitySupport
    {
        /// <summary>
        /// =&gt; HttpContext.Current?.Request.Url;
        /// </summary>
        public static Uri Request_Url => Providers.Instance.CompatibilitySupport.Request_Url;
        /// <summary>
        /// =&gt; HttpContext.Current?.Request.UrlReferrer;
        /// </summary>
        public static Uri Request_UrlReferrer => Providers.Instance.CompatibilitySupport.Request_UrlReferrer;
        /// <summary>
        /// =&gt; HttpContext.Current?.Request.RawUrl;
        /// </summary>
        public static string Request_RawUrl => Providers.Instance.CompatibilitySupport.Request_RawUrl;

        /// <summary>
        /// =&gt; HttpContext.Current?.Response?.IsClientConnected ?? true;
        /// </summary>
        public static bool Response_IsClientConnected => Providers.Instance.CompatibilitySupport.Response_IsClientConnected;

        /// <summary>
        /// =&gt; HttpContext.Current.Request.Cookies.AllKeys.Contains("AllowResourceEditorCookie");
        /// </summary>
        public static bool IsResourceEditorAllowed => Providers.Instance.CompatibilitySupport.IsResourceEditorAllowed;

        /// <summary>
        /// =&gt; HttpContext.Current?.Items[name];
        /// </summary>
        public static object GetHttpContextItem(string name) => Providers.Instance.CompatibilitySupport.GetHttpContextItem(name);
        /// <summary>
        /// =&gt; HttpContext.Current?.Request.Headers[name];
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetRequestHeader(string name) => Providers.Instance.CompatibilitySupport.GetRequestHeader(name);
        /// <summary>
        /// =&gt; HttpContext.Current?.Request[name];
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetRequestItem(string name) => Providers.Instance.CompatibilitySupport.GetRequestItem(name);

    }
}
