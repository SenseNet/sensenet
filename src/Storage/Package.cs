using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    [Obsolete("####", true)]
    public enum PackageType { Product, Application }

    public enum PackageLevel { Tool, Patch, ServicePack, Upgrade, Install }

    public enum ExecutionResult { Successful, Faulty, Unfinished }

    [Serializable]
    [DebuggerDisplay("{Id}, {AppId}: {PackageLevel} {ExecutionResult}, {ApplicationVersion}")]
    public class Package
    {
        //UNDONE: Not used field: [Edition] [nvarchar](450) NULL,
        //UNDONE: Not used field: [PackageType] [varchar](50) NOT NULL,

        public int Id { get; internal set; }                  // [Id] [int] IDENTITY(1,1) NOT NULL,
        public string Name { get; set; }                      // [Name] [nvarchar](450) NOT NULL,
        public string Description { get; set; }               // [Description] [nvarchar](1000) NULL,
        public string AppId { get; set; }                     // [AppId] [varchar](50) NULL,
        public PackageLevel PackageLevel { get; set; }        // [PackageLevel] [varchar](50) NOT NULL,
        public DateTime ReleaseDate { get; set; }             // [ReleaseDate] [datetime] NOT NULL,
        public DateTime ExecutionDate { get; set; }           // [ExecutionDate] [datetime] NOT NULL,
        public ExecutionResult ExecutionResult { get; set; }  // [ExecutionResult] [varchar](50) NOT NULL,
        public Version ApplicationVersion { get; set; }       // [AppVersion] [varchar](50) NULL,
        [Obsolete("####")]
        public Version SenseNetVersion { get; set; }          // [SenseNetVersion] [varchar](50) NOT NULL,
        public Exception ExecutionError { get; set; }         // [ExecutionError] [nvarchar](max) NULL
    }
}