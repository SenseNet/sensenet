using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IO = System.IO;
using Ionic.Zip;
using System.Diagnostics;
using System.Xml;
using StorageContext = SenseNet.ContentRepository.Storage.StorageContext;
using SenseNet.Diagnostics;
using System.Threading;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search.Lucene29
{
    [Serializable]
    public sealed class IndexBackupProgressMessage : ClusterMessage //UNDONE:!!!!!!! Delete asap.
    {
        public IndexBackupProgressType Type { get; private set; }
        public string Message { get; private set; }
        public long Value { get; private set; }
        public long MaxValue { get; private set; }
        public IndexBackupProgressMessage( IndexBackupProgressType type, string message, long value, long maxValue)
        {
            Type = type;
            Message = message;
            Value = value;
            MaxValue = maxValue;
        }
    }

    public class BackupTools
    {
        internal const string BACKUPFILENAME = "IndexBackup.zip";
        internal const string RECOVEREDFILENAME = "IndexRecovered.zip";
        internal const string COMPRESSIONROOT = "files";
        internal const string RESTOREINFOFILENAME = "RestoreInfo.xml";
        internal const string RESTOREINFOCONTENT = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RestoreInfo>
  <BackupId>{0}</BackupId>
</RestoreInfo>";

        private static readonly string _backupDirectoryPath;
        private static readonly string _zipFilePath;

        private static string IndexDirectoryPath
        {
            get { return SenseNet.ContentRepository.Storage.IndexDirectory.CurrentDirectory; }
        }
        private static string RestoreInfoPath
        {
            get { return IO.Path.Combine(IndexDirectoryPath, RESTOREINFOFILENAME); }
        }

        public static IndexBackupProgress Progress { get; private set; }

        static BackupTools()
        {
            _backupDirectoryPath = StorageContext.Search.IndexDirectoryBackupPath;
            _zipFilePath = IO.Path.Combine(_backupDirectoryPath, BACKUPFILENAME);
            Progress = new IndexBackupProgress();
        }

        /// <summary>
        /// Asynchronous backup method for GUI purposes.
        /// </summary>
        public static void BackupIndex()
        {
            EnsureEmptyDirctory(_backupDirectoryPath);

            var activity = new SenseNet.Search.Indexing.Activities.BackupActivity(Environment.MachineName, AppDomain.CurrentDomain.FriendlyName);
            activity.Distribute();
            activity.InternalExecuteIndexingActivity();
        }
        /// <summary>
        /// Synchronous backup method for console applications. 
        /// </summary>
        public static void SynchronousBackupIndex()
        {
            using (var op = SnTrace.Repository.StartOperation("Backup index immediatelly."))
            {
                EnsureEmptyDirctory(_backupDirectoryPath);
                IndexManager.PauseIndexing();

                while (!IndexManager.Paused)
                {
                    Thread.Sleep(100);
                }
                Thread.Sleep(1000);
                try
                {
                    CopyIndexToBackupDirectory();
                }
                finally
                {
                    IndexManager.ContinueIndexing();
                }

                OptimizeCompressAndStore();

                op.Successful = true;
            }
        }

        internal static void BackupIndexImmediatelly() // caller: DocumentPopulator.ClearAndPopulateAll
        {
            using (var op = SnTrace.Repository.StartOperation("Backup index immediatelly."))
            {
                EnsureEmptyDirctory(_backupDirectoryPath);
                CopyIndexToBackupDirectory();
                OptimizeCompressAndStore();
                op.Successful = true;
            }
        }
        internal static void CopyIndexToBackupDirectory() // caller: BackupActivity.Execute() (synchron)
        {
            var lockPath = StorageContext.Search.IndexLockFilePath;
            var excludedFileList = lockPath == null ? new List<string>(new[] { RestoreInfoPath }) : new List<string>(new[] { RestoreInfoPath, lockPath });
            Progress.StartCopyIndexToBackupDirectory();
            CopyDirectoryContent(IndexDirectoryPath, _backupDirectoryPath, excludedFileList);
            Progress.FinishCopyIndexToBackupDirectory();
        }
        internal static void OptimizeCompressAndStore() // caller: BackupIndexImmediatelly (synchron), BackupActivity.Execute() (asynchron)
        {
            try
            {
                OptimizeBeforeBackup(_backupDirectoryPath);
                CompressTheIndex(_zipFilePath, _backupDirectoryPath);
                var backupId = StoreIndexBackupToDb(_zipFilePath);
                SaveBackupIdToFile(backupId, RestoreInfoPath);
                DeleteUnnecessaryBackups();
                SnLog.WriteInformation("Index directory is successfully backed up.");
            }
            catch (Exception ex)
            {

                SnLog.WriteException(ex);
            }
        }

        internal static void RestoreIndex(bool force, System.IO.TextWriter consoleOut)
        {
            using (var op = SnTrace.Repository.StartOperation("Restore index."))
            {
                var recoveredZipPath = IO.Path.Combine(_backupDirectoryPath, RECOVEREDFILENAME);
                var recoveredFilesPath = IO.Path.Combine(_backupDirectoryPath, COMPRESSIONROOT);

                Guid lastIdFromDb;
                var need = NeedRestore(out lastIdFromDb);
                if (force || need)
                {
                    EnsureEmptyDirctory(_backupDirectoryPath);
                    RecoverIndexBackupFromDb(recoveredZipPath);
                    DecompressTheIndex(recoveredZipPath, _backupDirectoryPath);

                    var dir = SenseNet.ContentRepository.Storage.IndexDirectory.CreateNew();
                    MoveDirectoryContent(recoveredFilesPath, dir);
                    SaveBackupIdToFile(lastIdFromDb, IO.Path.Combine(dir, RESTOREINFOFILENAME));
                    SenseNet.ContentRepository.Storage.IndexDirectory.Reset();
                    SenseNet.ContentRepository.Storage.IndexDirectory.RemoveUnnecessaryDirectories();

                    if (consoleOut != null)
                    {
                        consoleOut.WriteLine("    Index directory is restored.");
                        consoleOut.WriteLine("        Path: {0},", IndexDirectoryPath);
                        consoleOut.WriteLine("        BackupId: {0},", lastIdFromDb);
                    }
                    SnLog.WriteInformation("Index directory is successfully restored.", EventId.Indexing, properties: new Dictionary<string, object> { { "BackupId", lastIdFromDb } });
                }
                else
                {
                    if (consoleOut != null)
                        consoleOut.WriteLine("    Index directory restoring is skipped.");
                    SnTrace.System.Write("Index directory restoring is skipped. BackupId:" + lastIdFromDb);
                }
                op.Successful = true;
            }
        }

        private static bool NeedRestore(out Guid lastIdFromDb)
        {
            lastIdFromDb = Guid.Empty;
            lastIdFromDb = SenseNet.ContentRepository.Storage.DataBackingStore.GetLastStoredBackupNumber();
            if (!IO.Directory.Exists(IndexDirectoryPath))
                return true;
            if (!IO.File.Exists(RestoreInfoPath))
                return true;
            Guid lastIdFromFile = LoadBackupIdFromFile(RestoreInfoPath);
            if (lastIdFromFile != lastIdFromDb)
                return true;
            return false;
        }
        private static Guid LoadBackupIdFromFile(string restoreInfoPath)
        {
            var xml = new XmlDocument();
            xml.Load(restoreInfoPath);
            var xnode = xml.SelectSingleNode("//RestoreInfo/BackupId");
            if (xnode == null)
                return Guid.Empty;
            Guid id;
            if (Guid.TryParse(xnode.InnerText, out id))
                return id;
            return Guid.Empty;
        }
        private static void SaveBackupIdToFile(Guid backupId, string restoreInfoPath)
        {
            var xml = new XmlDocument();
            xml.LoadXml(String.Format(RESTOREINFOCONTENT, backupId));
            xml.Save(restoreInfoPath);
        }
        private static void EnsureEmptyDirctory(string path)
        {
            if (!IO.Directory.Exists(path))
                IO.Directory.CreateDirectory(path);
            else
                DeleteDirectoryContent(path);
        }
        private static void OptimizeBeforeBackup(string indexDirectoryPath)
        {
            using (var op = SnTrace.Repository.StartOperation("Optimize index."))
            {
                Progress.StartOptimizeBeforeBackup();
                var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(indexDirectoryPath));
                var writer = new Lucene.Net.Index.IndexWriter(dir, Lucene29IndexManager.GetAnalyzer(), false, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED);
                writer.Optimize();
                writer.Close();
                if (!Lucene29IndexingEngine.WaitForWriterLockFileIsReleased(StorageContext.Search.IndexDirectoryBackupPath))
                    throw new ApplicationException("Writer lock releasing time out.");
                Progress.FinishOptimizeBeforeBackup();
                op.Successful = true;
            }
        }

        private static void DeleteDirectoryContent(string path)
        {
            foreach (var subPath in IO.Directory.GetDirectories(path))
                IO.Directory.Delete(subPath, true);
            foreach (var subPath in IO.Directory.GetFiles(path))
                IO.File.Delete(subPath);
        }
        private static void CopyDirectoryContent(string sourcePath, string targetPath, List<string> excludedFileNames)
        {
            foreach (var subPath in IO.Directory.GetDirectories(sourcePath))
            {
                var targetSubPath = IO.Path.Combine(targetPath, IO.Path.GetFileName(subPath));
                IO.Directory.CreateDirectory(targetSubPath);
                CopyDirectoryContent(subPath, targetSubPath, excludedFileNames);
            }
            foreach (var subPath in IO.Directory.GetFiles(sourcePath))
                if (!excludedFileNames.Contains(subPath))
                    IO.File.Copy(subPath, IO.Path.Combine(targetPath, IO.Path.GetFileName(subPath)));
        }
        private static void MoveDirectoryContent(string sourcePath, string targetPath)
        {
            foreach (var subPath in IO.Directory.GetDirectories(sourcePath))
                IO.Directory.Move(subPath, IO.Path.Combine(targetPath, IO.Path.GetFileName(subPath)));
            foreach (var subPath in IO.Directory.GetFiles(sourcePath))
                IO.File.Move(subPath, IO.Path.Combine(targetPath, IO.Path.GetFileName(subPath)));
        }
        private static void CompressTheIndex(string zipFilePath, string backupDirectoryPath)
        {
            using (ZipFile zip = new ZipFile())
            {
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
                Progress.StartCompressTheIndex();
                String[] filenames = System.IO.Directory.GetFiles(backupDirectoryPath);
                zip.AddFiles(filenames, COMPRESSIONROOT);
                zip.Save(zipFilePath);
                Progress.FinishCompressTheIndex();
            }
        }
        private static void DecompressTheIndex(string zipFilePath, string targetDirPath)
        {
            using (var zip = ZipFile.Read(zipFilePath))
            {
                foreach (var entry in zip)
                    entry.Extract(targetDirPath, ExtractExistingFileAction.OverwriteSilently);
            }
        }
        private static Guid StoreIndexBackupToDb(string backupFilePath)
        {
            var length = new System.IO.FileInfo(backupFilePath).Length;
            Progress.StartStoreIndexBackupToDb(length);
            var id = SenseNet.ContentRepository.Storage.DataBackingStore.StoreIndexBackupToDb(backupFilePath, Progress);
            Progress.FinishStoreIndexBackupToDb();
            return id;
        }
        private static void DeleteUnnecessaryBackups()
        {
            Progress.StartDeleteUnnecessaryBackups();
            SenseNet.ContentRepository.Storage.DataBackingStore.DeleteUnnecessaryBackups();
            Progress.FinishDeleteUnnecessaryBackups();
        }
        private static void RecoverIndexBackupFromDb(string recoveredFilePath)
        {
            SenseNet.ContentRepository.Storage.DataBackingStore.RecoverIndexBackupFromDb(recoveredFilePath);
        }
    }
}
