using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Data
{
    internal class TransactionQueue
    {
        private static readonly object DataReferencesSync = new object();
        private List<ITransactionParticipant> _dataReferences;

        public void Add(ITransactionParticipant data)
        {
            lock (DataReferencesSync)
            {
                if (_dataReferences == null)
                    _dataReferences = new List<ITransactionParticipant>();
                _dataReferences.Add(data);
            }
        }

        public void Commit()
        {
            foreach (var reference in GetCopyAndReset())
                reference.Commit();
        }

        public void Rollback()
        {
            foreach (var reference in GetCopyAndReset())
                reference.Rollback();
        }

        private ITransactionParticipant[] GetCopyAndReset()
        {
            ITransactionParticipant[] dataReferences = null;
            lock (DataReferencesSync)
            {
                if (_dataReferences == null)
                    return new ITransactionParticipant[0];
                dataReferences = _dataReferences.ToArray();
                _dataReferences = null;
            }
            return dataReferences;
        }
    }
}