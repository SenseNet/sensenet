using System;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Fields;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class CalendarEvent : GenericContent
    {
        private bool _successfulFormCreation = false;

        private const string REGISTRATIONFORM = "RegistrationForm";
        [RepositoryProperty(REGISTRATIONFORM, RepositoryDataType.Reference)]
        public virtual Node RegistrationForm
        {
            get { return GetReference<Node>(REGISTRATIONFORM); }
            set { SetReference(REGISTRATIONFORM, value); }
        }

        private const string NUMPARTICIPANTS = "NumParticipants";
        public int NumParticipants
        {
            get { return GetNumberOfParticipants(this); }
        }


        public override object GetProperty(string name)
        {
            switch (name)
            {
                case REGISTRATIONFORM:
                    return RegistrationForm;
                case NUMPARTICIPANTS:
                    return NumParticipants;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case REGISTRATIONFORM:
                    RegistrationForm = (Node)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }


        // ================================================================================= Construction

        public CalendarEvent(Node parent) : this(parent, null) { }
        public CalendarEvent(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected CalendarEvent(NodeToken nt) : base(nt) { }


        public override void Save(SavingMode mode)
        {
            _successfulFormCreation = false;
            // Creating registration form if necessary.
            if (GetReferenceCount(REGISTRATIONFORM) == 0 && Convert.ToBoolean(this["RequiresRegistration"]))
            {
                var regFormFolder = Parent.GetPropertySafely("RegistrationFolder") as NodeList<Node>;
                var formFolder = regFormFolder != null ? regFormFolder.FirstOrDefault() : null;

                if (formFolder != null)
                {
                    var formName = String.Format("{0}_{1}", ParentName, this["Name"]);

                    if (Content.Load(formFolder.Path + "/" + formName) == null)
                    {
                        var regForm = Content.CreateNew("EventRegistrationForm", formFolder, formName);

                        regForm["Name"] = formName;

                        regForm["AllowedChildTypes"] = ContentType.GetByName("EventRegistrationFormItem");

                        regForm["EmailList"] = !String.IsNullOrEmpty(this["OwnerEmail"].ToString()) ? this["OwnerEmail"].ToString() : String.Empty;

                        regForm["EmailTemplate"] = !String.IsNullOrEmpty(this["EmailTemplate"].ToString()) ? this["EmailTemplate"] : "{0}";

                        regForm["EmailTemplateSubmitter"] = !String.IsNullOrEmpty(this["EmailTemplateSubmitter"].ToString()) ? this["EmailTemplateSubmitter"] : "{0}";

                        regForm["EmailFrom"] = !String.IsNullOrEmpty(this["EmailFrom"].ToString()) ? this["EmailFrom"] : "mailerservice@example.com";

                        regForm["EmailFromSubmitter"] = !String.IsNullOrEmpty(this["EmailFromSubmitter"].ToString()) ? this["EmailFromSubmitter"] : "mailerservice@example.com";

                        regForm["EmailField"] = !String.IsNullOrEmpty(this["EmailField"].ToString()) ? this["EmailField"] : "mailerservice@example.com";

                        regForm.Save();

                        AddReference(REGISTRATIONFORM, LoadNode(regForm.Id));

                        _successfulFormCreation = true;
                    }
                }
            }

            // validation
            if (this["StartDate"] != null && this["EndDate"] != null)
            {
                var startDate = (DateTime)this["StartDate"];
                var endtDate = (DateTime)this["EndDate"];

                // check only real values
                if (startDate > DateTime.MinValue && endtDate > DateTime.MinValue)
                {
                    if (startDate > endtDate)
                        throw new InvalidContentException(SR.GetString(SR.Exceptions.CalendarEvent.Error_InvalidStartEndDate));
                }
            }

            base.Save(mode);
        }

        protected override void OnCreated(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnCreated(sender, e);
            if (_successfulFormCreation)
            {
                string page = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                var backUrl = HttpContext.Current.Request.Params["back"];
                var link = string.Concat(page, RegistrationForm.Path, "?action=ManageFields", "&back=", backUrl);
                HttpContext.Current.Response.Redirect(link);
            }
        }

        private static int GetNumberOfParticipants(Node item)
        {
            var regForm = item.GetReference<Node>(REGISTRATIONFORM);
            if (regForm != null)
            {
                var qResult = ContentQuery.Query("+Type:eventregistrationformitem +ParentId:@0",
                    new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled },
                    regForm.Id);

                var i = 0;

                foreach (var node in qResult.Nodes)
                {
                    var subs = Content.Create(node);
                    var guests = 0;
                    int.TryParse(subs["GuestNumber"].ToString(), out guests);
                    i += (guests + 1);
                }

                return i;
            }
            return 0;
        }

    }
}
