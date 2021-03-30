using System;
using System.Collections.Generic;
using System.Data;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.IntegrationTests.Common
{
    internal static class Extensions
    {
        public static int ToInt(this long input)
        {
            return Convert.ToInt32(input);
        }

        public static IRepositoryBuilder AddBlobProviders(this IRepositoryBuilder repositoryBuilder,
            IEnumerable<IBlobProvider> blobProviders)
        {
            if (blobProviders != null)
                foreach (var blobProvider in blobProviders)
                    Configuration.Providers.Instance.BlobProviders.AddProvider(blobProvider);
            return repositoryBuilder;
        }
    }
}
