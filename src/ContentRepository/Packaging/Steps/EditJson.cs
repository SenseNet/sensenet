using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SenseNet.Packaging.Steps
{
    public class EditJson : EditText
    {
        [DefaultProperty]
        public string Value { get; set; }

        protected override string Edit(string text, ExecutionContext context)
        {
            // no change, return the original text
            if (string.IsNullOrEmpty(Value))
                return text;

            var jo = JsonConvert.DeserializeObject<JObject>(text ?? string.Empty);

            // merge the provided json into the original
            var newJson = JsonConvert.DeserializeObject<JObject>(Value);
            jo.Merge(newJson);

            return jo.ToString();
        }
    }
}
