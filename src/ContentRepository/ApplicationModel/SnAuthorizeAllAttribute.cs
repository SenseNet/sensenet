using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Declares an authorization rule for an Operation Method that enables the method for anyone.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SnAuthorizeAllAttribute : Attribute
    {
    }
}
