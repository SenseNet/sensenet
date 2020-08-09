using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ServicesExtensions
    {

        /// <summary>
        /// Sets the membership extender used for extending user membership on-the-fly.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="membershipExtender">MembershipExtender instance.</param>
        public static IRepositoryBuilder UseMembershipExtender(this IRepositoryBuilder repositoryBuilder, MembershipExtenderBase membershipExtender)
        {
            Configuration.Providers.Instance.MembershipExtender = membershipExtender;
            RepositoryBuilder.WriteLog("MembershipExtender", membershipExtender);

            return repositoryBuilder;
        }
    }
}
