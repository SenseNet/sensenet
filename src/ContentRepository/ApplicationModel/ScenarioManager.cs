using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using SenseNet.ContentRepository.Storage;
using System;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Tools;

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

        private static UnityContainer _scenarioContainer;
        private static object _scenarioContainerLock = new object();

        private static UnityContainer ScenarioContainer
        {
            get
            {
                if (_scenarioContainer == null)
                {
                    lock (_scenarioContainerLock)
                    {
                        if (_scenarioContainer == null)
                        {
                            _scenarioContainer = GetUnityContainerForScenarios();
                        }
                    }
                }
                return _scenarioContainer;
            }
        }

        private static GenericScenario ResolveScenarioType(string name)
        {
            try
            {
                var sc = ScenarioContainer.Resolve<GenericScenario>(name);

                // set the name
                if (sc != null && string.IsNullOrEmpty(sc.Name))
                    sc.Name = name;

                return sc;
            }
            catch (ResolutionFailedException)
            {
                return null;
            }
        }

        private static UnityContainer GetUnityContainerForScenarios()
        {
            var container = new UnityContainer();

            var baseType = typeof(GenericScenario);
            var subTypes = TypeResolver.GetTypesByBaseType(baseType);
            var attributeType = typeof (ScenarioAttribute);

            // register scenarios that have a codebehind
            foreach (var scenarioType in subTypes)
            {
                var name = scenarioType.Name;
                var singleton = true;

                // use the information given by the developer who added a ScenarioAttribute to his scenario class
                var scAttribute = scenarioType.GetCustomAttributes(attributeType, false).FirstOrDefault() as ScenarioAttribute;
                if (scAttribute != null)
                {
                    singleton = scAttribute.AllowSingleton;
                    if (!string.IsNullOrEmpty(scAttribute.Name))
                        name = scAttribute.Name;
                }

                try
                {
                    if (singleton)
                    {
                        // Most of the scenarios are stateless, so we can store a singleton 
                        // object to handle all requests for the scenario.
                        container.RegisterType(baseType, scenarioType, name, new ContainerControlledLifetimeManager(), new InjectionMember[0]);
                    }
                    else
                    {
                        // Scenario type contains its own properties or uses parameters, so
                        // we will have to create a new instance of the type every time.
                        container.RegisterType(baseType, scenarioType, name);
                    }
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Error during scenario type registration. Type name: " + scenarioType.FullName);
                }
            }
            
            // collect scenarios that we did not register yet (do not have a codebehind)
            var genericScenarios = ApplicationStorage.Instance.ScenarioNames.Except(container.Registrations.Select(reg => reg.Name)).ToArray();
            
            foreach (var genScen in genericScenarios)
            {
                try
                {
                    // register a singleton generic scenario instance for every dynamic scenario
                    container.RegisterType(baseType, baseType, genScen, new ContainerControlledLifetimeManager(), new InjectionMember[0]);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Error during scenario type registration. Type name: " + genScen);
                } 
            }

            return container;
        }
    }
}
