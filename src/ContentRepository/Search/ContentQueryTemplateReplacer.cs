﻿using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.Search.Querying;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search
{
    /// <summary>
    /// Provides methods to resolve templates for ContentQuery.
    /// </summary>
    public class ContentQueryTemplateReplacer : TemplateReplacerBase
    {
        /// <summary>
        /// Gets the array of supported template names.
        /// </summary>
        public override IEnumerable<string> TemplateNames => new[]
        {
            "currentuser", "currentdate", "currentday", "today", "currenttime", "currentmonth", "currentweek",
            "currentyear", "yesterday", "tomorrow", "nextworkday", "nextweek", "nextmonth", "nextyear",
            "previousworkday", "previousweek", "previousmonth", "previousyear"
        };

        /// <summary>
        /// Resolve a named template in the given expression.
        /// </summary>
        public override string EvaluateTemplate(string templateName, string templateExpression, object templatingContext)
        {
            var evaluated = EvaluateTemplatePrivate(templateName, templateExpression, templatingContext);
            return string.IsNullOrEmpty(evaluated) ? SnQuery.NotResolvedTemplateText : evaluated;
        }
        public string EvaluateTemplatePrivate(string templateName, string templateExpression, object templatingContext)
        {
            switch (templateName)
            {
                case "currentuser":
                    return EvaluateExpression(User.Current as GenericContent, templateExpression, templatingContext);
                case "currenttime":
                    return EvaluateExpression(DateTime.UtcNow, templateExpression, templatingContext, DefaultUnits.Minutes);
                case "currentdate":
                case "currentday":
                case "today":
                    return EvaluateExpression(DateTime.UtcNow.Date, templateExpression, templatingContext, DefaultUnits.Days);
                case "currentweek":
                    return EvaluateExpression(DateTime.UtcNow.StartOfWeek(), templateExpression, templatingContext, DefaultUnits.Weeks);
                case "currentmonth":
                    return EvaluateExpression(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), templateExpression, templatingContext, DefaultUnits.Months);
                case "currentyear":
                    return EvaluateExpression(new DateTime(DateTime.UtcNow.Year, 1, 1), templateExpression, templatingContext, DefaultUnits.Years);
                case "yesterday":
                    return EvaluateExpression(DateTime.UtcNow.Date.AddDays(-1), templateExpression, templatingContext, DefaultUnits.Days);
                case "tomorrow":
                    return EvaluateExpression(DateTime.UtcNow.Date.AddDays(1), templateExpression, templatingContext, DefaultUnits.Days);
                case "nextworkday":
                    return EvaluateExpression(DateTime.UtcNow.AddWorkdays(1), templateExpression, templatingContext, DefaultUnits.Workdays);
                case "nextweek":
                    return EvaluateExpression(DateTime.UtcNow.StartOfWeek().AddDays(7), templateExpression, templatingContext, DefaultUnits.Weeks);
                case "nextmonth":
                    return EvaluateExpression(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1), templateExpression, templatingContext, DefaultUnits.Months);
                case "nextyear":
                    return EvaluateExpression(new DateTime(DateTime.UtcNow.Year, 1, 1).AddYears(1), templateExpression, templatingContext, DefaultUnits.Years);
                case "previousworkday":
                    return EvaluateExpression(DateTime.UtcNow.AddWorkdays(-1), templateExpression, templatingContext, DefaultUnits.Workdays);
                case "previousweek":
                    return EvaluateExpression(DateTime.UtcNow.StartOfWeek().AddDays(-7), templateExpression, templatingContext, DefaultUnits.Weeks);
                case "previousmonth":
                    return EvaluateExpression(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1), templateExpression, templatingContext, DefaultUnits.Months);
                case "previousyear":
                    return EvaluateExpression(new DateTime(DateTime.UtcNow.Year, 1, 1).AddYears(-1), templateExpression, templatingContext, DefaultUnits.Years);
                default:
                    return base.EvaluateTemplate(templateName, templateExpression, templatingContext);
            }
        }

        /// <summary>
        /// Defines constants for units used in templates.
        /// </summary>
        protected static class DefaultUnits
        {
            /// <summary>Value: "seconds".</summary>
            public static readonly string Seconds = "seconds";
            /// <summary>Value: "minutes".</summary>
            public static readonly string Minutes = "minutes";
            /// <summary>Value: "days".</summary>
            public static readonly string Days = "days";
            /// <summary>Value: "workdays".</summary>
            public static readonly string Workdays = "workdays";
            /// <summary>Value: "weeks".</summary>
            public static readonly string Weeks = "weeks";
            /// <summary>Value: "months".</summary>
            public static readonly string Months = "months";
            /// <summary>Value: "years".</summary>
            public static readonly string Years = "years";
        }
    }
}
