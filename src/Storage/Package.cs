using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public enum PackageType { Tool, Patch, Install }

    public enum ExecutionResult { Successful, Faulty, Unfinished, FaultyBefore, SuccessfulBefore }

    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public class Package
    {
        public int Id { get; set; }                           // [Id] [int] IDENTITY(1,1) NOT NULL,
        public string Description { get; set; }               // [Description] [nvarchar](1000) NULL,
        public string ComponentId { get; set; }               // [ComponentId] [nvarchar](450) NULL,
        [JsonConverter(typeof(StringEnumConverter))]
        public PackageType PackageType { get; set; }          // [PackageType] [varchar](50) NOT NULL,
        public DateTime ReleaseDate { get; set; }             // [ReleaseDate] [datetime] NOT NULL,
        public DateTime ExecutionDate { get; set; }           // [ExecutionDate] [datetime] NOT NULL,
        [JsonConverter(typeof(StringEnumConverter))]
        public ExecutionResult ExecutionResult { get; set; }  // [ExecutionResult] [varchar](50) NOT NULL,
        public Version ComponentVersion { get; set; }         // [ComponentVersion] [varchar](50) NULL,
        public Exception ExecutionError { get; set; }         // [ExecutionError] [nvarchar](max) NULL,
        [JsonIgnore]
        public string Manifest { get; set; }                  // [Manifest] [nvarchar](max) NULL

        public override string ToString()
        {
            return $"{Id}, {ComponentId}: {PackageType} {ExecutionResult}, {ComponentVersion}";
        }
    }
}