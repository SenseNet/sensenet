﻿using System;
using System.Linq;
using SenseNet.Diagnostics;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using Microsoft.Net.Http.Headers;
using SenseNet.Portal;

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
        private static readonly string HEADER_CONTENTDISPOSITION_VALUE = "Attachment";
        
        private readonly HttpContext _context;
        private readonly CacheControlHeaderValue _cacheHeaders = new CacheControlHeaderValue();

        public HttpHeaderTools(HttpContext context)
        {
            _context = context;
        }

        // ============================================================================================ Private methods
        private bool IsClientCached(DateTime contentModified)
        {
            var modifiedSinceHeader = _context.Request.Headers[HeaderNames.IfModifiedSince].FirstOrDefault();
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
            SetCacheControlHeaders(cacheForSeconds, HttpCacheability.Public);
        }
        public void SetCacheControlHeaders(int cacheForSeconds, HttpCacheability httpCacheability)
        {
            SetCacheControlHeaders(httpCacheability, maxAge: new TimeSpan(0, 0, cacheForSeconds));
        }
        public void SetCacheControlHeaders(HttpCacheability? httpCacheability = null, 
            DateTime? lastModified = null, TimeSpan? maxAge = null)
        {
            // Cache control headers are stored temporarily in an aggregated object
            // and written to the single response header. The reason behind this is
            // that this method may be called multiple times with different kinds
            // of parameters.

            var writeCacheControlHeader = false;

            try
            {
                if (httpCacheability.HasValue)
                {
                    switch (httpCacheability)
                    {
                        case HttpCacheability.NoCache:
                            _cacheHeaders.NoCache = true;
                            _cacheHeaders.NoStore = true;
                            _cacheHeaders.ProxyRevalidate = true;
                            _cacheHeaders.MustRevalidate = true;
                            break;
                        case HttpCacheability.Private:
                            _cacheHeaders.Private = true;
                            break;
                        case HttpCacheability.Public:
                            _cacheHeaders.Public = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(httpCacheability), httpCacheability, null);
                    }

                    writeCacheControlHeader = true;
                }

                if (lastModified.HasValue)
                {
                    // make sure that the date is in the past
                    var t = lastModified.Value;
                    if (t > DateTime.UtcNow)
                        t = DateTime.UtcNow;
                    
                    _context.Response.Headers[HeaderNames.LastModified] = t.ToUniversalTime().ToString("r");
                }

                if (maxAge.HasValue)
                {
                    _cacheHeaders.MaxAge = maxAge.Value;
                    writeCacheControlHeader = true;
                }

                if (writeCacheControlHeader)
                    _context.Response.Headers[HeaderNames.CacheControl] = _cacheHeaders.ToString();
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

            if (string.IsNullOrEmpty(fileName))
                fileName = "unknown";

            // Inline or Attachment
            var forDownload = _context.Request.Path.StartsWithSegments("/binaryhandler.ashx");

            // the default .net core conversion should work in most cases
            if (TrySetHeader(fileName))
                return;

            //fallback 1
            if (TrySetHeader(ConvertByUrlEncode()))
                return;

            // fallback to ascii
            TrySetHeader(ConvertToAscii());

            bool TrySetHeader(string fn)
            {
                var cdh = new ContentDisposition { FileName = fn, Inline = !forDownload};
                try
                {
                    // We have to remove new line characters so that the generated value is a single
                    // line of text, otherwise setting the header would lead to an
                    // "Invalid non-ASCII or control character in header" exception.
                    var headerValue = cdh.ToString().Replace("\r\n", string.Empty);

                    // There must be only one header entry with this name
                    _context.Response.Headers[HeaderNames.ContentDisposition] = headerValue;
                }
                catch
                {
                    // this may happen if the header cannot handle certain special characters
                    return false;
                }
                return true;
            }
            string ConvertByUrlEncode() => WebUtility.UrlEncode(fileName)?.Replace("+", "%20");

            string ConvertToAscii()
            {
                // This method will ruin unicode characters in the file name, but at least
                // it will be possible to download it.

                var ascii = Encoding.ASCII;
                var unicode = Encoding.UTF8;
                var unicodeBytes = unicode.GetBytes(fileName);

                var asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
                var asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
                ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
                var asciiString = new string(asciiChars);

                return asciiString;
            }
        }

        /// <summary>
        /// If the resource requested by the client is still valid based on its modification date and the 
        /// date sent by the client (in the request header), this method sets 304 (not modified) as 
        /// the response status and optionally physically ends the response.
        /// </summary>
        /// <param name="lastModificationDate">Last modification date of the accessed resource.</param>
        public bool EndResponseForClientCache(DateTime lastModificationDate)
        {
            //  http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html
            //  14.25 If-Modified-Since
            //  14.29 Last-Modified

            if (IsClientCached(lastModificationDate))
            {
                _context.Response.StatusCode = 304;

                return true;
            }

            SetCacheControlHeaders(lastModified: lastModificationDate);

            return false;
        }

        /// <summary>
        /// Gets the appropriate cache header from the portal settings determined by the given path. 
        /// Settings can be different based on content type or extension. A setting is a match for
        /// the provided parameters if all the parameters match the setting criteria.
        /// </summary>
        /// <param name="path">Context path. If it is empty, the caller gets the global setting.</param>
        /// <param name="contentType">Content type name. Can be empty.</param>
        /// <param name="extension">Extension (e.g. js) to load settings for. Can be empty.</param>
        /// <returns>The found MaxAge setting or null.</returns>
        public int? GetCacheHeaderSetting(string path, string contentType, string extension = null)
        {
            if (extension == null)
                extension = Path.GetExtension(path)?.ToLower().Trim(' ', '.');
            
            var cacheHeaderSettings = Settings.GetValue<IEnumerable<CacheHeaderSetting>>(
                PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_CACHEHEADERS, path);
            if (cacheHeaderSettings == null)
                return null;

            foreach (var chs in cacheHeaderSettings)
            {
                // Check if one of the criterias does not match. Empty extension or content type
                // will not match if these criterias are provided explicitly in the setting.
                var extMismatch = !string.IsNullOrEmpty(chs.Extension) && chs.Extension != extension;
                var contentTypeMismatch = !string.IsNullOrEmpty(chs.ContentType) && chs.ContentType != contentType;
                var pathMismatch = !string.IsNullOrEmpty(chs.Path) && !path.StartsWith(RepositoryPath.Combine(chs.Path, RepositoryPath.PathSeparator));

                if (extMismatch || pathMismatch || contentTypeMismatch)
                    continue;

                // found a match
                return chs.MaxAge;
            }

            return null;
        }
    }
}
