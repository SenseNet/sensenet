using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Packaging.Steps
{
    public class IfEquals : ConditionalStep
    {
        public string LeftOperand { get; set; }
        public string RightOperand { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(LeftOperand) && string.IsNullOrEmpty(RightOperand))
                return true;

            var left = context.ResolveVariable(LeftOperand) ?? string.Empty;
            var right = context.ResolveVariable(RightOperand) ?? string.Empty;

            // check if one of them was a real boolean variable
            if (left is bool)
                return Compare((bool) left, right);
            if (right is bool)
                return Compare((bool) right, left);

            // check if they can be parsed as boolean values
            bool tempBool;
            if (bool.TryParse(left.ToString(), out tempBool))
                return Compare(tempBool, right);

            // compare regular strings
            return string.Compare(left.ToString(), right.ToString(), StringComparison.InvariantCulture) == 0;
        }

        private static bool Compare(bool first, object second)
        {
            // compare a bool or a string representation of a bool to a bool variable

            if (second is bool)
                return first == (bool)second;

            bool rightBool;
            if (bool.TryParse(second.ToString(), out rightBool))
                return first == rightBool;

            throw new InvalidParameterException($"Cannot parse {second} to boolean.");
        }
    }
}
