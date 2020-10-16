using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Storage.Diagnostics;
using SenseNet.Tools;
using SenseNet.Tools.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class AuditEventWriterExtensions
    {
        public static IRepositoryBuilder UseAuditEventWriter(this IRepositoryBuilder builder, IAuditEventWriter provider)
        {
            Providers.Instance.AuditEventWriter = provider;
            return builder;
        }
        public static IRepositoryBuilder UseInactiveAuditEventWriter(this IRepositoryBuilder builder)
        {
            Providers.Instance.AuditEventWriter = new InactiveAuditEventWriter();
            return builder;
        }
    }
}
