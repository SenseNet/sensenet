using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using BlobStorage = SenseNet.Configuration.BlobStorage;

namespace SenseNet.IntegrationTests.Common
{
    public class TestBlobProviderSelector : IBlobProviderSelector
    {
        private readonly string _externalBlobProviderTypeName;
        private readonly bool _useBuiltInBlobProvider;

        private IBlobProviderStore BlobProviderStore => Providers.Instance.BlobProviders;


        public TestBlobProviderSelector(Type externalBlobProviderType, bool useBuiltInBlobProvider)
        {
            _externalBlobProviderTypeName = externalBlobProviderType.FullName;
            _useBuiltInBlobProvider = useBuiltInBlobProvider;
        }

        public IBlobProvider GetProvider(long fullSize)
        {
            if (_useBuiltInBlobProvider && fullSize < Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes)
                return BlobProviderStore.BuiltInBlobProvider;
            return BlobProviderStore[_externalBlobProviderTypeName];
        }
    }
}
