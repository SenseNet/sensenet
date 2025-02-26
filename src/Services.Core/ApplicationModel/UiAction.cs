using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel;

public class UiAction : ClientAction
{
    public Task<object> ExecuteGetAsync(Content content, CancellationToken cancel, params object[] parameters)
    {
        var app = (Operation) this.GetApplication();
        return Task.FromResult((object)app.UIDescriptor);
    }
    public Task<object> ExecutePostAsync(Content content, CancellationToken cancel, params object[] parameters)
    {
        var message = "Parameters: ";
        for (int i = 0; i < this.ParamNames.Length; i++)
        {
            if (i > 0)
                message += ", ";
            message += $"{ParamNames[i]} = {parameters[i]}";
        }

        var result = new {message = message};
        return Task.FromResult((object)result);
    }
}