using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;

namespace SenseNet.OData;

public class ODataController
{
    public ODataRequest ODataRequest { get; internal set; }
    public HttpContext HttpContext { get; internal set; }
    public Content Content { get; internal set; }
}