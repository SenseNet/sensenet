using System.Collections.Generic;
using System.Linq;
using System;
using SenseNet.Diagnostics;
using SenseNet.Tools;
// ReSharper disable CheckNamespace

namespace SenseNet.ApplicationModel
{
    public class ScenarioManager
    {
        public static GenericScenario GetScenario(string name)
        {
            return GetScenario(name, null);
        }

        public static GenericScenario GetScenario(string name, string parameters)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var sc = ResolveScenarioType(name);
            if (sc == null)
                return null;

            if (!string.IsNullOrEmpty(parameters))
                sc.Initialize(GetParameters(parameters));

            return sc;
        }

        private static Dictionary<string, object> GetParameters(string parameters)
        {
            return ActionFramework.ParseParameters(parameters);
        }

        // ======================================================================== Sceario type handling

        private static Dictionary<string, object> _scenarioCache;
        private static readonly object ScenarioCacheLock = new object();

        private static GenericScenario ResolveScenarioType(string name)
        {
            if (_scenarioCache == null)
                lock (ScenarioCacheLock)
                    if (_scenarioCache == null)
                        _scenarioCache = BuildScenarioCache();

            if (!_scenarioCache.TryGetValue(name, out object cachedValue))
                return null;

            GenericScenario instance;
            switch (cachedValue)
            {
                case GenericScenario singleton:
                    instance = singleton;
                    break;
                case Type scenarioType:
                    instance = (GenericScenario) Activator.CreateInstance(scenarioType);
                    break;
                default:
                    return null;
            }

            instance.Name = name;
            return instance;
        }

        private static Dictionary<string, object> BuildScenarioCache()
        {
            var container = new Dictionary<string, object>();

            var baseType = typeof(GenericScenario);
            var subTypes = TypeResolver.GetTypesByBaseType(baseType);
            var attributeType = typeof (ScenarioAttribute);

            // register scenarios that have a codebehind
            foreach (var scenarioType in subTypes)
            {
                var name = scenarioType.Name;
                var singleton = true;

                // use the information given by the developer who added a ScenarioAttribute to his scenario class
                if (scenarioType.GetCustomAttributes(attributeType, false).FirstOrDefault() is ScenarioAttribute scAttribute)
                {
                    singleton = scAttribute.AllowSingleton;
                    if (!string.IsNullOrEmpty(scAttribute.Name))
                        name = scAttribute.Name;
                }

                try
                {
                    // Most of the scenarios are stateless, so we can store a singleton instance to handle
                    // all requests for the scenario. If the Scenario type contains its own properties or uses
                    // parameters, so we will have to create a new instance of the type every time.
                    container.Add(name, singleton ? Activator.CreateInstance(scenarioType) : scenarioType);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Error during scenario type registration. Type name: " + scenarioType.FullName);
                }
            }
            
            // collect scenarios that we did not register yet (do not have a codebehind)
            var genericScenarioNames = ApplicationStorage.Instance.ScenarioNames
                .Except(container.Keys).ToArray();
            
            foreach (var genericScenarioName in genericScenarioNames)
            {
                try
                {
                    // register a singleton generic scenario instance for every dynamic scenario
                    container.Add(genericScenarioName, new GenericScenario());
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Error during scenario type registration. Type name: " + genericScenarioName);
                } 
            }

            return container;
        }
    }
}
