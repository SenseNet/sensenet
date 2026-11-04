using Microsoft.Extensions.Options;
using SenseNet.Authentication.Local;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SnWebApplication.Api.Sql.LocalAuth;

/// <summary>Compatibility wrapper; AddSenseNetMsSqlProviders registers the repository-wide lock.</summary>
public sealed class SqlLocalAuthenticationUserLock(IOptions<ConnectionStringOptions> connectionStrings)
    : MsSqlUserAuthenticationLock(connectionStrings), ILocalAuthenticationUserLock;
