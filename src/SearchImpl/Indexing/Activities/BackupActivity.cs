using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using SenseNet.Diagnostics;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class BackupActivity : DistributedLuceneActivity
    {
        private string _machine;
        public string _appDomain;

        public BackupActivity(string machine, string appDomain)
        {
            _machine = machine;
            _appDomain = appDomain;
        }

        internal override void ExecuteIndexingActivity()
        {
            if (Environment.MachineName == _machine && AppDomain.CurrentDomain.FriendlyName == _appDomain)
            {
                try
                {
                    BackupTools.CopyIndexToBackupDirectory();
                    var backupFinisherThread = new Thread(new ThreadStart(BackupTools.OptimizeCompressAndStore));
                    backupFinisherThread.Start();
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
            }
            else
            {
                // Shedule new backup time?
            }
        }
    }
}
