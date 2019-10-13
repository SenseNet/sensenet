using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.i18n;

// ReSharper disable once CheckNamespace
namespace SenseNet.Services
{
    // ReSharper disable once InconsistentNaming
    internal class SNSR
    {
        public static string GetString(string fullResourceKey)
        {
            return SenseNetResourceManager.Current.GetString(fullResourceKey);
        }
        public static string GetString(string className, string name)
        {
            return SenseNetResourceManager.Current.GetString(className, name);
        }
        public static string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(SenseNetResourceManager.Current.GetString(fullResourceKey), args);
        }
    }
}
