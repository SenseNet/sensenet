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
    public IDictionary<string, Type> ControllerTypes { get; }
    ODataController CreateController(string controllerName);
}

/// <summary>
/// Singleton service for creating an ODataController by the registered name
/// </summary>
public class ODataControllerFactory : IODataControllerFactory
{
    private readonly IServiceProvider _services;
    // ReSharper disable once InconsistentNaming
    private ReadOnlyDictionary<string, Type> __controllerTypes;

    public ODataControllerFactory(IServiceProvider serviceProvider)
    {
        _services = serviceProvider;
    }

    private static readonly object LoaderLock = new();
    public IDictionary<string, Type> ControllerTypes
    {
        get
        {
            if (__controllerTypes == null)
            {
                lock (LoaderLock)
                {
                    if (__controllerTypes == null)
                    {
                        __controllerTypes = new ReadOnlyDictionary<string, Type>(LoadRegistrations());
                        var logger = _services.GetRequiredService<ILogger<ODataControllerFactory>>();
                        logger.LogInformation($"ODataControllers discovered. Names and types: " +
                            $"{string.Join(", ", __controllerTypes.Select(x => $"'.{x.Key}': {x.Value.GetType().FullName}"))}");
                    }
                }
            }
            return __controllerTypes;
        }
    }

    private IDictionary<string, Type> LoadRegistrations()
    {
        var registration = _services.GetServices<ODataControllerRegistration>();
        var types = new Dictionary<string, Type>();
        foreach (var item in registration)
        {
            types[item.Name.Trim().ToLowerInvariant()] = item.Type;

//UNDONE:yOdataController: Create method registrations: 
//var method = typeof(TestODataController2).GetMethod("GetData");
//OperationCenter.AddMethod(method, controllerName);

        }
        return types;
    }

    public ODataController CreateController(string controllerName)
    {
        if (!ControllerTypes.TryGetValue(controllerName.ToLowerInvariant(), out var controllerType))
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