﻿using System;
using System.Diagnostics;

namespace SenseNet.Diagnostics
{
    public interface IStatisticalDataRecord
    {
        int Id { get; }                    // by provider
        string DataType { get; }           // General
        DateTime WrittenTime { get; }      // by provider
        DateTime? CreationTime { get; }    // from WebTransfer
        TimeSpan? Duration { get; }        // from WebTransfer
        long? RequestLength { get; }       // from WebTransfer
        long? ResponseLength { get; }      // from WebTransfer
        int? ResponseStatusCode { get; }   // from WebTransfer
        string Url { get; }                // from WebTransfer
        int? TargetId { get; }            // from WebHook
        int? ContentId { get; }            // from WebHook
        string EventName { get; }          // from WebHook
        string ErrorMessage { get; }       // from WebHook

        string GeneralData { get; }        // from General
    }
    [DebuggerDisplay("{DataType} {CreationTime} {Url}")]
    public class StatisticalDataRecord : IStatisticalDataRecord
    {
        public int Id { get; set; }
        public string DataType { get; set; }
        public DateTime WrittenTime { get; set; }
        public DateTime? CreationTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public long? RequestLength { get; set; }
        public long? ResponseLength { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string Url { get; set; }
        public int? TargetId { get; set; }
        public int? ContentId { get; set; }
        public string EventName { get; set; }
        public string ErrorMessage { get; set; }
        public string GeneralData { get; set; }
    }

}
