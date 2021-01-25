using SenseNet.Configuration;
using SenseNet.Events;
using SenseNet.Tools;
using SenseNet.Tools.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class EventsExtensions
    {
        /// <summary>
        /// Deletes the existing <see cref="IEventProcessor"/>s and registers the given elements.
        /// </summary>
        /// <param name="builder">The <see cref="IRepositoryBuilder"/> instance.</param>
        /// <param name="processors">One or more <see cref="IEventProcessor"/> instance.</param>
        /// <returns>The updated <see cref="IRepositoryBuilder"/>.</returns>
        public static IRepositoryBuilder UseAsyncEventProcessors(this IRepositoryBuilder builder, params IEventProcessor[] processors)
        {
            var list = Providers.Instance.AsyncEventProcessors;
            list.Clear();
            builder.AddAsyncEventProcessors(processors);
            return builder;
        }
        /// <summary>
        /// Adds the given <see cref="IEventProcessor"/>s to the existing list.
        /// </summary>
        /// <param name="builder">The <see cref="IRepositoryBuilder"/> instance.</param>
        /// <param name="processors">One or more <see cref="IEventProcessor"/> instance.</param>
        /// <returns>The updated <see cref="IRepositoryBuilder"/>.</returns>
        public static IRepositoryBuilder AddAsyncEventProcessors(this IRepositoryBuilder builder, params IEventProcessor[] processors)
        {
            Providers.Instance.AsyncEventProcessors.AddRange(processors);
            return builder;
        }

        /// <summary>
        /// Registers the AuditLogEventProcessor
        /// </summary>
        /// <param name="builder">The <see cref="IRepositoryBuilder"/> instance.</param>
        /// <param name="processor">The <see cref="IEventProcessor"/> instance.</param>
        /// <returns>The updated <see cref="IRepositoryBuilder"/>.</returns>
        public static IRepositoryBuilder UseAuditLogEventProcessor(this IRepositoryBuilder builder,
            IEventProcessor processor)
        {
            Providers.Instance.AuditLogEventProcessor = processor;
            return builder;
        }

        /// <summary>
        /// Registers the given <see cref="IEventDistributor"/> provider.
        /// </summary>
        /// <param name="builder">The <see cref="IRepositoryBuilder"/> instance.</param>
        /// <param name="provider">The <see cref="IEventDistributor"/> instance.</param>
        /// <returns>The updated <see cref="IRepositoryBuilder"/>.</returns>
        public static IRepositoryBuilder UseEventDistributor(this IRepositoryBuilder builder,
            IEventDistributor provider)
        {
            Providers.Instance.EventDistributor = provider;
            return builder;
        }
    }
}
