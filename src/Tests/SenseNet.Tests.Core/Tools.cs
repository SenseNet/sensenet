using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Testing;

namespace SenseNet.Tests.Core
{
    public class Tools
    {
        public static IDisposable Swindle(Type @class, string memberName, object cheat)
        {
            return new Swindler(@class, memberName, cheat);
        }

        private class Swindler : IDisposable
        {
            private readonly TypeAccessor _accessor;
            private readonly string _memberName;
            private readonly object _originalValue;

            public Swindler(Type @class, string memberName, object cheat)
            {
                _accessor = new TypeAccessor(@class);
                _memberName = memberName;
                _originalValue = _accessor.GetStaticFieldOrProperty(memberName);
                _accessor.SetStaticFieldOrProperty(_memberName, cheat);
            }
            public void Dispose()
            {
                _accessor.SetStaticFieldOrProperty(_memberName, _originalValue);
            }
        }

        public class SearchEngineSwindler : IDisposable
        {
            private readonly ObjectAccessor _accessor;
            private string _memberName = "_searchEngine";
            private readonly object _originalSearchEngine;

            public SearchEngineSwindler(ISearchEngine searchEngine)
            {
                var storageContextAcc = new TypeAccessor(typeof(StorageContext));
                var storageContextInstance = storageContextAcc.GetStaticFieldOrProperty("Instance");
                _accessor = new ObjectAccessor(storageContextInstance);
                _originalSearchEngine = _accessor.GetField(_memberName);
                _accessor.SetField(_memberName, searchEngine);
            }
            public void Dispose()
            {
                _accessor.SetField(_memberName, _originalSearchEngine);
            }
        }

        public class LoggerSwindler<T> : IDisposable where T : class, IEventLogger
        {
            private readonly IEventLogger _originalLogger;
            public T Logger => SnLog.Instance as T;

            public LoggerSwindler()
            {
                _originalLogger = SnLog.Instance;
                SnLog.Instance = Activator.CreateInstance<T>();
            }
            public LoggerSwindler(IEventLogger logger)
            {
                _originalLogger = SnLog.Instance;
                SnLog.Instance = logger;
            }
            public void Dispose()
            {
                SnLog.Instance = _originalLogger;
            }
        }
    }
}
