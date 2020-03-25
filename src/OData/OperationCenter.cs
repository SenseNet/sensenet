using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Security;
using ContentOperations = SenseNet.Services.Core.Operations.ContentOperations;

namespace SenseNet.OData
{
    internal class OperationCenter
    {
        internal static readonly bool IsCaseInsensitiveOperationNameEnabled = true;

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

        internal static Dictionary<string, OperationInfo[]> Operations { get; } =
            new Dictionary<string, OperationInfo[]>();

        internal static Dictionary<string, IOperationMethodPolicy> Policies { get; } =
            new Dictionary<string, IOperationMethodPolicy>();

        public static Type[] SystemParameters { get; } = {typeof(HttpContext), typeof(ODataRequest)};

        internal static void Discover()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(ass => !ass.IsDynamic))
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

            var opAttr = attributes.OfType<ODataOperationAttribute>().FirstOrDefault();
            var name = opAttr?.OperationName ?? method.Name;

            var info = new OperationInfo(name, opAttr?.Icon, opAttr?.Description, method, attributes)
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
            if (!info.Attributes.Any(a => a is ODataOperationAttribute))
                return null;

            if (info.RequiredParameterNames.Length == 0)
                return null;
            if (info.RequiredParameterTypes[0] != typeof(Content))
                return null;

            info.RequiredParameterNames = info.RequiredParameterNames.Skip(1).ToArray();
            info.RequiredParameterTypes = info.RequiredParameterTypes.Skip(1).ToArray();

            var operationName = IsCaseInsensitiveOperationNameEnabled ? info.Name.ToLowerInvariant() : info.Name;

            // This is a custom dynamic array implementation. 
            // Reason: The single / overloaded method rate probably very high (a lot of single vs a few overloads).
            // Therefore the usual List<T> approach is ineffective because the most List<T> item will contain
            // many unnecessary empty pointers.
            if (!Operations.TryGetValue(operationName, out var methods))
            {
                methods = new[] { info };
                Operations.Add(operationName, methods);
            }
            else
            {
                var copy = new OperationInfo[methods.Length + 1];
                methods.CopyTo(copy, 0);
                copy[copy.Length - 1] = info;
                Operations[operationName] = copy;
            }

            return info;
        }

        public static OperationCallingContext GetMethodByRequest(Content content, string methodName, string requestBody)
        {
            return GetMethodByRequest(content, methodName, ODataMiddleware.ReadToJson(requestBody), null);
        }
        internal static OperationCallingContext GetMethodByRequest(
            Content content, string methodName, JObject requestParameters, IQueryCollection query)
        {
            var odataParameters = new ODataParameterCollection(requestParameters, query);
            var requestParameterNames = odataParameters.Keys.ToArray();

            var inspector = OperationInspector.Instance;
            var candidates = GetCandidatesByName(methodName);
            if (candidates.Length > 0)
            {
                candidates = candidates.Where(x =>
                        AllRequiredParametersExist(x, requestParameterNames) &&
                        FilterByContentTypes(inspector, content, x.ContentTypes))
                    .ToArray();
            }

            // If there is no any candidates, throw: Operation not found ERROR
            if (candidates.Length == 0)
                throw new OperationNotFoundException("Operation not found: " + GetRequestSignature(methodName, requestParameterNames));

            candidates = candidates.Where(x => FilterByRolesAndPermissions(inspector, x.Roles, x.Permissions, content)).ToArray();
            if (candidates.Length == 0)
                throw new AccessDeniedException(null, null, 0, null, null);

            // Search candidates by parameter types
            // Phase-1: search complete type match (strict)
            var contexts = new List<OperationCallingContext>();
            foreach (var candidate in candidates)
                if (TryParseParameters(candidate, content, odataParameters, true, out var context))
                    contexts.Add(context);

            if (contexts.Count == 0)
            {
                // Phase-2: search convertible type match
                foreach (var candidate in candidates)
                    if (TryParseParameters(candidate, content, odataParameters, false, out var context))
                        contexts.Add(context);
            }

            if (contexts.Count == 0)
                throw new OperationNotFoundException("Operation not found: " + GetRequestSignature(methodName, requestParameterNames));

            if (contexts.Count > 1)
                throw new AmbiguousMatchException($"Ambiguous call: {GetRequestSignature(methodName, requestParameterNames)} --> {GetMethodSignatures(contexts)}");

            return contexts[0];
        }

        private static bool FilterByContentTypes(OperationInspector inspector, Content content, string[] allowedContentTypes)
        {
            if (allowedContentTypes.Length == 0)
                return true;
            return inspector.CheckByContentType(content, allowedContentTypes);
        }
        private static bool FilterByRolesAndPermissions(OperationInspector inspector, string[] roles, string[] permissions, Content content)
        {
            if (roles.Length > 0 && !inspector.CheckByRoles(roles))
                return false;
            if (permissions.Length > 0 && !inspector.CheckByPermissions(content, permissions))
                return false;
            return true;
        }

        private static OperationInfo[] GetCandidatesByName(string methodName)
        {
            var name = IsCaseInsensitiveOperationNameEnabled ? methodName.ToLowerInvariant() : methodName;
            if (Operations.TryGetValue(name, out var methods))
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
        private static bool TryParseParameters(OperationInfo candidate, Content content, ODataParameterCollection odataParameters, bool strict, out OperationCallingContext context)
        {
            context = new OperationCallingContext(content, candidate);

            // If there is no any request parameter, the validity of candidate depends on the required parameter count.
            if (odataParameters == null)
                return candidate.RequiredParameterNames.Length == 0;

            // Foreach all optional parameters of the method
            for (int i = 0; i < candidate.OptionalParameterNames.Length; i++)
            {
                var name = candidate.OptionalParameterNames[i];

                // If does not exist in the request: continue (move the next parameter)
                if (!odataParameters.TryGetValue(name, out var value))
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
                var value = odataParameters[name];
                var type = candidate.RequiredParameterTypes[i];

                // If parse request by parameter"s type is not successful: return false
                if (!TryParseParameter(type, value, strict, out var parsed))
                    return false;

                // Add parameter name/value to the calling context
                context.SetParameter(name, parsed);
            }
            return true;
        }
        private static bool TryParseParameter(Type expectedType, ODataParameterValue parameter, bool strict, out object parsed)
        {
            if (expectedType == GetTypeAndValue(expectedType, parameter, out parsed))
                return true;

            Type nullableBaseType;
            if ((nullableBaseType = Nullable.GetUnderlyingType(expectedType)) != null)
                expectedType = nullableBaseType;

            if (!strict)
            {
                if (parameter.Type == JTokenType.String)
                {
                    var stringValue = parameter.Value<string>();
                    if (expectedType == typeof(int))
                    {
                        if (int.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (expectedType == typeof(long))
                    {
                        if (long.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (expectedType == typeof(byte))
                    {
                        if (byte.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (expectedType == typeof(bool))
                    {
                        if (bool.TryParse(stringValue, out var v))
                        {
                            parsed = v;
                            return true;
                        }
                    }
                    if (expectedType == typeof(decimal))
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
                    if (expectedType == typeof(float))
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
                    if (expectedType == typeof(double))
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
                    //TODO: try parse further opportunities from string to "expectedType"
                }
            }

            parsed = null;
            return false;
        }
        private static Type GetTypeAndValue(Type expectedType, ODataParameterValue parameter, out object value)
        {
            // The only exceptional case.
            if (expectedType == typeof(ContentOperations.SetPermissionsRequest))
            {
                value = parameter.ToObject<ContentOperations.SetPermissionsRequest>();
                return typeof(ContentOperations.SetPermissionsRequest);
            }

            switch (parameter.Type)
            {
                case JTokenType.String:
                    value = parameter.Value<string>();
                    return typeof(string);
                case JTokenType.Integer:
                    if (expectedType == typeof(int?))
                    {
                        value = parameter.Value<int?>();
                        return typeof(int?);
                    }
                    if (expectedType == typeof(long))
                    {
                        value = parameter.Value<long>();
                        return typeof(long);
                    }
                    if (expectedType == typeof(long?))
                    {
                        value = parameter.Value<long?>();
                        return typeof(long?);
                    }
                    if (expectedType == typeof(byte))
                    {
                        value = parameter.Value<byte>();
                        return typeof(byte);
                    }
                    if (expectedType == typeof(byte?))
                    {
                        value = parameter.Value<byte?>();
                        return typeof(byte?);
                    }
                    value = parameter.Value<int>();
                    return typeof(int);
                case JTokenType.Boolean:
                    if (expectedType == typeof(bool?))
                    {
                        value = parameter.Value<bool?>();
                        return typeof(bool?);
                    }
                    value = parameter.Value<bool>();
                    return typeof(bool);
                case JTokenType.Float:
                    if (expectedType == typeof(float))
                    {
                        value = parameter.Value<float>();
                        return typeof(float);
                    }
                    if (expectedType == typeof(float?))
                    {
                        value = parameter.Value<float?>();
                        return typeof(float?);
                    }
                    if (expectedType == typeof(decimal))
                    {
                        value = parameter.Value<decimal>();
                        return typeof(decimal);
                    }
                    if (expectedType == typeof(decimal?))
                    {
                        value = parameter.Value<decimal?>();
                        return typeof(decimal?);
                    }
                    if (expectedType == typeof(double?))
                    {
                        value = parameter.Value<double?>();
                        return typeof(double?);
                    }
                    value = parameter.Value<double>();
                    return typeof(double);
                case JTokenType.Object:
                    try
                    {
                        value = parameter.ToObject(expectedType, ValueDeserializer);
                        return expectedType;
                    }
                    catch (JsonSerializationException)
                    {
                        value = null;
                        return typeof(object);
                    }

                case JTokenType.Array:
                    value = parameter.ToArray(expectedType, out var realType);
                    return realType;
                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Undefined:
                    value = expectedType.IsValueType ? Activator.CreateInstance(expectedType) : null;
                    return expectedType;

                case JTokenType.Date:
                case JTokenType.Guid:
                case JTokenType.TimeSpan:
                case JTokenType.Uri:
                    value = parameter.Value<string>();
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

        /* ===================================================================================== INVOKE */

        public static object Invoke(OperationCallingContext context)
        {
            var parameters = PrepareInvoke(context);

            try
            {
                return context.Operation.Method.Invoke(null, parameters);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException == null)
                    throw;
                throw e.InnerException;
            }
        }

        public static async Task<object> InvokeAsync(OperationCallingContext context)
        {
            var parameters = PrepareInvoke(context);

            try
            {
                dynamic awaitable = context.Operation.Method.Invoke(null, parameters);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException == null)
                    throw;
                throw e.InnerException;
            }
        }

        private static object[] PrepareInvoke(OperationCallingContext context)
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

            var inspector = OperationInspector.Instance;
            var operation = context.Operation;
            var user = User.Current;

            // ContentType, role, permission verification is not necessary here because the method candidates are already filtered.

            // The method call is not allowed if there is no authorization rule.
            var roles = operation.Roles;
            var permissions = operation.Permissions;
            var policies = context.Operation.Policies;
            if (user.Id != Identifiers.SystemUserId)
                if (roles.Length + permissions.Length + policies.Length == 0)
                    throw new UnauthorizedAccessException();

            // Execute the code-based policies (if there is any).
            if (policies.Length > 0 && inspector.CheckPolicies(policies, context) < OperationMethodVisibility.Enabled)
                throw new AccessDeniedException(null, null, 0, null, null);

            return paramValues;
        }
    }
}
