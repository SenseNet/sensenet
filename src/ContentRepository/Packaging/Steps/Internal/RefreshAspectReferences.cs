using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Data;
using System.Data;
using System.Linq;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Packaging.Steps.Internal
{
    public class RefreshAspectReferences : Step
    {
        public override string ElementName
        {
            get { return "Internal." + this.GetType().Name; }
        }

        private const string SCRIPT = @"
SELECT DISTINCT SrcId
FROM ReferencesInfoView
WHERE RelType = 'Aspects' and TargetId in
    (SELECT NodeId FROM NodeInfoView WHERE Type = 'Aspect')
";

        public override void Execute(ExecutionContext context)
        {
            var count = 0;

            using (var proc = DataProvider.Instance.CreateDataProcedure(SCRIPT)) //DB:??
            {
                proc.CommandType = CommandType.Text;
                using (var reader = proc.ExecuteReader())
                {
                    do
                    {
                        if (!reader.HasRows)
                            continue;

                        using (new SystemAccount())
                        {
                            while (reader.Read())
                            {
                                Operate(reader.GetInt32(0));
                                count++;
                            } 
                        }

                    } while (reader.NextResult());
                }
            }

            if (count < 1)
                Logger.LogMessage("No content was found with aspect reference field.");
            else
                Logger.LogMessage(string.Format("Aspect references were updated on {0} content.", count));
        }
        private void Operate(int id)
        {
            var content = Content.Load(id);

            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                return;

            // iterate through reference aspect fields and re-set them
            foreach (var field in content.AspectFields.Values.Where(f => f is ReferenceField))
            {
                content[field.Name] = content[field.Name];
            }

            content.SaveSameVersion();
        }
    }
}
