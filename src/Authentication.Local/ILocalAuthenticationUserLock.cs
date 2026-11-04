namespace SenseNet.Authentication.Local;

/// <summary>Compatibility alias. Register IUserAuthenticationLock with the repository services instead.</summary>
public interface ILocalAuthenticationUserLock : SenseNet.ContentRepository.Storage.Security.IUserAuthenticationLock;
