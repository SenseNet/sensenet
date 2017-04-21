namespace SenseNet.Packaging.Steps
{
    public class IfDatabaseExists : IfDatabaseValue
    {
        public string Name { get; set; }
        public new string UserName { get; set; }
        public new string Password { get; set; }

        public new string DataSource { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var dbName = (string)context.ResolveVariable(Name);
            if (string.IsNullOrEmpty(dbName))
                throw new InvalidParameterException("Database name cannot be empty.");

            Query = $"Select [database_id] From [sys].[databases] Where [name] ='{dbName}'";
            this.InitialCatalogName = "master";

            base.Execute(context);
        }
    }
}
