using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Data
{
    public enum IndexBackupProgressType { CopyIndexToBackup, Optimizing, Compressing, Storing, DeletingUnnecessaryBackups, Error }
    public class IndexBackupProgress
    {
        public event EventHandler Changed;
        public IndexBackupProgressType Type { get; set; }
        public string Message { get; set; }
        public long Value { get; set; }
        public long MaxValue { get; set; }
        private StringBuilder _summary = new StringBuilder();
        public string Summary
        {
            get { return _summary.ToString(); }
        }

        public void Reset()
        {
            _summary.Clear();
        }
        public void SetAndNotify(IndexBackupProgressType type, string msg, long value, long maxValue)
        {
            Type = type;
            Message = msg;
            Value = value;
            MaxValue = maxValue;
            NotifyChanged();
        }

        private DateTime _lastTime;
        private long _lastPercent;
        public void NotifyChanged()
        {
            if (Changed == null)
                return;

            if (Type != IndexBackupProgressType.Storing)
            {
                Changed(this, EventArgs.Empty);
                return;
            }

            var percent = Value * 100 / MaxValue;
            var time = DateTime.UtcNow;
            if (time.AddSeconds(-2.0d) > _lastTime || percent > _lastPercent)
            {
                _lastPercent = percent;
                _lastTime = time;
                Changed(this, EventArgs.Empty);
            }
        }

        private Stopwatch _timer;
        public void StartCopyIndexToBackupDirectory()
        {
            Reset();
            Start(IndexBackupProgressType.CopyIndexToBackup, "Copying index to backup directory", 0, 1);
        }
        public void FinishCopyIndexToBackupDirectory()
        {
            Finish();
        }
        public void StartOptimizeBeforeBackup()
        {
            Start(IndexBackupProgressType.Optimizing, "Optimizing", 0, 1);
        }
        public void FinishOptimizeBeforeBackup()
        {
            Finish();
        }
        public void StartCompressTheIndex()
        {
            Start(IndexBackupProgressType.Compressing, "Compressing", 0, 1);
        }
        public void FinishCompressTheIndex()
        {
            Finish();
        }
        public void StartStoreIndexBackupToDb(long fileLength)
        {
            _summary.Append("Backup length: ").Append(fileLength).Append(Environment.NewLine);
            Start(IndexBackupProgressType.Storing, Message = "Storing", Value = 0, MaxValue = 1);
        }
        public void FinishStoreIndexBackupToDb()
        {
            Finish();
        }
        public void StartDeleteUnnecessaryBackups()
        {
            Start(IndexBackupProgressType.DeletingUnnecessaryBackups, "Deleting unnecessary backups", 0, 1);
        }
        public void FinishDeleteUnnecessaryBackups()
        {
            Finish();
        }

        private void Start(IndexBackupProgressType type, string message, long value, long maxValue)
        {
            Type = type;
            Message = message;
            Value = value;
            MaxValue = maxValue;
            _timer = Stopwatch.StartNew();
            NotifyChanged();
        }
        private void Finish()
        {
            _timer.Stop();
            _summary.Append(Message).Append(":\t").Append(_timer.Elapsed).Append(Environment.NewLine);
            Value = MaxValue;
            NotifyChanged();
        }
    }
}
