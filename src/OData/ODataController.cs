using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;

namespace SenseNet.OData;

public interface IODataControllerResolver
{
    ODataController ResolveController(string controllerName);
}

public class ODataControllerResolver : IODataControllerResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ODataControllerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ODataController ResolveController(string controllerName)
    {
        var typeName = $"{controllerName}";
        //var typeName = $"SenseNet.ODataTests.{controllerName}";
        //var typeName = $"SenseNet.ODataTests.{controllerName}, SenseNet.ODataTests";
        var controllerType = Type.GetType(typeName,
            assemblyName =>
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => x.GetName().Name == assemblyName.Name);
                return asm;
            },
            (asm, typeName, b) =>
            {
                if (asm != null)
                {
                    var type = asm.GetTypes().FirstOrDefault(x => x.FullName == typeName);
                    return type;
                }

                var typeSet = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes().Where(t => t.BaseType == typeof(ODataController)));
                var types = typeName.Contains('.')
                    ? typeSet.Where(x => x.FullName == typeName).ToArray()
                    : typeSet.Where(x => x.Name == typeName).ToArray();
                if (types.Length == 0)
                    throw new MissingMethodException(controllerName);
                if (types.Length > 1)
                    throw new AmbiguousMatchException(
                        $"Ambiguous call: {string.Join(", ", types.Select(t => t.AssemblyQualifiedName))}");
                return types[0];

            });
        var controller = (ODataController)_serviceProvider.GetService(controllerType);
        return controller;
    }
}

public class ODataController
{
    public ODataRequest ODataRequest { get; internal set; }
    public HttpContext HttpContext { get; internal set; }
    public Content Content { get; internal set; }
}