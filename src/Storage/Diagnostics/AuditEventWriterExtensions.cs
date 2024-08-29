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
        /// Adds the InactiveAuditEventWriter as an IAuditEventWriter implementation type to the service collection.
        /// </summary>
        public static IServiceCollection AddInactiveAuditEventWriter(this IServiceCollection services)
        {
            return services.AddAuditEventWriter<InactiveAuditEventWriter>();
        }
        /// <summary>
        /// Adds the DatabaseAuditEventWriter as an IAuditEventWriter implementation type to the service collection.
        /// </summary>
        public static IServiceCollection AddDatabaseAuditEventWriter(this IServiceCollection services)
        {
            return services.AddAuditEventWriter<DatabaseAuditEventWriter>();
        }
        /// <summary>
        /// Adds an IAuditEventWriter implementation type to the service collection.
        /// </summary>
        public static IServiceCollection AddAuditEventWriter<T>(this IServiceCollection services) where T : class, IAuditEventWriter
        {
            return services.AddSingleton<IAuditEventWriter, T>();
        }
    }
}
