using Newtonsoft.Json;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Email
{
    [ContentHandler]
    public class EmailTemplate : GenericContent, IEmailTemplate
    {
        [RepositoryProperty(nameof(Subject), RepositoryDataType.String)]
        public string Subject
        {
            get => base.GetProperty<string>(nameof(Subject));
            set => this[nameof(Subject)] = value;
        }
        [RepositoryProperty(nameof(Body), RepositoryDataType.Text)]
        public string Body
        {
            get => base.GetProperty<string>(nameof(Body));
            set => this[nameof(Body)] = value;
        }

        string IEmailTemplate.Body => this.Body;

        public EmailTemplate(Node parent) : base(parent) { }
        public EmailTemplate(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected EmailTemplate(NodeToken nt) : base(nt) { }

        public override object GetProperty(string name)
        {
            return name switch
            {
                nameof(Subject) => Subject,
                nameof(Body) => Body,
                _ => base.GetProperty(name),
            };
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case nameof(Subject): Subject = (string)value; break;
                case nameof(Body): Body = (string)value; break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
