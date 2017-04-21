using System;
using System.Data.SqlClient;
using System.Linq;
using System.Xml;

namespace SenseNet.Packaging.Steps
{
    public class EditConnectionString : EditXmlNodes
    {
        public string ConnectionName { get; set; } = "SnCrMsSql";
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IntegratedSecurity { get; set; } = false;
        public override string Xpath
        {
            get { return $"/configuration/connectionStrings/add[@name='{ConnectionName}']/@connectionString"; }
            set { base.Xpath = value; }
        }

        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(ConnectionName))
                throw new PackagingException("ConnectionName cannot be empty.");

            if (string.IsNullOrEmpty(DataSource) && string.IsNullOrEmpty(InitialCatalogName))
                throw new PackagingException("Please provide a datasource or database name to insert.");

            ConnectionName = (string)context.ResolveVariable(ConnectionName);
            DataSource = (string)context.ResolveVariable(DataSource);
            InitialCatalogName = (string)context.ResolveVariable(InitialCatalogName);
            UserName = (string) context.ResolveVariable(UserName);
            Password = (string) context.ResolveVariable(Password);

            base.Execute(context);
        }

        protected override bool EditXml(XmlDocument doc, string path)
        {
            // load the original value from the xml if exists
            var connStrNode = SelectXmlNodes(doc, this.Xpath).Cast<XmlNode>().FirstOrDefault();
            var connectionString = connStrNode?.InnerText;
            if (string.IsNullOrEmpty(connectionString))
                return false;

            var conn = new SqlConnectionStringBuilder(connectionString);
            var changed = false;

            // modify parts of the connection string, if they are different
            if (!string.IsNullOrEmpty(DataSource) && string.Compare(conn.DataSource, DataSource, StringComparison.InvariantCulture) != 0)
            {
                conn.DataSource = DataSource;
                changed = true;
            }
            if (!string.IsNullOrEmpty(InitialCatalogName) && string.Compare(conn.InitialCatalog, InitialCatalogName, StringComparison.InvariantCulture) != 0)
            {
                conn.InitialCatalog = InitialCatalogName;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password) && string.Compare(conn.UserID, UserName, StringComparison.InvariantCulture) != 0)
            {
                conn.UserID = UserName;
                conn.Password = Password;
                conn.IntegratedSecurity = false;
                changed = true;
            }
            else if(IntegratedSecurity)
            {
                conn.UserID = null;
                conn.Password = null;
                conn.IntegratedSecurity = true;
                changed = true;
            }

            if (!changed)
                return false;

            // set the modified connection string as the "source" value that the base method will use
            Source = conn.ToString();

            return base.EditXml(doc, path);
        }
    }
}
