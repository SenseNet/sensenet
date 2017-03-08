using System.Text;
using System.Globalization;
using SenseNet.ContentRepository.i18n;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.Portal.Resources
{
    public static class ResourceScripter
    {
        public static string RenderResourceScript(string className, CultureInfo cultureInfo)
        {
            var jsClassName = "SN.Resources[\"" + className.Replace("\"", "\\\"") + "\"]";
            var values = SenseNetResourceManager.Current.GetClassItems(className, cultureInfo);
            var sb = new StringBuilder("var SN=SN||{};SN.Resources=SN.Resources||{};" + jsClassName + "=");

            if ((values == null || values.Count == 0) && !cultureInfo.IsNeutralCulture)
            {
                cultureInfo = cultureInfo.Parent;
                values = SenseNetResourceManager.Current.GetClassItems(className, cultureInfo);
            }

            // use some or all of the strings defined for the fallback culture if they are not defined for the main culture
            if (!string.IsNullOrWhiteSpace(RepositoryEnvironment.FallbackCulture))
            {
                cultureInfo = CultureInfo.GetCultureInfo(RepositoryEnvironment.FallbackCulture);
                var fallbackValues = SenseNetResourceManager.Current.GetClassItems(className, cultureInfo);

                if (values == null || values.Count == 0)
                {
                    values = fallbackValues;
                }
                else
                {
                    // add fallback strings that are not present in the main culture
                    foreach (var item in fallbackValues.Where(kvp => !values.ContainsKey(kvp.Key)))
                    {
                        values.Add(item.Key, item.Value);
                    }
                }
            }

            if (values == null)
            {
                sb.Append("null");
            }
            else
            {
                var serializer = new JsonSerializer();
                using (var writer = new StringWriter(sb))
                {
                    serializer.Serialize(writer, values);
                }
            }

            sb.Append(";");

            return sb.ToString();
        }

        public static string GetResourceUrl(string className)
        {
            return "/" + ResourceHandler.UrlPart + "/" + CultureInfo.CurrentUICulture.Name + "/" + className;
        }
    }
}
