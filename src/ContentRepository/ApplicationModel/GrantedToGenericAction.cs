using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataAction : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataFunction : Attribute
    {
    }

}
