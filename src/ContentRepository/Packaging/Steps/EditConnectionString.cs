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
        public string DbUserName { get; set; }
        public string DbPassword { get; set; }
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
            DbUserName = (string) context.ResolveVariable(DbUserName);
            DbPassword = (string) context.ResolveVariable(DbPassword);

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

            if (string.Compare(conn.UserID, DbUserName, StringComparison.InvariantCulture) != 0)
            {
                if (!string.IsNullOrWhiteSpace(DbUserName) && !string.IsNullOrWhiteSpace(DbPassword))
                {
                    conn.UserID = DbUserName;
                    conn.Password = DbPassword;
                    conn.IntegratedSecurity = false;
                    changed = true;
                }
                else
                {
                    conn.Remove("User ID");
                    conn.Remove("Password");
                    conn.IntegratedSecurity = true;
                    changed = true;
                }
            }
            if (!changed)
                return false;

            // set the modified connection string as the "source" value that the base method will use
            Source = conn.ConnectionString;

            return base.EditXml(doc, path);
        }
    }
}
