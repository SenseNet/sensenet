using System.Text.RegularExpressions;

namespace SenseNet.Packaging.Steps
{
    public class IfMatch : ConditionalStep
    {
        public string Value { get; set; }
        public string Pattern { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Value))
                throw new InvalidParameterException("Value cannot be empty.");

            var conditionVariable = context.ResolveVariable(Value);
            var conditionText = conditionVariable as string;
            if (string.IsNullOrEmpty(conditionText))
                throw new InvalidParameterException("Invalid value: " + conditionVariable);

            return Regex.IsMatch(Value, Pattern);
        }
    }
}