using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.Replication;
using STT = System.Threading.Tasks;

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

        options.Initialize();

#pragma warning disable CS4014 // This call is not awaited, execution of the current method continues before the call is completed.
        replicationService.ReplicateNodeAsync(content.ContentHandler, targetContent.ContentHandler, options, CancellationToken.None);
#pragma warning restore CS4014
    }
}
