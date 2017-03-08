using System.Text.RegularExpressions;

namespace SenseNet.Packaging.Steps
{
    public class ReplaceText : EditText
    {
        private static readonly string CDATAStart = "<![CDATA[";
        private static readonly string CDATAEnd = "]]>";

        [DefaultProperty]
        public string Value { get; set; }
        public string Template { get; set; }
        public string Regex { get; set; }

        public override void Execute(ExecutionContext context)
        {
            if ((string.IsNullOrEmpty(Template) && string.IsNullOrEmpty(Regex)) ||
                (!string.IsNullOrEmpty(Template) && !string.IsNullOrEmpty(Regex)))
                throw new PackagingException(SR.Errors.InvalidParameters);

            base.Execute(context);
        }

        protected override string Edit(string text, ExecutionContext context)
        {
            return Replace(text, context);
        }

        protected virtual string Replace(string text, ExecutionContext context)
        {
            if (text == null)
                return string.Empty;

            return !string.IsNullOrEmpty(Regex) 
                ? System.Text.RegularExpressions.Regex.Replace(text, Regex, GetReplacement(context), RegexOptions.Singleline) 
                : text.Replace(Template, GetReplacement(context));
        }

        private string GetReplacement(ExecutionContext context)
        {
            var text = Value ?? string.Empty;

            // CDATA workaround
            if (text.StartsWith(CDATAStart) && text.EndsWith(CDATAEnd))
                text = text.Substring(CDATAStart.Length, text.Length - (CDATAStart.Length + CDATAEnd.Length));

            return (string)context.ResolveVariable(text);
        }
    }
}
