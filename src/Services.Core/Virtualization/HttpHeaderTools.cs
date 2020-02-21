using System;
using System.Linq;
using System.Web;
using SenseNet.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core.Virtualization
{
    /// <summary>
    /// Provides enumerated values that are used to set the Cache-Control HTTP header.
    /// </summary>
    public enum HttpCacheability
    {
        /// <summary>
        /// Sets the Cache-Control: no-cache header. Without a field name, the directive
        /// applies to the entire request and a shared (proxy server) cache must force a
        /// successful revalidation with the origin Web server before satisfying the request.
        /// With a field name, the directive applies only to the named field; the rest of
        /// the response may be supplied from a shared cache.
        /// </summary>
        NoCache = 1,

        /// <summary>
        /// Default value. Sets Cache-Control: private to specify that the response is cacheable
        /// only on the client and not by shared (proxy server) caches.
        /// </summary>
        Private = 2,

        /// <summary>
        /// Sets Cache-Control: public to specify that the response is cacheable by clients
        /// and shared (proxy) caches.
        /// </summary>
        Public = 4
    }


    public class HttpHeaderTools
    {
        private static readonly string HEADER_CONTENTDISPOSITION_NAME = "Content-Disposition";
        private static readonly string HEADER_CONTENTDISPOSITION_VALUE = "Attachment";
        
        private readonly HttpContext _context;

        public HttpHeaderTools(HttpContext context)
        {
            _context = context;
        }

        // ============================================================================================ Private methods
        private bool IsClientCached(DateTime contentModified)
        {
            var modifiedSinceHeader = _context.Request.Headers["If-Modified-Since"].FirstOrDefault();
            if (modifiedSinceHeader == null) 
                return false;

            if (!DateTime.TryParse(modifiedSinceHeader, out var isModifiedSince)) 
                return false;

            isModifiedSince = isModifiedSince.ToUniversalTime();

            // contentModified is more precise
            return isModifiedSince - contentModified > TimeSpan.FromSeconds(-1);
        }

        // ============================================================================================ Public methods
        public void SetCacheControlHeaders(int cacheForSeconds)
        {
            //UNDONE: port the whole HandleResponseForClientCache feature from PortalContextModule
            SetCacheControlHeaders(cacheForSeconds, HttpCacheability.Public);
        }
        public void SetCacheControlHeaders(int cacheForSeconds, HttpCacheability httpCacheability)
        {
            SetCacheControlHeaders(httpCacheability, maxAge: new TimeSpan(0, 0, cacheForSeconds));
        }
        public void SetCacheControlHeaders(HttpCacheability? httpCacheability = null, DateTime? lastModified = null, TimeSpan? maxAge = null)
        {
            var cacheHeaders = _context.Response.GetTypedHeaders().CacheControl;

            try
            {
                if (httpCacheability.HasValue)
                {
                    switch (httpCacheability)
                    {
                        case HttpCacheability.NoCache:
                            cacheHeaders.NoCache = true;
                            cacheHeaders.NoStore = true;
                            cacheHeaders.ProxyRevalidate = true;
                            cacheHeaders.MustRevalidate = true;
                            break;
                        case HttpCacheability.Private:
                            cacheHeaders.Private = true;
                            break;
                        case HttpCacheability.Public:
                            cacheHeaders.Public = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(httpCacheability), httpCacheability, null);
                    }
                }

                if (lastModified.HasValue)
                {
                    var t = lastModified.Value;
                    if (t > DateTime.UtcNow)
                        t = DateTime.UtcNow;
                    
                    _context.Response.Headers["Last-Modified"] = t.ToUniversalTime().ToString("r");
                }

                if (maxAge.HasValue)
                {
                    cacheHeaders.MaxAge = maxAge.Value;
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex,
                    "Exception in SetCacheControlHeaders. " +
                    $"httpCacheability:'{httpCacheability}' lastModified:'{lastModified}' maxAge:'{maxAge}'",
                    EventId.Portal);
            }
        }

        public void SetContentDispositionHeader(string fileName)
        {
            if (_context == null)
                return;

            var cdHeader = HEADER_CONTENTDISPOSITION_VALUE;
            if (!string.IsNullOrEmpty(fileName))
            {
                cdHeader += "; filename=\"" + fileName;

                // According to MSDN UrlPathEncode should not be used, so we need to replace '+' signs manually to 
                // let browsers interpret the file name correctly. Otherwise 'foo bar.docx' would become 'foo+bar.docx'.
                var encoded = HttpUtility.UrlEncode(fileName).Replace("+", "%20");

                // If the encoded name is different, add the UTF-8 version too. Note that this will be executed
                // even if the only difference is that the space characters were encoded.
                if (string.CompareOrdinal(fileName, encoded) != 0)
                    cdHeader += "\"; filename*=UTF-8''" + encoded;
            }

            // there must be only one header entry with this name
            _context.Response.Headers[HEADER_CONTENTDISPOSITION_NAME] = cdHeader;
        }
    }
}
