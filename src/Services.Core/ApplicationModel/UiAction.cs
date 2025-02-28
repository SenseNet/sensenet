using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository;
using SenseNet.Tools;
using static QRCoder.PayloadGenerator.ShadowSocksConfig;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel;

// ReSharper disable once InconsistentNaming
public class UIAction : ClientAction
{
    public Task<object> ExecuteGetAsync(Content content, HttpContext httpContext, params object[] parameters)
    {
        var app = (Operation) this.GetApplication();
        return Task.FromResult((object)app.UIDescriptor);
    }
    public async Task<object> ExecutePostAsync(Content content, HttpContext httpContext, params object[] parameters)
    {
        var app = (Operation)this.GetApplication();
        if (app.ClassName == null && app.MethodName == null)
            return await ExecuteAsync(content, httpContext, parameters);
        return await ExecuteClassAndMethodAsync(content, httpContext, parameters);
    }

    private async Task<object> ExecuteClassAndMethodAsync(Content content, HttpContext httpContext, object[] parameters)
    {
        var app = (Operation)GetApplication();
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
            ? await InvokeAsync(method, prmValues)
            : method.Invoke(null, prmValues);

        return result;
    }
    private async Task<object> InvokeAsync(MethodInfo method, object[] parameters)
    {
        var invokeResult = method.Invoke(null, parameters);
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
}