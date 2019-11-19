using System;
using System.Collections;
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
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    //UNDONE: one class per file.
    internal class OperationMethodStorage : IOperationMethodStorage
    {
        public IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content content, string scenario)
        {
            var stored = storedActions.ToArray();
            var operationMethods = OperationCenter.Operations
                .SelectMany(x => x.Value)
                .Where(x => AllowedName(x.Method.Name, stored))
                .Where(x => IsRelevantContentType(content.ContentType, x.ContentTypes))
                .Select(x => new ODataOperationMethodAction(x));

            return stored.Union(operationMethods).ToArray();
        }

        private bool AllowedName(string operationName, ActionBase[] stored)
        {
            for (int i = 0; i < stored.Length; i++)
                if (stored[i].Name == operationName)
                    return false;
            return true;
        }
        private bool IsRelevantContentType(ContentType contentType, string[] allowedContentTypeNames)
        {
            if (allowedContentTypeNames.Length == 0)
                return true;

            for (int i = 0; i < allowedContentTypeNames.Length; i++)
            {
                var existingContentType = ContentType.GetByName(allowedContentTypeNames[i]);
                if (existingContentType == null)
                    continue;
                if (contentType.IsInstaceOfOrDerivedFrom(existingContentType.Name))
                    return true;
            }

            return false;
        }
    }

    public class OperationCenter
    {
        private static readonly OperationInfo[] EmptyMethods = new OperationInfo[0];
        private static readonly JsonSerializer ValueDeserializer = JsonSerializer.Create(
            new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });

        private static readonly Type[] AllowedArrayElementTypes = new[]
        {
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(decimal),
            typeof(float),
            typeof(bool),
            typeof(object),
        };

        internal static readonly Dictionary<string, OperationInfo[]> Operations =
            new Dictionary<string, OperationInfo[]>();

        public static Type[] SystemParameters { get; } = {typeof(HttpContext), typeof(ODataRequest)};

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
            var parameters = method.GetParameters();
            var req = new List<ParameterInfo>();
            var opt = new List<ParameterInfo>();
            foreach (var parameter in parameters)
            {
                var paramType = parameter.ParameterType;
                if (SystemParameters.Contains(parameter.ParameterType))
                    continue;
                if (!IsParameterTypeAllowed(paramType))
                    return null;
                if (parameter.IsOptional)
                    opt.Add(parameter);
                else
                    req.Add(parameter);
            }

            var info = new OperationInfo(method, attributes)
            {
                RequiredParameterNames = req.Select(x => x.Name).ToArray(),
                RequiredParameterTypes = req.Select(x => x.ParameterType).ToArray(),
                OptionalParameterNames = opt.Select(x => x.Name).ToArray(),
                OptionalParameterTypes = opt.Select(x => x.ParameterType).ToArray(),
            };
            return AddMethod(info);
        }

        private static bool IsParameterTypeAllowed(Type type)
        {
            if (type.IsArray)
            {
                var eType = type.GetElementType();
                return AllowedArrayElementTypes.Contains(eType);
            }
            return true;
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
            return GetMethodByRequest(content, methodName, ODataMiddleware.Read(requestBody));
        }
        internal static OperationCallingContext GetMethodByRequest(Content content, string methodName, JObject requestParameters)
        {
            var requestParameterNames = requestParameters == null
            ? new string[0]
            : requestParameters.Properties().Select(p => p.Name).ToArray();

            var candidates = GetCandidatesByName(methodName);
            if (candidates.Length > 0)
            {
                candidates = candidates.Where(x =>
                        AllRequiredParametersExist(x, requestParameterNames) &&
                        FilterByContentTypes(x.ContentTypes, content.ContentType.Name) &&
                        FilterByRolesAndPermissions(x.Roles, x.RequiredPermissions, content, User.Current))
                    .ToArray();
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

                case JTokenType.Array:
                    var array = (JArray)token;
                    value = null;
                    try
                    {
                        if (expectedType == typeof(string[])) value = array.Select(x => x.ToObject<string>()).ToArray();
                        else if (expectedType == typeof(int[])) value = array.Select(x => x.ToObject<int>()).ToArray();
                        else if (expectedType == typeof(long[])) value = array.Select(x => x.ToObject<long>()).ToArray();
                        else if (expectedType == typeof(bool[])) value = array.Select(x => x.ToObject<bool>()).ToArray();
                        else if (expectedType == typeof(float[])) value = array.Select(x => x.ToObject<float>()).ToArray();
                        else if (expectedType == typeof(double[])) value = array.Select(x => x.ToObject<double>()).ToArray();
                        else if (expectedType == typeof(decimal[])) value = array.Select(x => x.ToObject<decimal>()).ToArray();

                        else if (expectedType == typeof(List<string>)) value = array.Select(x => x.ToObject<string>()).ToList();
                        else if (expectedType == typeof(List<int>)) value = array.Select(x => x.ToObject<int>()).ToList();
                        else if (expectedType == typeof(List<long>)) value = array.Select(x => x.ToObject<long>()).ToList();
                        else if (expectedType == typeof(List<bool>)) value = array.Select(x => x.ToObject<bool>()).ToList();
                        else if (expectedType == typeof(List<float>)) value = array.Select(x => x.ToObject<float>()).ToList();
                        else if (expectedType == typeof(List<double>)) value = array.Select(x => x.ToObject<double>()).ToList();
                        else if (expectedType == typeof(List<decimal>)) value = array.Select(x => x.ToObject<decimal>()).ToList();

                        else if (expectedType == typeof(IEnumerable<string>)) value = array.Select(x => x.ToObject<string>()).ToArray();
                        else if (expectedType == typeof(IEnumerable<int>)) value = array.Select(x => x.ToObject<int>()).ToArray();
                        else if (expectedType == typeof(IEnumerable<long>)) value = array.Select(x => x.ToObject<long>()).ToArray();
                        else if (expectedType == typeof(IEnumerable<bool>)) value = array.Select(x => x.ToObject<bool>()).ToArray();
                        else if (expectedType == typeof(IEnumerable<float>)) value = array.Select(x => x.ToObject<float>()).ToArray();
                        else if (expectedType == typeof(IEnumerable<double>)) value = array.Select(x => x.ToObject<double>()).ToArray();
                        else if (expectedType == typeof(IEnumerable<decimal>)) value = array.Select(x => x.ToObject<decimal>()).ToArray();

                        else
                        {
                            value = array.Select(x => x.ToObject<object>()).ToArray();
                            return typeof(object[]);
                        }
                        return expectedType;
                    }
                    catch
                    {
                        // ignored
                    }
                    value = array;
                    return typeof(object);

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
                throw new UnauthorizedAccessException();

            return method.Invoke(null, paramValues);
        }
    }
}
