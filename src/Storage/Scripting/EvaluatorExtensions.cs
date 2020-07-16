using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Scripting;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class EvaluatorExtensions
    {
        public static IRepositoryBuilder UseScriptEvaluator(this IRepositoryBuilder repositoryBuilder, IEvaluator evaluator)
        {
            if (evaluator == null)
                return repositoryBuilder;

            var fullTagName = Evaluator.GetFullTagName(evaluator.GetType());
            if (!string.IsNullOrEmpty(fullTagName))
                Providers.Instance.SetProvider(fullTagName, evaluator);

            return repositoryBuilder;
        }
    }
}
