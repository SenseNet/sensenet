using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BackgroundOperations;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Components;
using SenseNet.ContentRepository.Diagnostics;
using SenseNet.ContentRepository.Email;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Packaging;
using SenseNet.ContentRepository.Packaging.Steps.Internal;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.Packaging;
using SenseNet.Preview;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.Search.Lucene29.Centralized.Common;
using SenseNet.Search.Lucene29.Centralized.GrpcClient;
using SenseNet.Search.Querying;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Security.Messaging;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Authentication.IdentityServer4;
using SenseNet.Services.Core.Cors;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Storage;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Storage.Diagnostics;
using SenseNet.Storage.Security;
using SenseNet.TaskManagement.Core;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using SenseNet.Tools.Diagnostics;
using SenseNet.Tools.SnInitialDataGenerator;
using SenseNet.WebHooks;
using BlobStorage = SenseNet.ContentRepository.Storage.Data.BlobStorage;

namespace WebAppTests
{
    [TestClass]
    public class ServicesTests : TestBase
    {
        #region Infrastructure
        private void StartupServicesTest<T>(
            IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, object> customizedExpectations,
            Type[] includedProvidersByType, string[] includedProvidersByName,
            IEnumerable<string> excludedProviderPropertyNames = null)
        {
            var configurationBuilder = new ConfigurationBuilder();
            //configuration.AddJsonFile("appSettings.json")
            var configuration = configurationBuilder.Build();

            var serviceCollection = new ServiceCollection();
            var cnOptions = Options.Create<ConnectionStringOptions>(new ConnectionStringOptions { Repository = "fake" });
            serviceCollection.AddSingleton<IOptions<ConnectionStringOptions>>(cnOptions);

            //var startup = new SnWebApplication.Api.InMem.Admin.Startup(configuration);
            var ctor = typeof(T).GetConstructor(new[] {typeof(IConfiguration) });
            //var startup = (T)ctor.Invoke();

            // ACTION
            var startupAcc = new ObjectAccessor(typeof(T), new[] {typeof(IConfiguration) }, new[] { configuration });
            startupAcc.Invoke("ConfigureServices", new[] {typeof(IServiceCollection) }, new[] { serviceCollection });

            // ASSERT
            AssertServices(serviceCollection,
                platformSpecificExpectations, customizedExpectations,
                includedProvidersByType, includedProvidersByName,
                excludedProviderPropertyNames);
        }

        private void AssertServices(IServiceCollection serviceCollection,
            IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, object> customizedExpectations,
            Type[] includedProvidersByType, string[] includedProvidersByName,
            IEnumerable<string> excludedProviderPropertyNames = null)
        {
            AssertServices(true, serviceCollection.BuildServiceProvider(),
                platformSpecificExpectations, customizedExpectations,
                includedProvidersByType, includedProvidersByName,
                excludedProviderPropertyNames);
        }
        private void AssertServices(bool useHosting, IServiceProvider services,
            IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, object> customizedExpectations,
            Type[] includedProvidersByType, string[] includedProvidersByName,
            IEnumerable<string> excludedProviderPropertyNames = null)
        {
            if (useHosting)
            {
                var repositoryHostedService = (RepositoryHostedService)services.GetService<IEnumerable<IHostedService>>()?
                    .FirstOrDefault(x => x.GetType() == typeof(RepositoryHostedService));
                repositoryHostedService?.BuildProviders();
            }

            var expectation = GetGeneralizedExpectations();
            if(platformSpecificExpectations != null)
                foreach (var item in platformSpecificExpectations)
                    expectation[item.Key] = item.Value;
            if(customizedExpectations != null)
                foreach (var item in customizedExpectations)
                    expectation[item.Key] = item.Value;

            var allServiceDescriptors = GetAllServiceDescriptors(services);
            var snServiceDescriptors = allServiceDescriptors
                .Where(x =>
                    (x.Key?.FullName?.Contains("SenseNet.") ?? false) ||
                    (
                        (x.Value?.ServiceType?.FullName ?? "null") +
                        (x.Value?.ImplementationType?.FullName ?? "null") +
                        (x.Value?.ImplementationInstance?.GetType().FullName ?? "null")
                    ).Contains("SenseNet.")
                ).ToDictionary(x => x.Key, x => x.Value);
            var unexpectedSnServices = snServiceDescriptors.Keys.Except(expectation.Keys)
                .Where(x=>x.Namespace != "Microsoft.Extensions.Options")
                .ToDictionary(x => x, x => snServiceDescriptors[x]);


            var dump = expectation.ToDictionary(
                x => x.Key,
                x => GetServiceOrServices(services, expectation, x.Key) /*services.GetService(x.Key)?.GetType()*/);

            Assert.AreEqual(0, dump
                .Where(x => x.Value != null)
                .Count(x => !TypesAreEquals(x.Value, expectation[x.Key])), GetMessageForDifferences(dump, expectation));
            Assert.AreEqual(0, dump.Values.Count(x => x == null), GetMessageForNulls(dump));

            AssertProvidersInstance(includedProvidersByType, includedProvidersByName, excludedProviderPropertyNames ?? Array.Empty<string>());

            if(_assertUnexpectedSnServices)
                Assert.AreEqual(_postponedInUnexpectedCheck.Length, unexpectedSnServices.Count, ServiceEntriesToString(unexpectedSnServices));
        }

        private object GetServiceOrServices(IServiceProvider services, IDictionary<Type, object> expectation, Type key)
        {
            if (!expectation.TryGetValue(key, out var exp))
                return null;
            if (exp is Type[])
                return services.GetServices(key)?
                    .Select(x => x.GetType())
                    .Where(t=>t.Namespace?.Contains("SenseNet.") ?? false)
                    .ToArray();
            return services.GetService(key)?.GetType();
        }
        #region GetAllServiceDescriptors
        public static Dictionary<Type, ServiceDescriptor> GetAllServiceDescriptors(IServiceProvider provider)
        {
            if (provider is ServiceProvider serviceProvider)
            {
                var result = new Dictionary<Type, ServiceDescriptor>();

                var engine = GetFieldValue(serviceProvider, "_engine");
                var callSiteFactory = GetPropertyValue(engine, "CallSiteFactory");
                var descriptorLookup = GetFieldValue(callSiteFactory, "_descriptorLookup");
                if (descriptorLookup is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        result.Add((Type)entry.Key, (ServiceDescriptor)GetPropertyValue(entry.Value, "Last"));
                    }
                }

                return result;
            }

            throw new NotSupportedException($"Type '{provider.GetType()}' is not supported!");
        }
        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type objType = obj.GetType();
            var fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
                throw new ArgumentOutOfRangeException(fieldName,
                    $"Couldn't find field {fieldName} in type {objType.FullName}");
            return fieldInfo.GetValue(obj);
        }
        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            FieldInfo fieldInfo = null;
            do
            {
                fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            } while (fieldInfo == null && type != null);

            return fieldInfo;
        }
        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type objType = obj.GetType();
            var propertyInfo = GetPropertyInfo(objType, propertyName);
            if (propertyInfo == null)
                throw new ArgumentOutOfRangeException(propertyName,
                    $"Couldn't find property {propertyName} in type {objType.FullName}");
            return propertyInfo.GetValue(obj, null);
        }
        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo propertyInfo = null;
            do
            {
                propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            } while (propertyInfo == null && type != null);

            return propertyInfo;
        }
        #endregion

        private string ServiceEntriesToString(Dictionary<Type, ServiceDescriptor> unexpectedSnServices)
        {
            //var lines = unexpectedSnServices.Select(x => x.ToString()).ToArray();
            var lines = unexpectedSnServices.Select(x => x.Key.Name)
//UNDONE:xxxxxxxxxxxxxxxxx Delete _postponedInUnexpectedCheck
.Except(_postponedInUnexpectedCheck)
                .ToArray();
            return string.Join(" ", lines);
        }

        private bool TypesAreEquals(object a, object b)
            {
                if (a is Type && b is Type)
                    return a == b;
                if (!(a is Type[] aa && b is Type[] bb))
                    return false;
                if (aa.Length != bb.Length)
                    return false;
                foreach (var t in aa)
                    if (!bb.Contains(t))
                        return false;

                return true;
            }
        private string GetMessageForDifferences(Dictionary<Type, object> dump, IDictionary<Type, object> expectation)
        {
            string GetName(object a)
            {
                if (a == null)
                    return "null";
                if (a is Type t)
                    return t.Name;
                return $"[{string.Join(", ", ((Type[]) a).Select(x => x.Name))}]";
            }
            
            var diffs = dump.Where(x => !TypesAreEquals(x.Value, expectation[x.Key]))
                .Where(x => x.Value != null)
                .ToDictionary(x => x.Key, x => (Actual: x.Value, Expected: expectation[x.Key]))
                .Select(x => $"{x.Key.Name}: expected: {GetName(x.Value.Expected)}, actual: {GetName(x.Value.Actual)}")
                .ToArray();
            return $"Different services: {string.Join(", ", diffs)}";
        }
        private string GetMessageForNulls(IDictionary<Type, object> dump)
        {
            var nulls = dump.Where(x => x.Value == null).Select(x => x.Key.Name);
            return $"Missing services: {string.Join(", ", nulls)}";
        }
        #endregion

        private bool _assertUnexpectedSnServices = true;
        //UNDONE:xxxxxxxxxxxxxxxxx Delete _postponedInUnexpectedCheck
        private readonly string[] _postponedInUnexpectedCheck = new[]
        {
            "ISnClientRequestParametersProvider",
            "ClientStore", 
            //"IStatisticalDataAggregator",
            //"IMaintenanceTask",
            //"IHostedService",
            //"ISnComponent"
        };

        private IDictionary<Type, object> GetGeneralizedExpectations()
        {
            return new Dictionary<Type, object>
            {
                {typeof(IEventLogger), typeof(SnILogger)},
                {typeof(ISnTracer), typeof(SnILoggerTracer)},

                {typeof(IBlobProviderStore), typeof(BlobProviderStore)},
                {typeof(IBlobStorage), typeof(BlobStorage)},
                {typeof(IExternalBlobProviderFactory), typeof(NullExternalBlobProviderFactory)},

                {typeof(SecurityHandler), typeof(SecurityHandler)},
                {typeof(IMissingEntityHandler), typeof(SnMissingEntityHandler)},

                {typeof(IPasswordHashProvider), typeof(SenseNetPasswordHashProvider)},
                {typeof(IPasswordHashProviderForMigration), typeof(Sha256PasswordHashProviderWithoutSalt)},

                // Search
                {typeof(ISearchManager), typeof(SearchManager)},
                {typeof(IIndexManager), typeof(IndexManager)},
                {typeof(IIndexPopulator), typeof(DocumentPopulator)},

                // TaskManager
                {typeof(ITaskManager), typeof(TaskManagerBase)},
                {typeof(ITaskManagementClient), typeof(TaskManagementClient)},

                // Preview
                {typeof(IPreviewProvider), typeof(DefaultDocumentPreviewProvider)},

                // Components
                {typeof(ILatestComponentStore), typeof(DefaultLatestComponentStore)},

                // Not used?
                {typeof(IApplicationCache), typeof(ApplicationCache)},

                // Not categorized
                {typeof(ISnCache), typeof(SnMemoryCache)},
                {typeof(IIndexDocumentProvider), typeof(IndexDocumentProvider)},
                {typeof(IEventPropertyCollector), typeof(EventPropertyCollector)},
                {typeof(IContentNamingProvider), typeof(CharReplacementContentNamingProvider)},
                {typeof(ICorsPolicyProvider), typeof(SnCorsPolicyProvider)},

                // error: {typeof(ISnClientRequestParametersProvider), typeof(DefaultSnClientRequestParametersProvider)},
                // error: {typeof(ClientStore), typeof(ClientStore)},
                {typeof(IClientManager), typeof(DefaultClientManager)},
                {typeof(IApiKeyManager), typeof(ApiKeyManager)},
                {typeof(IEmailTemplateManager), typeof(RepositoryEmailTemplateManager)},
                {typeof(IEmailSender), typeof(EmailSender)},
                {typeof(DefaultRegistrationProvider), typeof(DefaultRegistrationProvider)},
                {typeof(RegistrationProviderStore), typeof(RegistrationProviderStore)},
                {typeof(IStatisticalDataCollector), typeof(StatisticalDataCollector)},
                {typeof(WebTransferRegistrator), typeof(WebTransferRegistrator)},
                {typeof(IStatisticalDataAggregator), new[] {
                    typeof(WebTransferStatisticalDataAggregator),
                    typeof(DatabaseUsageStatisticalDataAggregator),
                    typeof(WebHookStatisticalDataAggregator),
                }},
                {typeof(IStatisticalDataAggregationController), typeof(StatisticalDataAggregationController)},
                {typeof(IDatabaseUsageHandler), typeof(DatabaseUsageHandler)},
                {typeof(IDataStore), typeof(DataStore)},
                {typeof(IMessageSenderManager), typeof(MessageSenderManager)},
                {typeof(IMaintenanceTask), new[] {
                    //typeof(ReindexBinariesTask),
                    typeof(CleanupFilesTask),
                    typeof(StartActiveDirectorySynchronizationTask),
                    typeof(AccessTokenCleanupTask),
                    typeof(SharedLockCleanupTask),
                    typeof(StatisticalDataAggregationMaintenanceTask),
                    typeof(StatisticalDataCollectorMaintenanceTask),
                }},
                {typeof(IHostedService), new[] {
                    typeof(RepositoryHostedService),
                    typeof(SnMaintenance),
                }},
            };
        }
        private IDictionary<Type, Type> GetInMemoryPlatform()
        {
            return new Dictionary<Type, Type>
            {
                // DataProvider
                {typeof(DataProvider), typeof(InMemoryDataProvider)},

                // Blob
                {typeof(IBlobStorageMetaDataProvider), typeof(InMemoryBlobStorageMetaDataProvider)},
                {typeof(IBlobProvider), typeof(InMemoryBlobProvider)},
                {typeof(IBlobProviderSelector), typeof(InMemoryBlobProviderSelector)},

                // Security
                {typeof(ISecurityDataProvider), typeof(MemoryDataProvider)},
                {typeof(ElevatedModificationVisibilityRule), typeof(ElevatedModificationVisibilityRule)},

                // ????
                {typeof(ISharedLockDataProvider), typeof(InMemorySharedLockDataProvider)},
                {typeof(IExclusiveLockDataProvider), typeof(InMemoryExclusiveLockDataProvider)},
                {typeof(IAccessTokenDataProvider), typeof(InMemoryAccessTokenDataProvider)},
                {typeof(IPackagingDataProvider), typeof(InMemoryPackageStorageProvider)},
                {typeof(IAuditEventWriter), typeof(InactiveAuditEventWriter)},

                {typeof(IClientStoreDataProvider), typeof(InMemoryClientStoreDataProvider)},
                {typeof(IStatisticalDataProvider), typeof(InMemoryStatisticalDataProvider)},

                //UNDONE: Add to services
                //{typeof(ISearchEngine), typeof(InMemorySearchEngine)},
                //InMemoryIndex?
                //{typeof(IIndexingEngine), typeof(InMemoryIndexingEngine)},
                //{typeof(IQueryEngine), typeof(InMemoryQueryEngine)},
            };
        }
        private IDictionary<Type, Type> GetMsSqlPlatform()
        {
            return new Dictionary<Type, Type>
            {
                // DataProvider
                {typeof(DataProvider), typeof(MsSqlDataProvider)},
                {typeof(IDataInstaller), typeof(MsSqlDataInstaller)},
                {typeof(MsSqlDatabaseInstaller), typeof(MsSqlDatabaseInstaller)},

                // Blob
                {typeof(IBlobStorageMetaDataProvider), typeof(MsSqlBlobMetaDataProvider)},
                {typeof(IBlobProvider), typeof(BuiltInBlobProvider)},
                {typeof(IBlobProviderSelector), typeof(BuiltInBlobProviderSelector)},

                {typeof(ISecurityDataProvider), typeof(EFCSecurityDataProvider)},

                // ????
                {typeof(ISharedLockDataProvider), typeof(MsSqlSharedLockDataProvider)},
                {typeof(IExclusiveLockDataProvider), typeof(MsSqlExclusiveLockDataProvider)},
                {typeof(IAccessTokenDataProvider), typeof(MsSqlAccessTokenDataProvider)},
                {typeof(IPackagingDataProvider), typeof(MsSqlPackagingDataProvider)},
                {typeof(IAuditEventWriter), typeof(DatabaseAuditEventWriter)},

                {typeof(IClientStoreDataProvider), typeof(MsSqlClientStoreDataProvider)},
                {typeof(IStatisticalDataProvider), typeof(MsSqlStatisticalDataProvider)},

                //UNDONE: Add to services
                //{typeof(ISearchEngine), typeof(Lucene29SearchEngine)},
                //IndexDirectory?
                //{typeof(IIndexingEngine), typeof(Lucene29LocalIndexingEngine)},
                //{typeof(IQueryEngine), typeof(Lucene29LocalQueryEngine)},
            };
        }
        private void AssertProvidersInstance(Type[] includedProvidersByType, string[] includedProvidersByName,
            IEnumerable<string> excludedPropertyNames)
        {
            var pi = Providers.Instance;
            Assert.IsNotNull(pi);
            Assert.IsNotNull(pi.EventLogger);
            Assert.IsNotNull(pi.PropertyCollector);
            Assert.IsNotNull(pi.AuditEventWriter);
            Assert.IsNotNull(pi.DataProvider);
            Assert.IsNotNull(pi.DataStore);
            Assert.IsNotNull(pi.BlobMetaDataProvider);
            Assert.IsNotNull(pi.BlobProviderSelector);
//Assert.IsNotNull(pi.BlobStorage);
            Assert.IsNotNull(pi.BlobProviders);
            Assert.IsNotNull(pi.SearchEngine);
            Assert.IsNotNull(pi.SearchManager);
            Assert.IsNotNull(pi.IndexManager);
            Assert.IsNotNull(pi.IndexPopulator);
            Assert.IsNotNull(pi.AccessProvider);
            Assert.IsNotNull(pi.SecurityDataProvider);
            Assert.IsNotNull(pi.SecurityMessageProvider);
            Assert.IsNotNull(pi.SecurityHandler);
            Assert.IsNotNull(pi.PasswordHashProvider);
            Assert.IsNotNull(pi.PasswordHashProviderForMigration);
            Assert.IsNotNull(pi.ContentNamingProvider);
            Assert.IsNotNull(pi.PreviewProvider);
            if(!excludedPropertyNames.Contains(nameof(pi.ElevatedModificationVisibilityRuleProvider)))
                Assert.IsNotNull(pi.ElevatedModificationVisibilityRuleProvider);
            Assert.IsNotNull(pi.MembershipExtender);
            Assert.IsNotNull(pi.CacheProvider);
            Assert.IsNotNull(pi.ApplicationCacheProvider);
            Assert.IsNotNull(pi.ClusterChannelProvider);
            Assert.IsNotNull(pi.PermissionFilterFactory);
            Assert.IsNotNull(pi.IndexDocumentProvider);
            Assert.IsNotNull(pi.ContentProtector);
            Assert.IsNotNull(pi.StorageSchema);
            Assert.IsNotNull(pi.CompatibilitySupport);
            Assert.IsNotNull(pi.EventDistributor);
            //Assert.IsNotNull(pi.AuditLogEventProcessor);
//Assert.IsNotNull(pi.TreeLock);
            Assert.IsNotNull(pi.TaskManager);

            Assert.IsTrue(pi.NodeObservers.Length > 0);
            //Assert.IsTrue(pi.AsyncEventProcessors.Count > 0);
            Assert.IsTrue(pi.Components.Count > 0);

            var providersMessage = GetProvidersMessage(includedProvidersByType, includedProvidersByName);
            if(providersMessage != null)
                Assert.Fail(providersMessage);
        }
        private string GetProvidersMessage(Type[] includedProvidersByType, string[] includedProvidersByName)
        {
            var pi = Providers.Instance;
            string providersMessage = null;

            var unexpectedNames = pi.ProvidersByName.Keys.Except(includedProvidersByName).ToArray();
            var missingNames = includedProvidersByName.Except(pi.ProvidersByName.Keys).ToArray();
            if (unexpectedNames.Length > 0)
                providersMessage = $"Unexpected providers by name: {string.Join(", ", unexpectedNames)}. ";
            if (missingNames.Length > 0)
                providersMessage += $"Missing providers by name: {string.Join(", ", missingNames)}. ";

            var unexpectedTypes = pi.ProvidersByType.Keys.Except(includedProvidersByType).Select(t => t.Name).ToArray();
            var missingTypes = includedProvidersByType.Except(pi.ProvidersByType.Keys).Select(t => t.Name).ToArray();
            if (unexpectedTypes.Length > 0)
                providersMessage += $"Unexpected providers by type: {string.Join(", ", unexpectedTypes)}. ";
            if (missingTypes.Length > 0)
                providersMessage += $"Missing providers by type: {string.Join(", ", missingTypes)}. ";

            return providersMessage;
        }

        private readonly Type[] _defaultIncludedProvidersByType = new[]
        {
            typeof(ISharedLockDataProvider),
            typeof(IExclusiveLockDataProvider),
            typeof(IAccessTokenDataProvider),
            typeof(IPackagingDataProvider),
            typeof(IStatisticalDataProvider),
            typeof(ISnTracer[]),
            typeof(ILogger<SnILogger>),
        };
        private readonly string[] _defaultIncludedProvidersByName = Array.Empty<string>();

        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_InMem_Admin()
        {
            StartupServicesTest<SnWebApplication.Api.InMem.Admin.Startup>(
                platformSpecificExpectations: GetInMemoryPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(ISearchEngine), typeof(InMemorySearchEngine)},

                    {typeof(IWebHookClient), typeof(HttpWebHookClient)},
                    {typeof(TemplateReplacerBase), typeof(WebHooksTemplateReplacer)},
                    {typeof(IEventProcessor), typeof(LocalWebHookProcessor)},
                    {typeof(IWebHookSubscriptionStore), typeof(BuiltInWebHookSubscriptionStore)},
                    {typeof(IWebHookEventProcessor), typeof(LocalWebHookProcessor)},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(WebHookComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName
            );
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_InMem_TokenAuth()
        {
            StartupServicesTest<SnWebApplication.Api.InMem.TokenAuth.Startup>(
                platformSpecificExpectations: GetInMemoryPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(ISearchEngine), typeof(InMemorySearchEngine)},

                    {typeof(IWebHookClient), typeof(HttpWebHookClient)},
                    {typeof(TemplateReplacerBase), typeof(WebHooksTemplateReplacer)},
                    {typeof(IEventProcessor), typeof(LocalWebHookProcessor)},
                    {typeof(IWebHookSubscriptionStore), typeof(BuiltInWebHookSubscriptionStore)},
                    {typeof(IWebHookEventProcessor), typeof(LocalWebHookProcessor)},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(WebHookComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName
            );
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_Admin()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.Admin.Startup>(
                platformSpecificExpectations: GetMsSqlPlatform(),
                customizedExpectations: new Lucene.Net.Support.Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(IInstallPackageDescriptor), typeof(InstallPackageDescriptor)},

                    {typeof(IWebHookClient), typeof(HttpWebHookClient)},
                    {typeof(TemplateReplacerBase), typeof(WebHooksTemplateReplacer)},
                    {typeof(IEventProcessor), typeof(LocalWebHookProcessor)},
                    {typeof(IWebHookSubscriptionStore), typeof(BuiltInWebHookSubscriptionStore)},
                    {typeof(IWebHookEventProcessor), typeof(LocalWebHookProcessor)},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(MsSqlExclusiveLockComponent),
                        typeof(MsSqlStatisticsComponent),
                        typeof(MsSqlClientStoreComponent),
                        typeof(WebHookComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName,
                excludedProviderPropertyNames: new[] {"ElevatedModificationVisibilityRuleProvider"}
            );
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_TokenAuth()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.TokenAuth.Startup>(
                platformSpecificExpectations: GetMsSqlPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(IInstallPackageDescriptor), typeof(InstallPackageDescriptor)},

                    {typeof(IWebHookClient), typeof(HttpWebHookClient)},
                    {typeof(TemplateReplacerBase), typeof(WebHooksTemplateReplacer)},
                    {typeof(IEventProcessor), typeof(LocalWebHookProcessor)},
                    {typeof(IWebHookSubscriptionStore), typeof(BuiltInWebHookSubscriptionStore)},
                    {typeof(IWebHookEventProcessor), typeof(LocalWebHookProcessor)},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(MsSqlExclusiveLockComponent),
                        typeof(MsSqlStatisticsComponent),
                        typeof(MsSqlClientStoreComponent),
                        typeof(WebHookComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName,
                excludedProviderPropertyNames: new[] {"ElevatedModificationVisibilityRuleProvider"}
            );
        }

        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_SearchService_Admin()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.SearchService.Admin.Startup>(
                platformSpecificExpectations: GetMsSqlPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(SenseNet.Security.Messaging.RabbitMQ.RabbitMQMessageProvider)},
                    {typeof(IInstallPackageDescriptor), typeof(InstallPackageDescriptor)},
                    {typeof(IIndexingEngine), typeof(Lucene29CentralizedIndexingEngine)},
                    {typeof(IQueryEngine), typeof(Lucene29CentralizedQueryEngine)},
                    {typeof(ISearchEngine), typeof(Lucene29SearchEngine)},
                    {typeof(ISearchServiceClient), typeof(GrpcServiceClient)},
                    {typeof(IClusterMessageFormatter), typeof(BinaryMessageFormatter)},
                    {typeof(IClusterChannel), typeof(SenseNet.Messaging.RabbitMQ.RabbitMQMessageProvider)},

                    {typeof(IWebHookClient), typeof(HttpWebHookClient)},
                    {typeof(TemplateReplacerBase), typeof(WebHooksTemplateReplacer)},
                    {typeof(IEventProcessor), typeof(LocalWebHookProcessor)},
                    {typeof(IWebHookSubscriptionStore), typeof(BuiltInWebHookSubscriptionStore)},
                    {typeof(IWebHookEventProcessor), typeof(LocalWebHookProcessor)},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(MsSqlExclusiveLockComponent),
                        typeof(MsSqlStatisticsComponent),
                        typeof(MsSqlClientStoreComponent),
                        typeof(WebHookComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName,
                excludedProviderPropertyNames: new[] {"ElevatedModificationVisibilityRuleProvider"}
            );
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_SearchService_TokenAuth()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.SearchService.TokenAuth.Startup>(
                platformSpecificExpectations: GetMsSqlPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(SenseNet.Security.Messaging.RabbitMQ.RabbitMQMessageProvider)},
                    {typeof(IInstallPackageDescriptor), typeof(InstallPackageDescriptor)},
                    {typeof(IIndexingEngine), typeof(Lucene29CentralizedIndexingEngine)},
                    {typeof(IQueryEngine), typeof(Lucene29CentralizedQueryEngine)},
                    {typeof(ISearchEngine), typeof(Lucene29SearchEngine)},
                    {typeof(ISearchServiceClient), typeof(GrpcServiceClient)},
                    {typeof(IClusterMessageFormatter), typeof(BinaryMessageFormatter)},
                    {typeof(IClusterChannel), typeof(SenseNet.Messaging.RabbitMQ.RabbitMQMessageProvider)},

                    {typeof(IWebHookClient), typeof(HttpWebHookClient)},
                    {typeof(TemplateReplacerBase), typeof(WebHooksTemplateReplacer)},
                    {typeof(IEventProcessor), typeof(LocalWebHookProcessor)},
                    {typeof(IWebHookSubscriptionStore), typeof(BuiltInWebHookSubscriptionStore)},
                    {typeof(IWebHookEventProcessor), typeof(LocalWebHookProcessor)},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(MsSqlExclusiveLockComponent),
                        typeof(MsSqlStatisticsComponent),
                        typeof(MsSqlClientStoreComponent),
                        typeof(WebHookComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName,
                excludedProviderPropertyNames: new[] {"ElevatedModificationVisibilityRuleProvider"}
            );
        }

        /* ========================================================================= Test tests */

        private class TestClassForTestingServices : TestBase
        { public IServiceProvider CreateServiceProvider() => CreateServiceProviderForTest(); }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_TestBase()
        {
            // ACTION
            var services = new TestClassForTestingServices().CreateServiceProvider();

            // ASSERT
            AssertServices(true, services,
                platformSpecificExpectations: GetInMemoryPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(ITestingDataProvider), typeof(InMemoryTestingDataProvider)},
                    {typeof(ISearchEngine), typeof(InMemorySearchEngine)},
                    {typeof(IStatisticalDataAggregator), new[] { // overrides the general services
                        typeof(WebTransferStatisticalDataAggregator),
                        typeof(DatabaseUsageStatisticalDataAggregator)}},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName
            );
        }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_Integration_InMem()
        {
            var platform = new InMemPlatform();
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            // ACTION
            platform.BuildServices(configuration, services);

            // ASSERT
            AssertServices(services,
                platformSpecificExpectations: GetInMemoryPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(ITestingDataProvider), typeof(InMemoryTestingDataProvider)},
                    {typeof(ISearchEngine), typeof(InMemorySearchEngine)},
                    {typeof(IStatisticalDataAggregator), new[] { // overrides the general services
                        typeof(WebTransferStatisticalDataAggregator),
                        typeof(DatabaseUsageStatisticalDataAggregator)}},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName
            );
        }
        [TestMethod, TestCategory("Services")]
        public void Test_Services_Integration_MsSql()
        {
            var platform = new MsSqlPlatform();
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            var cnOptions = Options.Create<ConnectionStringOptions>(new ConnectionStringOptions {Repository = "fake"});
            services.AddSingleton<IOptions<ConnectionStringOptions>>(cnOptions);

            // ACTION
            platform.BuildServices(configuration, services);

            // ASSERT
            AssertServices(services,
                platformSpecificExpectations: GetMsSqlPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(ITestingDataProvider), typeof(MsSqlTestingDataProvider)},
                    {typeof(IStatisticalDataAggregator), new[] { // overrides the general services
                        typeof(WebTransferStatisticalDataAggregator),
                        typeof(DatabaseUsageStatisticalDataAggregator)}},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                        typeof(MsSqlExclusiveLockComponent),
                        typeof(MsSqlStatisticsComponent),
                        typeof(MsSqlClientStoreComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName,
                excludedProviderPropertyNames: new[] {"ElevatedModificationVisibilityRuleProvider"}
            );

        }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_InitialDataGenerator()
        {
            // ACTION
            var builder = SenseNet.Tools.SnInitialDataGenerator.Program.CreateRepositoryBuilder(null);
            var services = builder.Services;

            // ASSERT
            AssertServices(true, services,
                platformSpecificExpectations: GetInMemoryPlatform(),
                customizedExpectations: new Dictionary<Type, object>
                {
                    {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                    {typeof(ISearchEngine), typeof(SearchEngineForInitialDataGenerator)}, // overrides the platform's service
                    {typeof(IStatisticalDataAggregator), new[] { // overrides the general services
                        typeof(WebTransferStatisticalDataAggregator),
                        typeof(DatabaseUsageStatisticalDataAggregator)}},

                    {typeof(ISnComponent), new[] {
                        typeof(ServicesComponent),
                    }},
                },
                includedProvidersByType: _defaultIncludedProvidersByType,
                includedProvidersByName: _defaultIncludedProvidersByName
            );
        }
    }
}
