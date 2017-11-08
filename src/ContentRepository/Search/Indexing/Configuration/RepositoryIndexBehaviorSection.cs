using System.Configuration;

namespace SenseNet.Search.Indexing.Configuration
{
    //UNDONE: remove RepositoryIndexBehaviorSection class
    public class RepositoryIndexBehaviorSection : ConfigurationSection
    {
        private const string EnableWriteBehindName = "enableWriteBehind";
        private const string CommitIntervalName = "commitInterval";
        private const string MaxUncommitedChangesName = "maxUncommitedChanges";
        private const string NearRealtimeSearchName = "nearRealtimeSearch";


        [ConfigurationProperty(NearRealtimeSearchName, DefaultValue = "true")]
        public bool NearRealtimeSearch
        {
            get => (bool)this[NearRealtimeSearchName];
            set => this[NearRealtimeSearchName] = value;
        }


        [ConfigurationProperty(EnableWriteBehindName, DefaultValue = "false")]
        public bool EnableWriteBehind
        {
            get => (bool)this[EnableWriteBehindName];
            set => this[EnableWriteBehindName] = value;
        }

        [ConfigurationProperty(CommitIntervalName, DefaultValue = 10)]
        public int CommitInterval
        {
            get => (int)this[CommitIntervalName];
            set => this[CommitIntervalName] = value;
        }

        [ConfigurationProperty(MaxUncommitedChangesName, DefaultValue = 100)]
        public int MaxUncommitedChanges
        {
            get => (int)this[MaxUncommitedChangesName];
            set => this[MaxUncommitedChangesName] = value;
        }
        
        private const string RepositoryIndexBehaviorSectionName = "sensenet/repositoryIndexBehavior";

        public static RepositoryIndexBehaviorSection Current
        {
            get
            {
                var section =
                    (RepositoryIndexBehaviorSection) ConfigurationManager.GetSection(
                        RepositoryIndexBehaviorSectionName) ??
                    new RepositoryIndexBehaviorSection
                    {
                        MaxUncommitedChanges = 100,
                        EnableWriteBehind = false,
                        NearRealtimeSearch = true,
                        CommitInterval = 10
                    };
                return section;
            }
        }
    }
}
