using System;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    public enum PackageType { Tool, Patch, ServicePack, Upgrade, Install }

    public enum ExecutionResult { Successful, Faulty, Unfinished }

    [Serializable]
    [DebuggerDisplay("{Id}, {ComponentId}: {PackageType} {ExecutionResult}, {ApplicationVersion}")]
    public class Package
    {
        public int Id { get; internal set; }                  // [Id] [int] IDENTITY(1,1) NOT NULL,
        public string Description { get; set; }               // [Description] [nvarchar](1000) NULL,
        public string ComponentId { get; set; }               // [ComponentId] [nvarchar](450) NULL,
        public PackageType PackageType { get; set; }          // [PackageType] [varchar](50) NOT NULL,
        public DateTime ReleaseDate { get; set; }             // [ReleaseDate] [datetime] NOT NULL,
        public DateTime ExecutionDate { get; set; }           // [ExecutionDate] [datetime] NOT NULL,
        public ExecutionResult ExecutionResult { get; set; }  // [ExecutionResult] [varchar](50) NOT NULL,
        public Version ComponentVersion { get; set; }         // [ComponentVersion] [varchar](50) NULL,
        public Exception ExecutionError { get; set; }         // [ExecutionError] [nvarchar](max) NULL
    }
}