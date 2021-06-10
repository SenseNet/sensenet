﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Data;
using SenseNet.Diagnostics;

namespace SenseNet.Tests.Core.Implementations
{
    /// <summary>
    /// Defines helper data methods necessary for tests to run.
    /// </summary>
    public interface ITestingDataProviderExtension : IDataProviderExtension
    {
        void InitializeForTests();
        string GetSecurityControlStringForTests();
        int GetPermissionLogEntriesCountAfterMoment(DateTime moment);
        AuditLogEntry[] LoadLastAuditLogEntries(int count);
        void CheckScript(string commandText);
        IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds);
        IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds);
        Task<int> GetLastNodeIdAsync();

        void SetContentHandler(string contentTypeName, string handler);
        void AddField(string contentTypeName, string fieldName, string fieldType = null, string fieldHandler = null);

        Task<int[]> GetChildNodeIdsByParentNodeIdAsync(int parentNodeId);
        Task<NodeHeadData> GetNodeHeadDataAsync(int nodeId);
        Task<VersionData> GetVersionDataAsync(int versionId);
        Task<int> GetBinaryPropertyCountAsync(string path);
        Task<int> GetFileCountAsync(string path);
        Task<int> GetLongTextCountAsync(string path);

        Task<long> GetAllFileSize();
        Task<long> GetAllFileSizeInSubtree(string path);
        Task<long> GetFileSize(string path);

        Task<object> GetPropertyValueAsync(int versionId, string name);
        Task UpdateDynamicPropertyAsync(int versionId, string name, object value);

        Task SetFileStagingAsync(int fileId, bool staging);
        Task DeleteFileAsync(int fileId);

        Task EnsureOneUnlockedSchemaLockAsync();

        DateTime GetSharedLockCreationDate(int nodeId);
        void SetSharedLockCreationDate(int nodeId, DateTime value);

        DataProvider CreateCannotCommitDataProvider(DataProvider mainDataProvider);

        Task DeleteAllStatisticalDataAsync(IStatisticalDataProvider dataProvider);
        Task<IEnumerable<IStatisticalDataRecord>> LoadAllStatisticalDataRecords(IStatisticalDataProvider dataProvider);
        Task<IEnumerable<Aggregation>> LoadAllStatisticalDataAggregations(IStatisticalDataProvider dataProvider);
    }
}
