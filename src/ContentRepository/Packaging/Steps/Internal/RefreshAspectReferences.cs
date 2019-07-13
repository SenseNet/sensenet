using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;
// ReSharper disable once ArrangeThisQualifier

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public class RefreshAspectReferences : Step
    {
        public override string ElementName => "Internal." + this.GetType().Name;

        private const string Script = @"
SELECT DISTINCT SrcId
FROM ReferencesInfoView
WHERE RelType = 'Aspects' and TargetId in
    (SELECT NodeId FROM NodeInfoView WHERE Type = 'Aspect')
";

        public override void Execute(ExecutionContext context)
        {
            var count = 0;

            using (var ctx = new MsSqlDataContext())
            {
                ctx.ExecuteReaderAsync/*UNDONE*/(Script, async (reader, cancel) =>
                {
                    do
                    {
                        if (!reader.HasRows)
                            continue;

                        using (new SystemAccount())
                        {
                            while (await reader.ReadAsync(cancel))
                            {
                                Operate(reader.GetInt32(0));
                                count++;
                            }
                        }

                    } while (reader.NextResult());

                    return Task.FromResult(0);
                }).Wait();
            }

            Logger.LogMessage(count < 1
                ? "No content was found with aspect reference field."
                : $"Aspect references were updated on {count} content.");
        }
        private void Operate(int id)
        {
            var content = Content.Load(id);

            if (!(content.ContentHandler is GenericContent))
                return;

            // iterate through reference aspect fields and re-set them
            foreach (var field in content.AspectFields.Values.Where(f => f is ReferenceField))
                content[field.Name] = content[field.Name];

            content.SaveSameVersion();
        }
    }
}
