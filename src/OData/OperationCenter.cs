using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    public class OperationCenter // DefaultActionResolver
    {
        private static readonly OperationInfo[] EmptyMethods = new OperationInfo[0];
        private static readonly JsonSerializer ValueDeserializer = JsonSerializer.Create(
            new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });

        internal static readonly Dictionary<string, OperationInfo[]> Operations =
            new Dictionary<string, OperationInfo[]>();
        public static Type[] SystemParameters { get; internal set; }

        public static void Initialize(Type[] systemParameters)
        {
            SystemParameters = systemParameters;
            Discover();
        }
        internal static void Discover()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetExportedTypes())
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                            AddMethod(method);
                }
                catch (NotSupportedException)
                {
                }
            }
        }
        internal static OperationInfo AddMethod(MethodBase method)
        {
            var attributes = method.GetCustomAttributes().ToArray();
            return AddMethod(method, attributes);
        }
        internal static OperationInfo AddMethod(MethodBase method, Attribute[] attributes)
        {
            var parameters = method.GetParameters().Where(p => !SystemParameters.Contains(p.ParameterType)).ToArray();
            var req = parameters.Where(x => !x.IsOptional).ToArray();
            var opt = parameters.Where(x => x.IsOptional).ToArray();
            var info = new OperationInfo(method, attributes)
            {
                RequiredParameterNames = req.Select(x => x.Name).ToArray(),
                RequiredParameterTypes = req.Select(x => x.ParameterType).ToArray(),
                OptionalParameterNames = opt.Select(x => x.Name).ToArray(),
                OptionalParameterTypes = opt.Select(x => x.ParameterType).ToArray(),
            };
            return AddMethod(info);
        }
        private static OperationInfo AddMethod(OperationInfo info)
        {
            if (!(info.Attributes.Any(a => a is ODataFunction || a is ODataAction)))
                return null;

            if (info.RequiredParameterNames.Length == 0)
                return null;
            if (info.RequiredParameterTypes[0] != typeof(Content))
                return null;

            info.RequiredParameterNames = info.RequiredParameterNames.Skip(1).ToArray();
            info.RequiredParameterTypes = info.RequiredParameterTypes.Skip(1).ToArray();

            // This is a custom dynamic array implementation. 
            // Reason: The single / overloaded method rate probably very high (a lot of single vs a few overloads).
            // Therefore the usual List<T> approach is ineffective because the most List<T> item will contain
            // many unnecessary empty pointers.
            if (!Operations.TryGetValue(info.Method.Name, out var methods))
            {
                methods = new[] { info };
                Operations.Add(info.Method.Name, methods);
            }
            else
            {
                var copy = new OperationInfo[methods.Length + 1];
                methods.CopyTo(copy, 0);
                copy[copy.Length - 1] = info;
                Operations[info.Method.Name] = copy;
            }

            return info;
        }

        public static OperationCallingContext GetMethodByRequest(Content content, string methodName, string requestBody)
        {
            return GetMethodByRequest(content, methodName, Read(requestBody));
        }
        internal static OperationCallingContext GetMethodByRequest(Content content, string methodName, JObject requestParameters)
        {
            var requestParameterNames = requestParameters == null
            ? new string[0]
            : requestParameters.Properties().Select(p => p.Name).ToArray();

            var candidates = GetCandidatesByName(methodName);
            if (candidates.Length > 0)
            {
                //UNDONE: concatenate where clauses before freeze the feature.
                candidates = candidates.Where(x => AllRequiredParametersExist(x, requestParameterNames)).ToArray();
                candidates = candidates.Where(x => FilterByContentTypes(x.ContentTypes, content.ContentType.Name)).ToArray();
                candidates = candidates.Where(x => FilterByRolesAndPermissions(x.Roles, x.RequiredPermissions, content, User.Current)).ToArray();
            }

            // If there is no any candidates, throw: Operation not found ERROR
            if (candidates.Length == 0)
                throw new OperationNotFoundException("Operation not found: " + GetRequestSignature(methodName, requestParameterNames));

            // Search candidates by parameter types
            // Phase-1: search complete type match (strict)
            var contexts = new List<OperationCallingContext>();
            foreach (var candidate in candidates)
                if (TryParseParameters(candidate, content, requestParameters, true, out var context))
                    contexts.Add(context);

            if (contexts.Count == 0)
            {
                // Phase-2: search convertible type match
                foreach (var candidate in candidates)
                    if (TryParseParameters(candidate, content, requestParameters, false, out var context))
                        contexts.Add(context);
            }

            if (contexts.Count == 0)
                throw new OperationNotFoundException("Operation not found: " + GetRequestSignature(methodName, requestParameterNames));

            if (contexts.Count > 1)
                throw new AmbiguousMatchException($"Ambiguous call: {GetRequestSignature(methodName, requestParameterNames)} --> {GetMethodSignatures(contexts)}");

            return contexts[0];
        }

        private static bool FilterByContentTypes(string[] allowedContentTypes, string currentContentType)
        {
            if (allowedContentTypes.Length == 0)
                return true;
            return allowedContentTypes.Contains(currentContentType);
        }
        private static bool FilterByRolesAndPermissions(string[] roles, string[] permissions, Content content, IUser user)
        {
            if (roles.Length > 0 && !OperationInspector.Instance.CheckByRoles(user, roles))
                return false;
            if (permissions.Length > 0 && !OperationInspector.Instance.CheckByPermissions(content, user, permissions))
                return false;
            return true;
        }

        private static OperationInfo[] GetCandidatesByName(string methodName)
        {
            if (Operations.TryGetValue(methodName, out var methods))
                return methods;
            return EmptyMethods;
        }
        private static bool AllRequiredParametersExist(OperationInfo info, string[] requestParameterNames)
        {
            foreach (var requiredParameterName in info.RequiredParameterNames)
                if (!requestParameterNames.Contains(requiredParameterName))
                    return false;
            return true;
        }
        private static bool TryParseParameters(OperationInfo candidate, Content content, JObject requestParameters, bool strict, out OperationCallingContext context)
        {
            context = new OperationCallingContext(content, candidate);

            // Foreach all optional parameters of the method
            for (int i = 0; i < candidate.OptionalParameterNames.Length; i++)
            {
                var name = candidate.OptionalParameterNames[i];

                // If does not exist in the request: continue (move the next parameter)
                if (!requestParameters.TryGetValue(name, out var value))
                    continue;

                // If parse request by parameter"s type is not successful: return false
                var type = candidate.OptionalParameterTypes[i];
                if (!TryParseParameter(type, value, strict, out var parsed))
                    return false;

                // Add parameter name/value to the calling context
                context.SetParameter(name, parsed);
            }
            // Foreach all required parameters of the method
            for (int i = 0; i < candidate.RequiredParameterNames.Length; i++)
            {
                var name = candidate.RequiredParameterNames[i];
                var value = requestParameters[name];
                var type = candidate.RequiredParameterTypes[i];

                // If parse request by parameter"s type is not successful: return false
                if (!TryParseParameter(type, value, strict, out var parsed))
                    return false;

                // Add parameter name/value to the calling context
                context.SetParameter(name, parsed);
            }
            return true;
        }
        private static bool TryParseParameter(Type type, JToken token, bool strict, out object parsed)
        {
            if (type == GetTypeAndValue(type, token, out parsed))
                return true;

            if (!strict)
            {
                if (token.Type == JTokenType.String)
                {
                    var stringValue = token.Value<string>();
                    if (type == typeof(int))
                    {
                        if (int.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (type == typeof(bool))
                    {
                        if (bool.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (type == typeof(decimal))
                    {
                        if (decimal.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                        if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (type == typeof(float))
                    {
                        if (float.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                        if (float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (type == typeof(double))
                    {
                        if (double.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                        if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    //TODO: try parse further opportunities from string to "type"
                }
            }

            parsed = null;
            return false;
        }
        private static Type GetTypeAndValue(Type expectedType, JToken token, out object value)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    value = token.Value<string>();
                    return typeof(string);
                case JTokenType.Integer:
                    value = token.Value<int>();
                    return typeof(int);
                case JTokenType.Boolean:
                    value = token.Value<bool>();
                    return typeof(bool);
                case JTokenType.Float:
                    if (expectedType == typeof(float))
                    {
                        value = token.Value<float>();
                        return typeof(float);
                    }
                    if (expectedType == typeof(decimal))
                    {
                        value = token.Value<decimal>();
                        return typeof(decimal);
                    }
                    value = token.Value<double>();
                    return typeof(double);

                case JTokenType.Object:
                    try
                    {
                        value = token.ToObject(expectedType, ValueDeserializer);
                        return expectedType;
                    }
                    catch (JsonSerializationException)
                    {
                        value = null;
                        return typeof(object);
                    }

                //UNDONE: handle array
                //case JTokenType.Array: break;

                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Undefined:
                    value = expectedType.IsValueType ? Activator.CreateInstance(expectedType) : null;
                    return expectedType;

                case JTokenType.Date:
                case JTokenType.Guid:
                case JTokenType.TimeSpan:
                case JTokenType.Uri:
                    value = token.Value<string>();
                    return typeof(string);

                //case JTokenType.Constructor:
                //case JTokenType.Property:
                //case JTokenType.Comment:
                //case JTokenType.Raw:
                //case JTokenType.Bytes:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private static string GetRequestSignature(string methodName, IEnumerable<string> parameterNames)
        {
            return $"{methodName}({string.Join(",", parameterNames)})";
        }
        private static string GetMethodSignatures(List<OperationCallingContext> contexts)
        {
            return string.Join(", ", contexts.Select(c => c.Operation.ToString()));
        }

        public static object Invoke(OperationCallingContext context)
        {
            var method = context.Operation.Method;
            var methodParams = method.GetParameters();
            var paramValues = new object[methodParams.Length];
            paramValues[0] = context.Content;

            for (int i = 1; i < methodParams.Length; i++)
            {
                if (!context.Parameters.TryGetValue(methodParams[i].Name, out paramValues[i]))
                {
                    //UNDONE: Resolve system parameters by a dynamic way (this class maybe don't know these types)
                    if (methodParams[i].ParameterType == typeof(HttpContext))
                        paramValues[i] = context.HttpContext;
                    else if (methodParams[i].ParameterType == typeof(ODataRequest))
                        paramValues[i] = context.HttpContext.GetODataRequest();
                    else
                        paramValues[i] = methodParams[i].DefaultValue;
                }
            }

            var policies = context.Operation.Policies;
            if (policies.Length > 0 && !OperationInspector.Instance.CheckPolicies(User.Current, policies, context))
                throw new UnauthorizedAccessException(); //UNDONE:? 404, 503?

            return method.Invoke(null, paramValues);
        }

        /* ====================================================================== */

        /// <summary>
        /// Helper method for deserializing the given string representation.
        /// </summary>
        /// <param name="models">JSON object that will be deserialized.</param>
        /// <returns>Deserialized JObject instance.</returns>
        internal static JObject Read(string models) //UNDONE: Use the existing ODataMiddleware method.
        {
            if (string.IsNullOrEmpty(models))
                return null;

            var firstChar = models.Last() == ']' ? '[' : '{';
            var p = models.IndexOf(firstChar);
            if (p > 0)
                models = models.Substring(p);

            var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
            var serializer = JsonSerializer.Create(settings);
            var jReader = new JsonTextReader(new StringReader(models));
            var deserialized = serializer.Deserialize(jReader);

            if (deserialized is JObject jObject)
                return jObject;
            if (deserialized is JArray jArray)
                return jArray[0] as JObject;

            throw new SnNotSupportedException();
        }
    }
}
