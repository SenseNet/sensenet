using Microsoft.Extensions.DependencyInjection;
using SenseNet.Diagnostics;
using SenseNet.Storage.Diagnostics;
using SenseNet.Tools.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class AuditEventWriterExtensions
    {
        /// <summary>
        /// Adds the <c>InactiveAuditEventWriter</c> as an <c>IAuditEventWriter</c> implementation type to the service collection.
        /// Used by InMemory data platform.
        /// </summary>
        public static IServiceCollection AddInactiveAuditEventWriter(this IServiceCollection services)
        {
            return services.AddAuditEventWriter<InactiveAuditEventWriter>();
        }
        /// <summary>
        /// Adds the <c>DatabaseAuditEventWriter</c> as an <c>IAuditEventWriter</c> implementation type to the service collection.
        /// Used by MsSql data platform.
        /// </summary>
        public static IServiceCollection AddDatabaseAuditEventWriter(this IServiceCollection services)
        {
            return services.AddAuditEventWriter<DatabaseAuditEventWriter>();
        }
        /// <summary>
        /// Adds an <c>IAuditEventWriter</c> implementation type to the service collection.
        /// Use this method when the default implementation needs to be replaced.
        /// Defaults: <c>InactiveAuditEventWriter</c> in InMemory data platform, <c>DatabaseAuditEventWriter</c> in MsSql data platform.
        /// </summary>
        public static IServiceCollection AddAuditEventWriter<T>(this IServiceCollection services) where T : class, IAuditEventWriter
        {
            return services.AddSingleton<IAuditEventWriter, T>();
        }
    }
}
