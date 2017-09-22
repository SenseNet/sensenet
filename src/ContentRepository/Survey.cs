using System;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Survey : ContentList
    {
        // ================================================================================= Properties

        [RepositoryProperty("LandingPage", RepositoryDataType.Reference)]
        public Node LandingPage
        {
            get
            {
                return base.GetReference<Node>("LandingPage");
            }
            set
            {
                this.SetReference("LandingPage", value);
            }
        }

        [RepositoryProperty("PageContentView", RepositoryDataType.Reference)]
        public Node PageContentView
        {
            get
            {
                return base.GetReference<Node>("PageContentView");
            }
            set
            {
                this.SetReference("PageContentView", value);
            }
        }

        [RepositoryProperty("InvalidSurveyPage", RepositoryDataType.Reference)]
        public Node InvalidSurveyPage
        {
            get
            {
                return base.GetReference<Node>("InvalidSurveyPage");
            }
            set
            {
                this.SetReference("InvalidSurveyPage", value);
            }
        }

        [RepositoryProperty("MailTemplatePage", RepositoryDataType.Reference)]
        public Node MailTemplatePage
        {
            get
            {
                return base.GetReference<Node>("MailTemplatePage");
            }
            set
            {
                this.SetReference("MailTemplatePage", value);
            }
        }


        [RepositoryProperty("EnableMoreFilling", RepositoryDataType.Int)]
        public bool EnableMoreFilling
        {
            get
            {
                return this.GetProperty<int>("EnableMoreFilling") != 0;
            }
            set { this["EnableMoreFilling"] = value ? 1 : 0; }
        }

        // ================================================================================= Constructors

        public Survey(Node parent) : this(parent, null) {}
        public Survey(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        protected Survey(NodeToken nt) : base(nt) { }

        // ================================================================================= Helper methods 

        private string _originalContentListDefinition;

        protected void StoreContentListDefinition()
        {
            _originalContentListDefinition = this.ContentListDefinition;
        }

        // ================================================================================= Generic property handling

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "LandingPage":
                    this.LandingPage = value as Node;
                    break;
                case "PageContentView":
                    this.PageContentView = value as Node;
                    break;
                case "InvalidSurveyPage":
                    this.InvalidSurveyPage = value as Node;
                    break;
                case "MailTemplatePage":
                    this.MailTemplatePage = value as Node;
                    break;
                case "EnableMoreFilling":
                    this.EnableMoreFilling = Convert.ToBoolean(value);
                    break;
                default: base.SetProperty(name, value);
                    break;
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "LandingPage":
                    return this.LandingPage;
                case "PageContentView":
                    return this.PageContentView;
                case "InvalidSurveyPage":
                    return this.InvalidSurveyPage;
                case "MailTemplatePage":
                    return this.MailTemplatePage;
                case "EnableMoreFilling":
                    return this.EnableMoreFilling;
            }
            return base.GetProperty(name);
        }

        // ================================================================================= Overrides

        public override void Save(SavingMode mode)
        {
            if (this.Id > 0 && StorageContext.Search.ContentQueryIsAllowed)
            {
                if (ContentQuery.Query(SafeQueries.SurveyItemsInFolderCount, null, this.Id)
                    .Count > 0 && _originalContentListDefinition != this.ContentListDefinition)
                {
                    throw new InvalidOperationException("Cannot modify questions due to existing filled survey(s).");
                }
            }
            base.Save(mode);
        }

        protected override void OnLoaded(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            StoreContentListDefinition();
        }

        protected class SafeQueries : ISafeQueryHolder
        {
            public static string SurveyItemsInFolderCount { get { return "+TypeIs:surveyitem +ParentId:@0 .COUNTONLY .LIFESPAN:OFF .AUTOFILTERS:OFF"; } }
        }
    }
}
