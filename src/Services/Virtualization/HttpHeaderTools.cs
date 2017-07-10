using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Portal.Virtualization
{
    public static class HttpHeaderTools
    {
        private static readonly string HEADER_CONTENTDISPOSITION_NAME = "Content-Disposition";
        private static readonly string HEADER_CONTENTDISPOSITION_VALUE = "Attachment";
        private static readonly string HEADER_ACESSCONTROL_ALLOWORIGIN_NAME = "Access-Control-Allow-Origin";
        private static readonly string HEADER_ACESSCONTROL_ALLOWCREDENTIALS_NAME = "Access-Control-Allow-Credentials";
        private static readonly string ACCESS_CONTROL_ALLOW_METHODS_NAME = "Access-Control-Allow-Methods";
        private static readonly string ACCESS_CONTROL_ALLOW_HEADERS_NAME = "Access-Control-Allow-Headers";
        private static readonly string HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL = "*";
        private static readonly string HEADER_ACESSCONTROL_ORIGIN_NAME = "Origin";

        private static readonly string[] ACCESS_CONTROL_ALLOW_METHODS_DEFAULT =
        {
            "GET", "POST", "PATCH", "DELETE", "MERGE", "PUT"
        };
        private static readonly string[] ACCESS_CONTROL_ALLOW_HEADERS_DEFAULT =
        {
            "X-Authentication-Type",
            "X-Refresh-Data", "X-Access-Data",
            "X-Requested-With", "Authorization", "Content-Type"
        };

        private delegate void PurgeDelegate(IEnumerable<string> urls);


        // ============================================================================================ Private methods
        private static bool IsClientCached(DateTime contentModified)
        {
            var modifiedSinceHeader = HttpContext.Current.Request.Headers["If-Modified-Since"];
            if (modifiedSinceHeader != null)
            {
                DateTime isModifiedSince;
                if (DateTime.TryParse(modifiedSinceHeader, out isModifiedSince))
                {
                    isModifiedSince = isModifiedSince.ToUniversalTime();
                    return isModifiedSince - contentModified > TimeSpan.FromSeconds(-1);    // contentModified is more precise
                }
            }
            return false;
        }
        private static string[] PurgeUrlFromProxy(string url, bool async)
        {
            // PURGE /contentem/maicontent.jpg HTTP/1.1
            // Host: myhost.hu

            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");

            if (WebApplication.ProxyIPs.Count == 0)
                return null;

            string contentPath;
            string host;

            var slashIndex = url.IndexOf("/");
            if (slashIndex >= 0)
            {
                contentPath = url.Substring(slashIndex);
                host = url.Substring(0, slashIndex);
            }
            else
            {
                contentPath = "/";
                host = url;
            }

            if (string.IsNullOrEmpty(host) && HttpContext.Current != null)
                host = HttpContext.Current.Request.Url.Host;

            string[] result = null;
            if (!async)
                result = new string[WebApplication.ProxyIPs.Count];

            var proxyIndex = 0;
            foreach (var proxyIP in WebApplication.ProxyIPs)
            {
                var proxyUrl = string.Concat("http://", proxyIP, contentPath);

                try
                {
                    var request = WebRequest.Create(proxyUrl) as HttpWebRequest;
                    if (request == null)
                        break;

                    request.Method = "PURGE";
                    request.Host = host;

                    if (!async)
                    {
                        using (request.GetResponse())
                        {
                            // we do not need to read the request here, just the status code
                            result[proxyIndex] = "OK";
                        }
                    }
                    else
                    {
                        request.BeginGetResponse(null, null);
                    }
                }
                catch (WebException wex)
                {
                    var wr = wex.Response as HttpWebResponse;
                    if (wr != null && !async)
                    {
                        switch (wr.StatusCode)
                        {
                            case HttpStatusCode.NotFound:
                                result[proxyIndex] = "MISS";
                                break;
                            case HttpStatusCode.OK:
                                result[proxyIndex] = "OK";
                                break;
                            default:
                                SnLog.WriteException(wex);
                                result[proxyIndex] = wex.Message;
                                break;
                        }
                    }
                    else
                    {
                        SnLog.WriteException(wex);
                        if (!async)
                            result[proxyIndex] = wex.Message;
                    }
                }

                proxyIndex++;
            }

            return result;
        }
        private static void PurgeUrlsFromProxyAsyncWithDelay(IEnumerable<string> urls) 
        {
            if (WebApplication.PurgeUrlDelayInMilliSeconds > 0)
            {
                Thread.Sleep(WebApplication.PurgeUrlDelayInMilliSeconds);
            }
            var distinctUrls = urls.Distinct().Where(url => !string.IsNullOrEmpty(url));
            foreach (var url in distinctUrls)
            {
                PurgeUrlFromProxyAsync(url);
            }
        }


        // ============================================================================================ Public methods
        public static void SetCacheControlHeaders(int cacheForSeconds)
        {
            SetCacheControlHeaders(cacheForSeconds, HttpCacheability.Public);
        }
        public static void SetCacheControlHeaders(int cacheForSeconds, HttpCacheability httpCacheability)
        {
            HttpContext.Current.Response.Cache.SetCacheability(httpCacheability);
            HttpContext.Current.Response.Cache.SetMaxAge(new TimeSpan(0, 0, cacheForSeconds));
            HttpContext.Current.Response.Cache.SetSlidingExpiration(true);  // max-age does not appear in response header without this...
        }
        public static void SetCacheControlHeaders(HttpCacheability? httpCacheability = null, DateTime? lastModified = null, TimeSpan? maxAge = null)
        {
            var cache = HttpContext.Current.Response.Cache;

            try
            {
                if (httpCacheability.HasValue)
                {
                    cache.SetCacheability(httpCacheability.Value);
                }

                if (lastModified.HasValue)
                {
                    var t = lastModified.Value;
                    if (t > DateTime.UtcNow)
                        t = DateTime.UtcNow;
                    cache.SetLastModified(t);
                }

                if (maxAge.HasValue)
                {
                    // max-age does not appear in response header without this
                    cache.SetMaxAge(maxAge.Value);
                    cache.SetSlidingExpiration(true);
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex,
                    $"Exception in SetCacheControlHeaders. Parameter values: httpCacheability:'{httpCacheability}' lastModified:'{lastModified}' maxAge:'{maxAge}'",
                    EventId.Portal);
            }
        }

        public static void SetContentDispositionHeader(string fileName)
        {
            if (HttpContext.Current == null)
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

            // cannot use AppendHeader, because there must be only one header entry with this name
            HttpContext.Current.Response.Headers.Set(HEADER_CONTENTDISPOSITION_NAME, cdHeader);
        }

        /// <summary>
        /// Set Cross-Origin Request Sharing (CORS) headers.
        /// </summary>
        /// <param name="domain">The domain that will be written to the response as allowed origin.</param>
        public static void SetAccessControlHeaders(string domain = null)
        {
            // Set headers only in a real-world environment, not in case of test/mock requests.
            if (HttpContext.Current == null || !HttpRuntime.UsingIntegratedPipeline)
                return;

            // Use the current domain if it was not provided by the caller.
            var allowedDomain = string.IsNullOrEmpty(domain)
                ? HttpContext.Current.Request.Url.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped)
                : domain;

            // Set the allowed origin. This will prevent unauthorized external sites from
            // accessing this resource from the client side (using Javascript ajax request).
            HttpContext.Current.Response.Headers.Set(HEADER_ACESSCONTROL_ALLOWORIGIN_NAME, allowedDomain);

            // Set Credentials header only if the domain is a real one, not a wildcard ('*').
            // FUTURE: set this header based on a more granular setting (by domain?)
            if (allowedDomain != HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL)
                HttpContext.Current.Response.Headers.Set(HEADER_ACESSCONTROL_ALLOWCREDENTIALS_NAME, "true");
        }

        /// <summary>
        /// Sets the Access-Control-Allow-Methods and Access-Control-Allow-Headers headers 
        /// in a response of an OPTIONS request.
        /// </summary>
        /// <param name="httpVerbs">List of the allowed HTTP verbs. For example: "GET", "POST". 
        /// If set to null, a global setting is used.</param>
        /// <param name="httpHeaders">List of the allowed HTTP headers. For example: "Content-Type", "Authentication". 
        /// If set to null, a global setting is used.</param>
        public static void SetPreflightResponse(string[] httpVerbs = null, string[] httpHeaders = null)
        {
            HttpContext.Current.Response.Headers.Set(ACCESS_CONTROL_ALLOW_METHODS_NAME, string.Join(", ", 
                httpVerbs ?? Settings.GetValue(PortalSettings.SETTINGSNAME,
                PortalSettings.SETTINGS_ALLOWEDMETHODS, null,
                ACCESS_CONTROL_ALLOW_METHODS_DEFAULT)));

            HttpContext.Current.Response.Headers.Set(ACCESS_CONTROL_ALLOW_HEADERS_NAME, string.Join(", ", 
                httpHeaders ?? Settings.GetValue(PortalSettings.SETTINGSNAME, 
                PortalSettings.SETTINGS_ALLOWEDHEADERS, null, 
                ACCESS_CONTROL_ALLOW_HEADERS_DEFAULT)));
        }

        /// <summary>
        /// Check if the origin header sent by the client is a known domain. It has to be the 
        /// same that the request was sent to, OR it has to be among the whitelisted external
        /// domains that are allowed to access the Content Repository.
        /// </summary>
        public static bool TrySetAllowedOriginHeader()
        {
            if (HttpContext.Current == null)
                return true;
            
            // Get the Origin header from the request, if it was sent by the browser.
            // Command-line tools or local html files will not send this.
            var originHeader = HttpContext.Current.Request.Headers[HEADER_ACESSCONTROL_ORIGIN_NAME];
            if (string.IsNullOrEmpty(originHeader) || string.Compare(originHeader, "null", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                SetAccessControlHeaders();
                return true;
            }

            // We compare only the domain parts of the two urls, because interim servers
            // may change the scheme and port of the url (e.g. from https to http).
            var currentDomain = HttpContext.Current.Request.Url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
            var originDomain = string.Empty;
            var error = false;

            try
            {
                var origin = new Uri(originHeader.Trim(' '));
                originDomain = origin.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
            }
            catch (Exception)
            {
                SnLog.WriteWarning("Unknown or incorrectly formatted origin header: " + originHeader, EventId.Portal);
                error = true;
            }

            if (!error)
            {
                // check if the request arrived from an external domain
                if (string.Compare(currentDomain, originDomain, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    // We allow requests from external domains only if they are registered in this whitelist.
                    var corsDomains = Settings.GetValue<IEnumerable<string>>(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_ALLOWEDORIGINDOMAINS, 
                        PortalContext.Current.ContextNodePath, new string[0]);

                    // try to find the domain in the whitelist (or the '*')
                    var externalDomain = corsDomains.FirstOrDefault( d => d == HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL ||
                        string.Compare(d, originDomain, StringComparison.InvariantCultureIgnoreCase) == 0);

                    if (!string.IsNullOrEmpty(externalDomain))
                    {
                        // Set the desired domain as allowed (or '*' if it is among the whitelisted domains). We cannot use 
                        // the value from the whitelist (e.g. 'example.com'), because the browser expects the full origin 
                        // (with schema and port, e.g. 'http://example.com:80').
                        SetAccessControlHeaders(externalDomain == HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL ? HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL : originHeader);
                        return true;
                    }

                    // not found in the whitelist
                    error = true;
                }
            }

            SetAccessControlHeaders();

            return !error;
        }

        /// <summary>
        /// If the resource requested by the client is still valid based on its modification date and the 
        /// date sent by the client (in the request header), this method sets 304 (not modified) as 
        /// the response status and optionally phisically ends the response.
        /// </summary>
        /// <param name="lastModificationDate">Last modification date of the accessed resource.</param>
        /// <param name="endResponse">Whether to actually end the response in case the client cache is still valid.</param>
        public static void EndResponseForClientCache(DateTime lastModificationDate, bool endResponse = true)
        {
            //  http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html
            //  14.25 If-Modified-Since
            //  14.29 Last-Modified

            var context = HttpContext.Current;
            if (IsClientCached(lastModificationDate))
            {
                context.Response.StatusCode = 304;
                context.Response.SuppressContent = true;

                if (endResponse)
                {
                    context.Response.Flush();
                    context.Response.End();
                    // thread exits here
                }
            }
            else
            {
                // make sure that the date is in the past
                var localDate = DateTime.Compare(lastModificationDate, DateTime.UtcNow) <= 0 ? lastModificationDate : DateTime.UtcNow;

                context.Response.Cache.SetLastModified(localDate);
            }
        }

        /// <summary>
        /// Gets the appropriate cache header from the portal settings determined by the given path. 
        /// Settings can be different based on content type or extension. A setting is a match for
        /// the provided parameters if all the parameters match the setting criteria.
        /// </summary>
        /// <param name="path">Context path. If it is empty, the caller gets the global setting.</param>
        /// <param name="contentType">Content type name. Can be empty.</param>
        /// <param name="extension">Extension (e.g. js) to load settings for. Can be empty.</param>
        /// <returns></returns>
        public static int? GetCacheHeaderSetting(string path, string contentType, string extension)
        {
            var cacheHeaderSettings = Settings.GetValue<IEnumerable<CacheHeaderSetting>>(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_CACHEHEADERS, path);
            if (cacheHeaderSettings == null)
                return null;

            foreach (var chs in cacheHeaderSettings)
            {
                // Check if one of the criterias does not match. Empty extension or content type
                // will not match if these criterias are provided explicitely in the setting.
                var extMismatch = !string.IsNullOrEmpty(chs.Extension) && chs.Extension != extension;
                var contentTypeMismach = !string.IsNullOrEmpty(chs.ContentType) && chs.ContentType != contentType;
                var pathMismatch = !string.IsNullOrEmpty(chs.Path) && !path.StartsWith(RepositoryPath.Combine(chs.Path, RepositoryPath.PathSeparator));

                if (extMismatch || pathMismatch || contentTypeMismach)
                    continue;

                // found a match
                return chs.MaxAge;
            }

            return null;
        }

        /// <summary>
        /// Sends a PURGE request to all of the configured proxy servers for the given urls. Purge requests are synchronous.
        /// </summary>
        /// <param name="urls">Urls of the content that needs to be purged. The urls must start with the host name (e.g. www.example.com/mycontent/myimage.jpg).</param>
        /// <returns>A Dictionary with the given urls and the purge result Dictionaries.</returns>
        public static Dictionary<string, string[]> PurgeUrlsFromProxy(IEnumerable<string> urls)
        {
            return urls.Distinct().Where(url => !string.IsNullOrEmpty(url)).ToDictionary(url => url, PurgeUrlFromProxy);
        }

        /// <summary>
        /// Sends a PURGE request to all of the configured proxy servers for the given url. Purge request is synchronous and result is processed.
        /// </summary>
        /// <param name="url">Url of the content that needs to be purged. It must start with the host name (e.g. www.example.com/mycontent/myimage.jpg).</param>
        /// <returns>A Dictionary with the result of each proxy request. Possible values: OK, MISS, {error message}.</returns>
        public static string[] PurgeUrlFromProxy(string url)
        {
            return PurgeUrlFromProxy(url, false);
        }

        /// <summary>
        /// Sends a PURGE request to all of the configured proxy servers for the given url. Purge request is asynchronous and result is not processed.
        /// </summary>
        /// <param name="url">Url of the content that needs to be purged. It must start with the host name (e.g. www.example.com/mycontent/myimage.jpg).</param>
        public static void PurgeUrlFromProxyAsync(string url)
        {
            PurgeUrlFromProxy(url, true);
        }

        /// <summary>
        /// Starts an async thread that will start purging urls after a specified delay. Delay is configured with PurgeUrlDelayInSeconds key in web.config.
        /// </summary>
        /// <param name="urls"></param>
        public static void BeginPurgeUrlsFromProxyWithDelay(IEnumerable<string> urls)
        {
            var purgeDelegate = new PurgeDelegate(PurgeUrlsFromProxyAsyncWithDelay);
            purgeDelegate.BeginInvoke(urls, null, null);
        }
    }
}
