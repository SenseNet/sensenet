using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    public abstract class TemplateReplacerBase
    {
        private static class RegexGroupNames
        {
            public static string Property = "Property";
            public static string Modifier = "Modifier";
            public static string Operator = "Operator";
            public static string MethodOperator = "MethodOperator";
            public static string Number = "Number";
            public static string Parameter = "Parameter";
            public static string MethodUnit = "MethodUnit";
            public static string Unit = "Unit";
        }

        public virtual string TemplatePatternFormat
        {
            get { return TemplateManager.TEMPLATE_PATTERN_FORMAT; }
        }

        public abstract IEnumerable<string> TemplateNames { get; }

        public virtual string EvaluateTemplate(string templateName, string templateExpression, object templatingContext)
        {
            return string.Empty;
        }

        protected virtual string EvaluateExpression(object templateObject, string templateExpression, object templatingContext, string defaultUnit = null)
        {
            /* 
             * templateObject can be one of the following:
             * 
             *      - a content (node)      : if no expression is provided, we return its id. If there is an expression,
             *                                it has to start with a property. We evaluate that property and continue
             *                                with that in a recursive call.
             *      - datetime              : we format it using the common format known by Lucene. If there is an 
             *                                expression (e.g. add 3 days), we evaluate that first.
             *      - value type or string  : return their string representation (after applying the modifier if
             *                                possible, e.g. in case of numbers).
             * 
             *  Expression examples:
             * 
             *  Deadline					    : single property
             *  Workspace.Deadline				: chained properties
             *  Manager.Workspace.Deadline		: chained properties
             *  +3months                        : value modifier
             *  Deadline+3months                : property and modifier
             *  Workspace.Deadline+3days        : chained properties and a modifier
             *  
             * defaultUnit: this should be provided by the caller in case of datetime templates (e.g. CurrentDate or a date field),
             *              because if the expression does not contain a unit (e.g. DateField+4), we have to know how much
             *              should we add to the base value (4 minutes or 4 days?).
             */

            if (templateObject == null)
                return string.Empty;

            var content = templateObject as GenericContent;
            if (content == null)
            {
                // In case of single reference fields there is a possibility
                // that the value returned by the API is not a single node
                // but a list of nodes. For that case we have to use the
                // first (and most likely the only one) item in the list.
                var contentArray = templateObject as IEnumerable<Node>;
                if (contentArray != null)
                    content = contentArray.FirstOrDefault() as GenericContent;
            }

            // no expression: convert the object to its string representation
            if (string.IsNullOrEmpty(templateExpression))
            {
                // handle nodes: return with their id
                if (content != null)
                    return content.Id.ToString();

                // handle date values
                if (templateObject is DateTime date)
                {
                    return FormatDateTime(date);
                }

                // any other type (e.g. strings like Path, numbers like Index, etc.)
                return templateObject.ToString();
            }

            // check if the first part is a property: find the first separator
            var match = Regex.Match(templateExpression, TemplateManager.TEMPLATE_PROPERTY_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (match.Success)
            {
                // we cannot evaluate a property of something that is not a content
                if (content == null)
                    return string.Empty;

                var propGroup = match.Groups[RegexGroupNames.Property];
                var remainingExpression = templateExpression.Length == propGroup.Length ? null : templateExpression.Substring(propGroup.Length).TrimStart('.');

                // load the property (e.g. a Deadline field) and evaluate it recursively
                return EvaluateExpression(content.GetProperty(propGroup.Value), remainingExpression, templatingContext);
            }

            // the base object is a content (e.g. a user or workspace), but we cannot apply a modifier to a content
            if (content != null)
                return content.Id.ToString();

            // parse modifier
            match = Regex.Match(templateExpression, TemplateManager.TEMPLATE_EXPRESSION_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // unknown expression
            if (!match.Success)
                return templateObject.ToString();

            var modifierPart = match.Groups[RegexGroupNames.Modifier];

            // no modifier found
            if (modifierPart == null || !modifierPart.Success)
                return templateObject.ToString();

            // evaluate the modifier part, e.g. '+3days' or '.AddDays(3)'
            var opGroup = match.Groups[RegexGroupNames.Operator];
            var methodOperatorGroup = match.Groups[RegexGroupNames.MethodOperator];

            // short syntax: '+3days'
            if (opGroup.Success)
            {
                var op = opGroup.Value;
                var number = int.Parse(match.Groups[RegexGroupNames.Number].Value);           // there is always a number here, according to the regex
                var unitGroup = match.Groups[RegexGroupNames.Unit];
                var unit = unitGroup == null || !unitGroup.Success ? (defaultUnit ?? string.Empty) : unitGroup.Value.ToLowerInvariant();

                return EvaluateModifier(templateObject, op, number, unit);
            }

            // method syntax: '.AddDays(3)'
            if (methodOperatorGroup.Success)
            {
                var op = methodOperatorGroup.Value.ToLowerInvariant();
                var number = int.Parse(match.Groups[RegexGroupNames.Parameter].Value);        // there is always a number here, according to the regex
                var unitGroup = match.Groups[RegexGroupNames.MethodUnit];
                var unit = unitGroup == null || !unitGroup.Success ? (defaultUnit ?? string.Empty) : unitGroup.Value.ToLowerInvariant();

                return EvaluateModifier(templateObject, op, number, unit);
            }

            return string.Empty;
        }

        protected virtual string EvaluateModifier(object templateObject, string oprtr, int number, string unit)
        {
            // determines whether the value will be added or subtracted from the base (e.g. +3days or -2months)
            int multiplier;

            switch (oprtr)
            {
                case "+":
                case "add":
                case "plus":
                    multiplier = 1;
                    break;
                case "-":
                case "minus":
                case "subtract":
                case "substract":
                    multiplier = -1;
                    break;
                default:
                    // unknown operator, stop evaluation
                    return templateObject.ToString();
            }

            if (templateObject is DateTime)
            {
                var date = (DateTime)templateObject;
                switch (unit)
                {
                    case "":
                        // if there was no unit in the template and there was no default unit given either
                        date = date.AddDays(multiplier * number);
                        break;
                    case "s":
                    case "sec":
                    case "secs":
                    case "seconds":
                        date = date.AddSeconds(multiplier * number);
                        break;
                    case "m":
                    case "mins":
                    case "minute":
                    case "minutes":
                        date = date.AddMinutes(multiplier * number);
                        break;
                    case "h":
                    case "hour":
                    case "hours":
                        date = date.AddHours(multiplier * number);
                        break;
                    case "d":
                    case "day":
                    case "days":
                        date = date.AddDays(multiplier * number);
                        break;
                    case "workdays":
                        date = date.AddWorkdays(multiplier * number);
                        break;
                    case "month":
                    case "months":
                        date = date.AddMonths(multiplier * number);
                        break;
                    case "w":
                    case "week":
                    case "weeks":
                        date = date.AddDays(multiplier * number * 7);
                        break;
                    case "y":
                    case "year":
                    case "years":
                        date = date.AddYears(multiplier * number);
                        break;
                }

                return FormatDateTime(date);
            }

            if (templateObject is int)
                return (Convert.ToInt32(templateObject) + (multiplier * number)).ToString();

            // unknown type
            return templateObject.ToString();
        }

        protected virtual string FormatDateTime(DateTime date)
        {
            return date.ToContentQueryString();
        }
    }
}
