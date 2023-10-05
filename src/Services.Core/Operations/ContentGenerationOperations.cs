using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data.Replication;
using STT = System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Schema;
using Microsoft.Extensions.Hosting;

namespace SenseNet.Services.Core.Operations;

public static class ContentGenerationOperations
{
    [ODataAction(OperationName = "Replicate")]
    [AllowedRoles(N.R.Administrators, N.R.Developers)]
    [ContentTypes(N.CT.GenericContent)]
    public static async STT.Task ReplicateAsync(Content content, HttpContext httpContext, string target, ReplicationDescriptor options)
    {
        var targetContent = await Content.LoadByIdOrPathAsync(target, httpContext.RequestAborted);
        if (targetContent == null)
            throw new ArgumentException("Target is not found by Id or Path: " + target);

        var replicationService = httpContext.RequestServices.GetService<IReplicationService>();
        if (replicationService == null)
            throw new NotSupportedException("Replication is not supported.");

        var cancel = CancellationToken.None;

        // load the app cancellation token that will shut down this background process gracefully
        var appHost = httpContext.RequestServices.GetService<IHostApplicationLifetime>();
        if (appHost != null)
            cancel = appHost.ApplicationStopping;

#pragma warning disable CS4014 // This call is not awaited, execution of the current method continues before the call is completed.
        replicationService.ReplicateNodeAsync(content.ContentHandler, targetContent.ContentHandler, options, cancel);
#pragma warning restore CS4014
    }

    [ODataFunction]
    [AllowedRoles(N.R.Administrators, N.R.Developers)]
    [ContentTypes(N.CT.GenericContent)]
    public static object GetReplicationTemplate(Content content)
    {
        var readOnlyFields = content.Fields.Where(x => x.Value.ReadOnly)
            .Select(x=>x.Key)
            .ToArray();
        var fields1 = ReplicationDescriptor.WellKnownProperties
            .Select(x=>new KeyValuePair<string,string>(x.Key, $"____DataType.{x.Value}____"));
        var fields2 = content.ContentHandler.NodeType.PropertyTypes
            .Where(x => x.DataType != DataType.Binary && x.DataType != DataType.Currency)
            .Select(x => new KeyValuePair<string, string>(x.Name, $"____DataType.{x.DataType}____"))
            ;
        var fields = fields1.Union(fields2)
            .Where(x => !readOnlyFields.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        var result = new
        {
            target = "__Id_or_Path_of_the_existing_empty_container_content__",
            options = new ReplicationDescriptor
            {
                MaxCount = 10,
                MaxItemsPerFolder = 100,
                MaxFoldersPerFolder = 100,
                FirstFolderIndex = 0,
                Fields = fields
            }
        };
        var json = JsonConvert.SerializeObject(result, Formatting.Indented);

        return json;
    }
}
