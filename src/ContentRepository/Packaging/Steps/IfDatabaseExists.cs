namespace SenseNet.Packaging.Steps
{
    public class IfDatabaseExists : IfDatabaseValue
    {
        public string Name { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var dbName = (string)context.ResolveVariable(Name);
            if (string.IsNullOrEmpty(dbName))
                throw new InvalidParameterException("Database name cannot be empty.");

            this.Query = $"SELECT db_id('{dbName}')";
            this.InitialCatalogName = "master";

            base.Execute(context);
        }
    }
}
