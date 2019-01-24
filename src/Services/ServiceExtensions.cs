using System;
using System.Web;
using System.Web.Compilation;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Services
{
    public static class ServiceExtensions
    {
        public static string GetStringByExpression(this SenseNetResourceManager resourceManager, string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return expression;

            if (!expression.StartsWith(SenseNetResourceManager.ResourceStartKey) ||
                !expression.EndsWith(SenseNetResourceManager.ResourceEndKey))
                return null;
            
            expression = expression.Replace(" ", "");
            expression = expression.Replace(SenseNetResourceManager.ResourceStartKey, "");
            expression = expression.Replace(SenseNetResourceManager.ResourceEndKey, "");

            if (expression.Contains("Resources:"))
                expression = expression.Remove(expression.IndexOf("Resources:", StringComparison.Ordinal), 10);

            var expressionFields = ResourceExpressionBuilder.ParseExpression(expression);
            if (expressionFields == null)
            {
                var context = HttpContext.Current;
                var msg = $"{expression} is not a valid string resource format.";
                if (context == null)
                    throw new ApplicationException(msg);
                return string.Format(msg);
            }

            return resourceManager.GetString(expressionFields.ClassKey, expressionFields.ResourceKey);
        }
    }
}
