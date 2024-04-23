using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;

namespace SenseNet.OData;

/// <summary>
/// Base class for OData controllers.
/// </summary>
public class ODataController
{
    /// <summary>
    /// Gets the current OData request.
    /// </summary>
    public ODataRequest ODataRequest { get; internal set; }
    /// <summary>
    /// Gets the current HTTP context.
    /// </summary>
    public HttpContext HttpContext { get; internal set; }
    /// <summary>
    /// Gets the current content.
    /// </summary>
    public Content Content { get; internal set; }
}