using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ContentRepository.Fields;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Voting : Survey
    {
        /// <summary>
        /// Gets or sets a value indicating whether this result is visible before filling the voting.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if result is visible; otherwise, <c>false</c>.
        /// </value>
        [RepositoryProperty("IsResultVisibleBefore", RepositoryDataType.Int)]
        public bool IsResultVisibleBefore
        {
            get
            {
                return this.GetProperty<int>("IsResultVisibleBefore") != 0;
            }
            set { this["IsResultVisibleBefore"] = value ? 1 : 0; }
        }

        /// <summary>
        /// Gets or sets the result page content view.
        /// </summary>
        /// <value>The result page content view.</value>
        [RepositoryProperty("ResultPageContentView", RepositoryDataType.Reference)]
        public Node ResultPageContentView
        {
            get
            {
                return base.GetReference<Node>("ResultPageContentView");
            }
            set
            {
                this.SetReference("ResultPageContentView", value);
            }
        }

        /// <summary>
        /// Gets or sets the voting page content view.
        /// </summary>
        /// <value>The voting page content view.</value>
        [RepositoryProperty("VotingPageContentView", RepositoryDataType.Reference)]
        public Node VotingPageContentView
        {
            get
            {
                return base.GetReference<Node>("VotingPageContentView");
            }
            set
            {
                this.SetReference("VotingPageContentView", value);
            }
        }

        /// <summary>
        /// Gets or sets the cannot see result content view content view.
        /// </summary>
        /// <value>The cannot see result page.</value>
        [RepositoryProperty("CannotSeeResultContentView", RepositoryDataType.Reference)]
        public Node CannotSeeResultContentView
        {
            get
            {
                return base.GetReference<Node>("CannotSeeResultContentView");
            }
            set
            {
                this.SetReference("CannotSeeResultContentView", value);
            }
        }
        
        /// <summary>
        /// Determines whether voting is available for user depending on if it is allowed to be filled multiple times and if user filled it already.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if voting of this instance is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsVotingAvailable
        {
            get
            {
                return Convert.ToBoolean(this["EnableMoreFilling"]) ||
                       ContentQuery.Query("+Type:votingitem +CreatedById:@0 +InTree:@1 .AUTOFILTERS:OFF .COUNTONLY", null, User.Current.Id, this.Path).Count <= 0;
            }
        }

        /// <summary>
        /// Sets the given property's value by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "IsResultVisibleBefore":
                    this.IsResultVisibleBefore = Convert.ToBoolean(value);
                    break;
                case "ResultPageContentView":
                    this.ResultPageContentView = value as Node;
                    break;
                case "VotingPageContentView":
                    this.VotingPageContentView = value as Node;
                    break;
                case "CannotSeeResultContentView":
                    this.CannotSeeResultContentView = value as Node;
                    break;
                default: base.SetProperty(name, value);
                    break;
            }
        }

        /// <summary>
        /// Gets the given property by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "IsResultVisibleBefore":
                    return this.IsResultVisibleBefore;
                case "ResultPageContentView":
                    return this.ResultPageContentView;
                case "VotingPageContentView":
                    return this.VotingPageContentView;
                case "CannotSeeResultContentView":
                    return this.CannotSeeResultContentView;
            }
            return base.GetProperty(name);
        }

        public Dictionary<string,string> Result
        {
            get
            {
                var questionName = (from fs in this.FieldSettings
                                    where fs.Name.StartsWith("#")
                                    select fs.Name).FirstOrDefault();

                if (questionName == null)
                    return new Dictionary<string, string>();      

                var questionXml = (from fs in this.FieldSettings
                                   where fs.Name.StartsWith("#")
                                   select fs.ToXml()).FirstOrDefault();

                var doc = new XmlDocument();
                doc.LoadXml(questionXml);

                doc.SelectSingleNode("Configuration");
                doc.SelectSingleNode("Options");

                var questions = new Dictionary<string, string>();

                if (doc.DocumentElement != null)
                {
                    foreach (XPathNavigator node in doc.GetElementsByTagName("Options")[0].CreateNavigator().SelectChildren(XPathNodeType.Element))
                    {
                        questions.Add(node.GetAttribute("value", ""), node.Value);
                    }
                }

                var result = new Dictionary<string, string>();
                var excludedQuestionsExp = new StringBuilder();

                foreach (var question in questions)
                {
                    var count = ContentQuery.Query("+Type:votingitem +InTree:@0 +@1:@2 .AUTOFILTERS:OFF .COUNTONLY", null,
                        this.Path, questionName, question.Key).Count;

                    result.Add(question.Value, count.ToString());
                    excludedQuestionsExp.Append(" -").Append(questionName).Append(":").Append(question.Key);
                }

                var otherAnswersCount = ContentQuery.Query(string.Format("+Type:votingitem +InTree:\"{0}\" {1} .AUTOFILTERS:OFF .COUNTONLY", this.Path, excludedQuestionsExp)).Count;

                if (otherAnswersCount > 0)
                    result.Add("Other", otherAnswersCount.ToString());

                return result;
            }
        }

        #region C'tors
        public Voting(Node parent)
            : this(parent, null)
        {
            StoreContentListDefinition();
        }
        public Voting(Node parent, string nodeTypeName)
            : base(parent, nodeTypeName)
        {
            StoreContentListDefinition();
        }
        protected Voting(NodeToken nt)
            : base(nt)
        {
            StoreContentListDefinition();
        }
        #endregion

        public override void Save(SavingMode mode)
        {
            // Checking for duplicated options
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(this.ContentListDefinition);
            }
            catch
            {
                return;
            }


            var data = XDocument.Parse(ContentListDefinition);
            var values = new List<string>();

            var nodes = data.Nodes();

            var descandants = from descendant in data.Descendants() where descendant.Name.LocalName == "Option" select descendant;

            foreach (var descandant in descandants)
            {
                if (values.Contains(descandant.Attribute("value").Value))
                {
                    throw new InvalidOperationException("There are multiple values for answers.");
                }
                values.Add(descandant.Attribute("value").Value);
            }

            base.Save(mode);
        }
    }
}
