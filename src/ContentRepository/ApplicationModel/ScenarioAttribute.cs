using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ApplicationModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ScenarioAttribute : Attribute
    {
        private string _name;
        public string Name { get { return _name; } }

        private bool _allowSingleton;
        public bool AllowSingleton { get { return _allowSingleton; } }

        public ScenarioAttribute(string name, bool allowSingleton = true)
        {
            _name = name;
            _allowSingleton = allowSingleton;
        }
    }
}
