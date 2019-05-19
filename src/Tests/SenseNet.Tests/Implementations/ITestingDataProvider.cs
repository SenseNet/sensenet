using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.Tests.Implementations
{
    public interface ITestingDataProviderExtension : IDataProviderExtension
    {
        void InitializeForTests();
        string GetSecurityControlStringForTests();
        int GetPermissionLogEntriesCountAfterMoment(DateTime moment);
        AuditLogEntry[] LoadLastAuditLogEntries(int count);
        void CheckScript(string commandText);
        IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForOneNodeIntegrityCheck(string path, Int32[] excludedNodeTypeIds);
        IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForRecursiveIntegrityCheck(string path, Int32[] excludedNodeTypeIds);
        int GetLastNodeId();

        void SetContentHandler(string contentTypeName, string handler);
        void AddField(string contentTypeName, string fieldName, string fieldType = null, string fieldHandler = null);
    }
}
