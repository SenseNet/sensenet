using System;
using System.Collections.Generic;
using SenseNet.Configuration;
// ReSharper disable CheckNamespace
#pragma warning disable 1591

namespace SenseNet.ContentRepository
{
    [Obsolete("After V6.5 PATCH 9: Use the values in the SenseNet.Configuration namespace instead.")]
    public static class CommonConfiguration
    {
        public static string ConnectionString => Configuration.ConnectionStrings.ConnectionString;

        [Obsolete("After V6.5 PATCH 9: Use the Configuration.ConnectionStrings.AllConnectionStrings instead.")]
        public static IReadOnlyDictionary<string, string> ConnectionStrings => Configuration.ConnectionStrings.AllConnectionStrings;

        [Obsolete("After V6.5 PATCH 9: Use the Configuration.Common.IsWebEnvironment instead.")]
        public static bool IsWebEnvironment => Configuration.Common.IsWebEnvironment;

        [Obsolete("Use Configuration.Data.DbCommandTimeout instead.", true)]
        public static int SqlCommandTimeout => Data.DbCommandTimeout;
        public static double TransactionTimeout => Data.TransactionTimeout;
        public static double LongTransactionTimeout => Data.LongTransactionTimeout;
    }
}