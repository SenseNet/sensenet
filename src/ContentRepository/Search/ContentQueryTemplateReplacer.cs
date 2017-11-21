using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;

namespace SenseNet.Search
{
    //UNDONE:!!!! XMLDOC ContentRepository
    public class ContentQueryTemplateReplacer : TemplateReplacerBase
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        public override IEnumerable<string> TemplateNames => new[]
        {
            "currentuser", "currentdate", "currentday", "today", "currenttime", "currentmonth", "currentweek",
            "currentyear", "yesterday", "tomorrow", "nextworkday", "nextweek", "nextmonth", "nextyear",
            "previousworkday", "previousweek", "previousmonth", "previousyear"
        };

        //UNDONE:!!!! XMLDOC ContentRepository
        public override string EvaluateTemplate(string templateName, string templateExpression, object templatingContext)
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

        //UNDONE:!!!! XMLDOC ContentRepository
        protected static class DefaultUnits
        {
            public static readonly string Seconds = "seconds";
            public static readonly string Minutes = "minutes";
            public static readonly string Days = "days";
            public static readonly string Workdays = "workdays";
            public static readonly string Weeks = "weeks";
            public static readonly string Months = "months";
            public static readonly string Years = "years";
        }
    }
}
