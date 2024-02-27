using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.OData;

public interface IODataControllerFactory
{
    Type GetControllerType(string controllerName);
    ODataController CreateController(string controllerName);
    void Initialize();
}

/// <summary>
/// Singleton service for creating an ODataController by the registered name
/// </summary>
public class ODataControllerFactory : IODataControllerFactory
{
    private readonly IServiceProvider _services;
    private ReadOnlyDictionary<string, Type> _controllerTypes;

    public ODataControllerFactory(IServiceProvider serviceProvider)
    {
        _services = serviceProvider;
    }


    public void Initialize()
    {
        _controllerTypes = new ReadOnlyDictionary<string, Type>(LoadRegistrations());
        var logger = _services.GetRequiredService<ILogger<ODataControllerFactory>>();
        logger.LogInformation($"ODataControllerFactory initialized. Controller names and types: " +
            $"{string.Join(", ", _controllerTypes.Select(x => $"'{x.Key}': {x.Value?.FullName}"))}.");
    }
    private IDictionary<string, Type> LoadRegistrations()
    {
        var registration = _services.GetServices<ODataControllerRegistration>();
        var types = new Dictionary<string, Type>();
        foreach (var item in registration)
        {
            types[item.Name.Trim().ToLowerInvariant()] = item.Type;

            var methods = item.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
                OperationCenter.AddMethod(method, item.Name);
        }
        return types;
    }

    public Type GetControllerType(string controllerName)
    {
        return _controllerTypes.TryGetValue(controllerName.ToLowerInvariant(), out var controllerType)
            ? controllerType :
            null;
    }

    public ODataController CreateController(string controllerName)
    {
        if (!_controllerTypes.TryGetValue(controllerName.ToLowerInvariant(), out var controllerType))
            throw new InvalidOperationException($"Controller not found: {controllerName}");
        var controller = (ODataController)_services.GetService(controllerType);
        return controller;
    }

}

public class ODataController
{
    public ODataRequest ODataRequest { get; internal set; }
    public HttpContext HttpContext { get; internal set; }
    public Content Content { get; internal set; }
}