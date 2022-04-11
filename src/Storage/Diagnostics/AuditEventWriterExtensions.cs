using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Storage.Diagnostics;
using SenseNet.Tools;
using SenseNet.Tools.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class AuditEventWriterExtensions
    {
        public static IServiceCollection AddInactiveAuditEventWriter(this IServiceCollection services)
        {
            return AddAuditEventWriter<InactiveAuditEventWriter>(services);
        }
        public static IServiceCollection AddDatabaseAuditEventWriter(this IServiceCollection services)
        {
            return AddAuditEventWriter<DatabaseAuditEventWriter>(services);
        }
        public static IServiceCollection AddAuditEventWriter<T>(this IServiceCollection services) where T : class, IAuditEventWriter
        {
            return services.AddSingleton<IAuditEventWriter, T>();
        }
    }
}
