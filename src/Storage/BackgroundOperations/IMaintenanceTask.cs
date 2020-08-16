using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    /// <summary>
    /// Defines a periodically called task
    /// </summary>
    public interface IMaintenanceTask
    {
        /// <summary>
        /// Gets a calling period in seconds.
        /// </summary>
        int WaitingSeconds { get; }

        /// <summary>
        /// Executes the task.
        /// This method is called periodically in every cycle. Developers may restrict execution further
        /// at the beginning of this method.
        /// </summary>
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
