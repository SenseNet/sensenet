using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// On a class: Defines a scenario with a specified name.
    /// On a method: Categorizes the operation method action into one or more scenarios.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ScenarioAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name(s) of the scenario.
        /// On a class: only one name is relevant.
        /// On a method: the value can be a comma separated list.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets whether singleton is enabled or not if the attribute is on a class.
        /// This value is ignored if the attribute is on a method.
        /// </summary>
        public bool AllowSingleton { get; set; }

        /// <summary>
        /// Initializes a new <see cref="ScenarioAttribute"/> instance.
        /// </summary>
        public ScenarioAttribute(string name = null, bool allowSingleton = true)
        {
            Name = name;
            AllowSingleton = allowSingleton;
        }
        public ScenarioAttribute(params string[] names)
        {
            if (names != null)
                Name = string.Join(",", names);
            AllowSingleton = true;
        }
    }
}
