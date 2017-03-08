namespace SenseNet.Packaging.Steps
{
    public class IfCondition : ConditionalStep
    {
        public override string ElementName => "If";

        public string Condition { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Condition))
                throw new InvalidParameterException("Condition cannot be empty.");

            var conditionVariable = context.ResolveVariable(Condition);
            if (conditionVariable is bool)
                return (bool) conditionVariable;

            var conditionText = conditionVariable as string;
            if (string.IsNullOrEmpty(conditionText))
                throw new InvalidParameterException("Unknown condition: " + conditionVariable);

            bool result;
            if (bool.TryParse(conditionText, out result))
                return result;

            throw new InvalidParameterException("Condition value could not be converted to bool: " + conditionVariable);
        }
    }
}
