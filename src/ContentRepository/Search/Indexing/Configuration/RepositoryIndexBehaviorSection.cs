using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SenseNet.Search.Indexing.Configuration
{

    public class RepositoryIndexBehaviorSection : ConfigurationSection
    {
        private const string EnableWriteBehindName = "enableWriteBehind";
        private const string CommitIntervalName = "commitInterval";
        private const string MaxUncommitedChangesName = "maxUncommitedChanges";
        private const string NearRealtimeSearchName = "nearRealtimeSearch";


        [ConfigurationProperty(RepositoryIndexBehaviorSection.NearRealtimeSearchName, DefaultValue = "true")]
        public bool NearRealtimeSearch
        {
            get
            {
                return (Boolean)this[RepositoryIndexBehaviorSection.NearRealtimeSearchName];
            }
            set
            {
                this[RepositoryIndexBehaviorSection.NearRealtimeSearchName] = value;
            }
        }


        [ConfigurationProperty(RepositoryIndexBehaviorSection.EnableWriteBehindName, DefaultValue = "false")]
        public bool EnableWriteBehind
        {
            get
            {
                return (Boolean)this[RepositoryIndexBehaviorSection.EnableWriteBehindName];
            }
            set
            {
                this[RepositoryIndexBehaviorSection.EnableWriteBehindName] = value;
            }
        }

        [ConfigurationProperty(RepositoryIndexBehaviorSection.CommitIntervalName, DefaultValue = 10)]
        public int CommitInterval
        {
            get
            {
                return (int)this[RepositoryIndexBehaviorSection.CommitIntervalName];
            }
            set
            {
                this[RepositoryIndexBehaviorSection.CommitIntervalName] = value;
            }
        }

        [ConfigurationProperty(RepositoryIndexBehaviorSection.MaxUncommitedChangesName, DefaultValue = 100)]
        public int MaxUncommitedChanges
        {
            get
            {
                return (int)this[RepositoryIndexBehaviorSection.MaxUncommitedChangesName];
            }
            set
            {
                this[RepositoryIndexBehaviorSection.MaxUncommitedChangesName] = value;
            }
        }


        private const string RepositoryIndexBehaviorSectionName = "sensenet/repositoryIndexBehavior";

        public static RepositoryIndexBehaviorSection Current
        {
            get
            {
                var section = (RepositoryIndexBehaviorSection)ConfigurationManager.GetSection(RepositoryIndexBehaviorSectionName);
                if (section == null)
                    section = new RepositoryIndexBehaviorSection() { MaxUncommitedChanges = 100, EnableWriteBehind = false, NearRealtimeSearch = true, CommitInterval = 10 };
                return section;
            }
        }

    }
}
