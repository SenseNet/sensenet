using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.OData;

namespace SenseNet.ODataTests
{
    internal static class ODataTestExtensions
    {
        /// <summary>
        /// Adds all test policies that are required by test methods - even UnknownPolicy.
        /// </summary>
        public static RepositoryBuilder AddAllTestPolicies(this RepositoryBuilder builder)
        {
            return builder.UseOperationMethodExecutionPolicy("Policy1",
                    new ODataOperationMethodTests.AllowEverythingPolicy())
                .UseOperationMethodExecutionPolicy("Policy2",
                    new ODataOperationMethodTests.AllowEverythingPolicy())
                .UseOperationMethodExecutionPolicy("Policy3",
                    new ODataOperationMethodTests.AllowEverythingPolicy())
                .UseOperationMethodExecutionPolicy("UnknownPolicy",
                    new ODataOperationMethodTests.AllowEverythingPolicy())
                .UseOperationMethodExecutionPolicy("ContentNameMustBeRoot",
                    (user, context) => context.Content.Name == "Root"
                        ? OperationMethodVisibility.Enabled
                        : OperationMethodVisibility.Invisible)
                .UseOperationMethodExecutionPolicy("VisitorAllowed", (user, context) =>
                {
                    return user.Id == Identifiers.VisitorUserId
                            ? OperationMethodVisibility.Enabled
                            : OperationMethodVisibility.Disabled;
                })
                .UseOperationMethodExecutionPolicy("AdminDenied",
                    new ODataOperationMethodTests.DeniedUsersOperationMethodPolicy(new[] { Identifiers.AdministratorUserId }));
        }
    }
}
