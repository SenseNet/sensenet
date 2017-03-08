namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines an API for general transaction participants that can extend the transactional
    /// behavior of the Content Repository.
    /// </summary>
    public interface ITransactionParticipant
    {
        /// <summary>
        /// Commits the operation represented by this participant.
        /// </summary>
        void Commit();
        /// <summary>
        /// Rolls back the operation represented by this participant.
        /// </summary>
        void Rollback();
    }
}
