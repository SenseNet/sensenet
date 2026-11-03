using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SenseNet.Services.Core.Diagnostics;

internal static class HealthDataSanitizer
{
    private static readonly Regex SecretAssignment = new(
        "(?i)(password|pwd|user id|username|accountkey|sharedaccesssignature|clientsecret|client_secret|apikey|api_key|token)\\s*[=:]\\s*(\"[^\"]*\"|'[^']*'|[^;\\s,&]+)",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex UserInfo = new("(://)[^/\\s@]+@", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    internal static string Text(string value)
    {
        if (value == null) return null;
        // Bound arbitrary provider output before applying regular expressions.
        if (value.Length > 4096) value = value.Substring(0, 4096);
        try { return UserInfo.Replace(SecretAssignment.Replace(value, "$1=***"), "$1***@"); }
        catch (RegexMatchTimeoutException) { return "[redacted]"; }
    }
    internal static string SerializeDetails(object value)
    {
        var token = value == null ? new JObject() : JToken.FromObject(value);
        Sanitize(token);
        return (token is JObject ? token : new JObject { ["Configuration"] = token }).ToString(Formatting.None);
    }
    private static void Sanitize(JToken token)
    {
        if (token is JObject obj)
            foreach (var property in obj.Properties().ToArray())
            {
                var key = property.Name.Replace("_", "").Replace("-", "").ToLowerInvariant();
                if (key.Contains("password") || key.Contains("secret") || key.Contains("token") || key.Contains("apikey") ||
                    key.Contains("accountkey") || key.Contains("sharedaccesssignature") || key is "pwd" or "userid" or "username")
                    property.Value = "***";
                else Sanitize(property.Value);
            }
        else if (token is JArray array)
            foreach (var item in array) Sanitize(item);
        else if (token is JValue { Type: JTokenType.String } text)
            text.Value = Text((string)text.Value);
    }
}
