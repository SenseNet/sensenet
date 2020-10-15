using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;

namespace SenseNet.Communication.Messaging
{
    /// <summary>
    /// Allows an activity to be executed first in the local AppDomain
    /// then via cluster messaging on the cluster members.
    /// </summary>
    [Serializable]
    public abstract class DistributedAction : ClusterMessage
    {
        /// <summary>
        /// Executes the activity's main action and distributes it to the other app domains in the cluster.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await DoActionAsync(false, true, cancellationToken).ConfigureAwait(false);

                SnTrace.Messaging.Write("Execute DistributedAction: {0}", this);
            }
            finally
            {
                try
                {
                    await DistributeAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exc2) // logged
                {
                    SnLog.WriteException(exc2);
                }
            }
        }

        /// <summary>
        /// Executes the activity's main action.
        /// </summary>
        /// <param name="onRemote">True if the caller is a message receiver.</param>
        /// <param name="isFromMe">True if the source of the activity is in the current appDomain.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken);

        /// <summary>
        /// Distributes the activity to the other app domains in the cluster.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public virtual async Task DistributeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await DistributedApplication.ClusterChannel.SendAsync(this, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exc) // logged
            {
                SnLog.WriteException(exc);
            }
        }
    }

    [Serializable]
    public class DebugMessage : ClusterMessage
    {
        public string Message { get; set; }
        public override string ToString()
        {
            return "DebugMessage: " + Message;
        }

        public static Task SendAsync(string message, CancellationToken cancellationToken)
        {
            var dm = new DebugMessage { Message = message };
            return dm.SendAsync(cancellationToken);
        }
    }

    [Serializable]
    public sealed class PingMessage : DebugMessage
    {
        public readonly Guid Id;
        public string[] NotResponsiveChannels { get; private set; }
        public PingMessage(string[] notResponsiveChannels = null)
        {
            NotResponsiveChannels = notResponsiveChannels == null ? new string[0] : notResponsiveChannels;
            Message = "PING";
            Id = Guid.NewGuid();
        }
    }

    [Serializable]
    public sealed class PongMessage : DebugMessage
    {
        public Guid PingId;
        public PongMessage()
        {
            Message = "PONG";
        }
    }

    [Serializable]
    public sealed class WakeUp : DebugMessage
    {
        public string Target { get; private set; }

        public WakeUp(string target)
        {
            Target = target;
            Message = "WAKEUP";
        }
    }

}