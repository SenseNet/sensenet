using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Web;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using System.Net;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class SurveyItem : GenericContent
    {
        // ================================================================================= Constructors

        public SurveyItem(Node parent)
            : this(parent, null)
        {
        }
        public SurveyItem(Node parent, string nodeTypeName)
            : base(parent, nodeTypeName)
        {
        }
        protected SurveyItem(NodeToken nt)
            : base(nt)
        {    
        }

        // ================================================================================= Properties

        private const string EvaluatedByStr = "EvaluatedBy";
        private const string EvaluatedAtStr = "EvaluatedAt";
        private const string EvaluationStr = "Evaluation";

        [RepositoryProperty(EvaluatedByStr, RepositoryDataType.Reference)]
        public Node EvaluatedBy
        {
            get { return base.GetReference<Node>(EvaluatedByStr); }
            set { this.SetReference(EvaluatedByStr, value); }
        }

        [RepositoryProperty(EvaluatedAtStr, RepositoryDataType.DateTime)]
        public DateTime EvaluatedAt
        {
            get { return base.GetProperty<DateTime>(EvaluatedAtStr); }
            set { this[EvaluatedAtStr] = value; }
        }

        [RepositoryProperty(EvaluationStr, RepositoryDataType.Text)]
        public String Evaluation
        {
            get { return base.GetProperty<String>(EvaluationStr); }
            set { this[EvaluationStr] = value; }
        }

        // ================================================================================= Generic property handling

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case EvaluatedByStr:
                    this.EvaluatedBy = value as Node;
                    break;
                case EvaluatedAtStr:
                    this.EvaluatedAt = (DateTime)value;
                    break;
                case EvaluationStr:
                    this.Evaluation = (String)value;
                    break;
                default: base.SetProperty(name, value);
                    break;
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case EvaluatedByStr:
                    return this.EvaluatedBy;
                case EvaluatedAtStr:
                    return this.EvaluatedAt;
                case EvaluationStr:
                    return this.Evaluation;
                default: base.GetProperty(name);
                    break;
            }
            return base.GetProperty(name);
        }

        // ================================================================================= Overrides

        protected override void OnCreating(object sender, Storage.Events.CancellableNodeEventArgs e)
        {
            base.OnCreating(sender, e);

            var parent = e.SourceNode.Parent;
            var searchPath = parent is Survey ? parent.Path : parent.ParentPath;

            // Count Survey Items
            var surveyItemCount = ContentQuery.Query("+Type:surveyitem +InTree:@0 .AUTOFILTERS:OFF .COUNTONLY", null, searchPath).Count;

            // Get children (SurveyItems) count
            String tempName;
            if (surveyItemCount < 10 && surveyItemCount != 9)
                tempName = "SurveyItem_0" + (surveyItemCount + 1);
            else
                tempName = "SurveyItem_" + (surveyItemCount + 1);

            // If node already exits
            while (Node.Exists(RepositoryPath.Combine(parent.Path, tempName)))
            {
                surveyItemCount++;
                if (surveyItemCount < 10)
                    tempName = "SurveyItem_0" + (surveyItemCount + 1);
                else
                    tempName = "SurveyItem_" + (surveyItemCount + 1);
            }

            e.SourceNode["DisplayName"] = tempName;
            e.SourceNode["Name"] = tempName.ToLower();
        }

        protected override void OnCreated(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnCreated(sender, e);

            SendNotification();
        }

        protected override void OnModifying(object sender, Storage.Events.CancellableNodeEventArgs e)
        {
            base.OnModifying(sender, e);

            if (e.ChangedData.Any(x => x.Name == EvaluationStr))
            {
                this.EvaluatedBy = (User)User.LoggedInUser;
                this.EvaluatedAt = DateTime.UtcNow;
            }
        }

        // ================================================================================= Helper methods 

        private void SendNotification()
        {
            var parent = Content.Create(this.Parent);
            bool isNotificationEnabled;


            if (bool.TryParse(parent["EnableNotificationMail"].ToString(), out isNotificationEnabled) && isNotificationEnabled)
            {
                var mailTemplate = this.Parent.GetReference<Node>("MailTemplatePage");
                var senderAddress = parent.Fields["SenderAddress"].GetData().ToString();
                
                if (mailTemplate != null && !string.IsNullOrEmpty(senderAddress))
                {
                    var evaluators = parent["Evaluators"] as List<Node>;
                    var emailList = new Dictionary<string, string>();

                    if (evaluators != null)
                    {
                        foreach (var evaluator in evaluators)
                        {
                            var user = evaluator as IUser;

                            if (user != null && !string.IsNullOrEmpty(user.Email) && !emailList.ContainsKey(user.Email))
                            {
                                emailList.Add(user.Email, user.FullName);
                            }
                            else
                            {
                                var group = evaluator as Group;

                                if (group != null)
                                {
                                    foreach (var usr in group.GetAllMemberUsers())
                                    {
                                        if (!string.IsNullOrEmpty(usr.Email) && !emailList.ContainsKey(usr.Email))
                                        {
                                            emailList.Add(usr.Email, usr.FullName);
                                        }
                                    }
                                }
                            }
                        }

                        var mailTemplateCnt = Content.Create(mailTemplate);

                        var mailSubject = new StringBuilder(mailTemplateCnt.Fields["Subtitle"].GetData().ToString());

                        var mailBody = new StringBuilder(mailTemplateCnt.Fields["Body"].GetData().ToString());
                        var linkText = "<a href='{0}?action={1}'>{1}</a>";

                        var url = GetUrl();

                        mailBody = mailBody.Replace("{User}", (this.CreatedBy as IUser).FullName);
                        mailBody = mailBody.Replace("{SurveyName}", parent.DisplayName);
                        mailBody = mailBody.Replace("{Browse}", string.Format(linkText, url, "Browse"));
                        mailBody = mailBody.Replace("{Evaluate}", string.Format(linkText, url, "Evaluate"));
                        mailBody = mailBody.Replace("{Creator}", (this.Parent.CreatedBy as IUser).FullName);

                        var smtpClient = new SmtpClient();

                        foreach (var email in emailList)
                        {
                            mailBody = mailBody.Replace("{Addressee}", email.Value);
                            var mailMessage = new MailMessage(senderAddress, email.Key)
                                                  {
                                                      Subject = mailSubject.ToString(),
                                                      IsBodyHtml = true,
                                                      Body = mailBody.ToString()
                                                  };

                            try
                            {
                                smtpClient.Send(mailMessage);
                            }
                            catch (Exception ex) // logged
                            {
                                SnLog.WriteException(ex);
                            }
                        }
                    }
                }
                else
                {
                    SnLog.WriteError("Notification e-mail cannot be sent because the template content or the sender address is missing.");
                }
            }
        }

        private string GetUrl()
        {
            var ctx = HttpContext.Current;
            var req = ctx == null ? null : ctx.Request;
            if (req != null)
            {
                var url = req.UrlReferrer.AbsoluteUri;
                return url.Substring(0, url.IndexOf("?")) + "/" + this.Name;
            }

            var site = Node.GetAncestorOfNodeType(this, "Site");
            if (site == null)
                return null;

            var relPath = this.Path.Substring(site.Path.Length);

            var urls = (IDictionary<string, string>)((GenericContent)site).GetProperty("UrlList");
            var host = urls.First().Key;

            var uri = new UriBuilder();
            uri.Host = host;
            uri.Path = relPath;

            return uri.ToString();
        }
    }
}
