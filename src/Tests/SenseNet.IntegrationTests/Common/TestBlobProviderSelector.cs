using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SenseNet.ContentRepository.Storage.Data;
using BlobStorage = SenseNet.Configuration.BlobStorage;

namespace SenseNet.IntegrationTests.Common
{
    public class TestBlobProviderSelector : IBlobProviderSelector
    {
        private readonly string _externalBlobProviderTypeName;
        private readonly bool _useBuiltInBlobProvider;

        public TestBlobProviderSelector(Type externalBlobProviderType, bool useBuiltInBlobProvider)
        {
            _externalBlobProviderTypeName = externalBlobProviderType.FullName;
            _useBuiltInBlobProvider = useBuiltInBlobProvider;
        }

        public IBlobProvider GetProvider(long fullSize, Dictionary<string, IBlobProvider> providers, IBlobProvider builtIn)
        {
            if (_useBuiltInBlobProvider && fullSize < BlobStorage.MinimumSizeForBlobProviderInBytes)
                return builtIn;
            return providers[_externalBlobProviderTypeName];
        }
    }
}
