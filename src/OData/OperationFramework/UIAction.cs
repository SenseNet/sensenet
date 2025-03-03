using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.OData;
using SenseNet.Tools;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.OperationFramework;

// ReSharper disable once InconsistentNaming
public class UIAction : ClientAction
{
    public virtual Task<object> ExecuteGetAsync(Content content, HttpContext httpContext, params object[] parameters)
    {
        var app = (Operation) this.GetApplication();
        return Task.FromResult((object)app.UIDescriptor);
    }
    public async Task<object> ExecutePostAsync(Content content, HttpContext httpContext, params object[] parameters)
    {
        var operation = (Operation)this.GetApplication();
        if (operation.ClassName == null || operation.MethodName == null)
            return await ExecuteAsync(content, httpContext, parameters);

        var controllerName = operation.ClassName;
        if (controllerName == null)
            return null;

        var resolver = httpContext.RequestServices.GetRequiredService<IODataControllerFactory>();
        var controllerType = resolver.GetControllerType(controllerName);
        if (controllerType != null)
        {
            var controller = resolver.CreateController(controllerName); 
            controller.Content = Content;
            controller.HttpContext = httpContext;
            controller.ODataRequest = httpContext.GetODataRequest();
            return await ExecuteControllerMethodAsync(controller, operation, content, httpContext, parameters);
        }

        return await ExecuteClassMethodAsync(operation, content, httpContext, parameters);
    }


    public virtual Task<object> ExecuteAsync(Content content, HttpContext httpContext, object[] parameters)
    {
        var message = $"{this.GetType().FullName}.ExecuteAsync called. Parameters: ";
        for (int i = 0; i < this.ParamNames.Length; i++)
        {
            if (i > 0)
                message += ", ";
            message += $"{ParamNames[i]} = {parameters[i]}";
        }

        var result = new { message = message };
        return Task.FromResult((object)result);
    }


    private async Task<object> ExecuteControllerMethodAsync(ODataController controller, Operation app, Content content, HttpContext httpContext, object[] parameters)
    {
        var type = controller.GetType();

        var method = type.GetMethod(app.MethodName, ParamTypes);
        if (method == null)
            throw new InvalidOperationException("Unknown method: " + app.MethodName);

        var isAsync = ParseSynchronicity(method);

        var result = isAsync
            ? await InvokeAsync(controller, method, parameters)
            : method.Invoke(controller, parameters);

        return result;
    }
    private async Task<object> ExecuteClassMethodAsync(Operation app, Content content, HttpContext httpContext, object[] parameters)
    {
        var type = TypeResolver.GetType(app.ClassName, false);
        if (type == null)
            throw new InvalidOperationException("Unknown type: " + app.ClassName);

        var prmTypes = new Type[ParamTypes.Length + 2];
        prmTypes[0] = typeof(Content);
        prmTypes[1] = typeof(HttpContext);

        Array.Copy(ParamTypes, 0, prmTypes, 2, ParamTypes.Length);

        var method = type.GetMethod(app.MethodName, prmTypes);
        if (method == null)
            throw new InvalidOperationException("Unknown method: " + app.MethodName);

        var prmValues = new object[prmTypes.Length];
        prmValues[0] = content;
        prmValues[1] = httpContext;

        Array.Copy(parameters, 0, prmValues, 2, parameters.Length);

        var isAsync = ParseSynchronicity(method);

        var result = isAsync
            ? await InvokeAsync(null, method, prmValues)
            : method.Invoke(null, prmValues);

        return result;
    }


    private async Task<object> InvokeAsync(object target, MethodInfo method, object[] parameters)
    {
        var invokeResult = method.Invoke(target, parameters);
        var invokeResultType = invokeResult.GetType();

        var awaitable = (Task)invokeResult;
        await awaitable;

        if (invokeResultType.IsGenericType)
        {
            // It is impossible to convert to the target type (Task<??>) so getting result with reflection. 
            var resultProperty = invokeResultType.GetProperty("Result");
            var result = resultProperty?.GetValue(awaitable);
            return result;
        }

        // Non-generic Task have no result.
        return null;
    }
    private bool ParseSynchronicity(MethodBase methodBase)
    {
        var isAsyncVoid = false;

        if (!(methodBase is MethodInfo method))
            return false;

        isAsyncVoid = method.ReturnType == typeof(Task);

        return isAsyncVoid || method.ReturnType.BaseType == typeof(Task);
    }
}