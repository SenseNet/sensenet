namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines a periodically called task
    /// </summary>
    public interface IMaintenanceTask
    {
        /// <summary>
        /// Gets a calling period in minutes.
        /// </summary>
        double WaitingMinutes { get; }

        /// <summary>
        /// Executes the task.
        /// </summary>
        void Execute();
    }
}
