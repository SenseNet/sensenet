﻿using System;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Settings and provider builder class that controls the startup options and provider
    /// instances when a sensenet repository starts.
    /// </summary>
    public class RepositoryBuilder : RepositoryStartSettings, IRepositoryBuilder
    {
        #region IRepositoryBuilder implementation
        
        T IRepositoryBuilder.GetProvider<T>()
        {
            return Configuration.Providers.Instance.GetProvider<T>();
        }
        T IRepositoryBuilder.GetProvider<T>(string name)
        {
            return Configuration.Providers.Instance.GetProvider<T>(name);
        }
        void IRepositoryBuilder.SetProvider(string providerName, object provider)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentNullException(nameof(provider));

            Configuration.Providers.Instance.SetProvider(providerName, provider);
            WriteLog(providerName, provider);
        }
        void IRepositoryBuilder.SetProvider(object provider)
        {
            if (provider == null)
                return;

            var providerType = provider.GetType();
            Configuration.Providers.Instance.SetProvider(providerType, provider);
            WriteLog(providerType.Name, provider);
        }

        #endregion

        public RepositoryBuilder(IServiceProvider provider = null)
        {
            Services = provider;
        }

        public InitialData InitialData { get; set; }
        public IServiceProvider Services { get; }

        public static void WriteLog(string name, object provider)
        {
            var providerName = provider is string pName ? pName : provider?.GetType().FullName ?? "null";
            var message = $"{name} configured: {providerName}";

            SnTrace.Repository.Write(message);
            SnLog.WriteInformation(message);
        }
    }
}
