﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryTestingDataProvider : ITestingDataProviderExtension
    {
        private DataProvider _mainProvider; //DB:ok
        public DataProvider MainProvider => _mainProvider ?? (_mainProvider = DataProvider.Instance); //DB:ok

        // ReSharper disable once InconsistentNaming
        public InMemoryDataProvider.Database DB => ((InMemoryDataProvider) MainProvider).DB;

        public void InitializeForTests()
        {
            // do nothing
        }

        public string GetSecurityControlStringForTests()
        {
            throw new NotImplementedException();
        }

        public int GetPermissionLogEntriesCountAfterMoment(DateTime moment)
        {
            return DB.LogEntries.Count(x => x.Title == "PermissionChanged" && x.LogDate >= moment);
        }

        public AuditLogEntry[] LoadLastAuditLogEntries(int count)
        {
            return DB.LogEntries
                .OrderByDescending(e => e.LogId)
                .Take(count)
                .OrderBy(e => e.LogId)
                .Select(e => new AuditLogEntry
                {
                    Id = e.LogId,
                    EventId = e.EventId,
                    Title = e.Title,
                    ContentId = e.ContentId,
                    ContentPath = e.ContentPath,
                    UserName = e.UserName,
                    LogDate = new DateTime(e.LogDate.Ticks),
                    Message = e.Message,
                    FormattedMessage = e.FormattedMessage
                })
                .ToArray();
        }

        public void CheckScript(string commandText)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public int GetLastNodeId()
        {
            return DB.Nodes.Count == 0 ? 0 : DB.Nodes.Max(n => n.NodeId);
        }

        public virtual string TestMethodThatIsNotInterfaceMember(string input)
        {
            return input + input;
        }
    }
}
