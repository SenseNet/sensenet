using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Search;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository;

internal class SettingsSafeQueries : ISafeQueryHolder
{
    /// <summary>Returns the following query: "+TypeIs:Group +InTree:'@0' +Name:@1"</summary>
    public static string LocalGroupByName => "+TypeIs:'Group' +InTree:@0 +Name:@1";
}

internal static class ExtensionsForSettings
{
    public static async Task<IGroup> GetLocalGroup(this Workspace workspace, string name, CancellationToken cancel)
    {
        var result = await ContentQuery.QueryAsync(SettingsSafeQueries.LocalGroupByName, QuerySettings.AdminSettings, cancel,
            RepositoryPath.Combine(workspace.Path, Workspace.LocalGroupsFolderName), name).ConfigureAwait(false);
        var group = await Node.LoadAsync<Group>(result.Identifiers.FirstOrDefault(), cancel).ConfigureAwait(false);
        return group;
    }
}

public partial class Settings
{

    // ================================================================================= Settings ODATA API

    /// <summary>
    /// Defines Settings related role names.
    /// </summary>
    private enum SettingsRole
    {
        /// <summary> The current user has Open permission.</summary>
        Reader,
        /// <summary> The current user has Open and Save permissions.</summary>
        Editor,
    }

    [ODataFunction]
    [ContentTypes(N.CT.GenericContent)]
    [AllowedRoles(N.R.Everyone, N.R.Visitor)]
    public static async Task<object> GetSettings(Content content, HttpContext httpContext, string name, string property = null)
    {
        var effectiveSettings = await GetEffectiveValues(name, content.Path, httpContext.RequestAborted)
            .ConfigureAwait(false);
        if (effectiveSettings == null)
            return "{}";
        if (property == null)
            return effectiveSettings;

        //UNDONE:ySettings: use GetEffectiveValues and return the whole object or a top level property
        return GetValue<object>(name, property, content.Path);
    }

    [ODataAction]
    [ContentTypes(N.CT.GenericContent)]
    [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers, N.R.Editors, N.R.IdentifiedUsers)]
    public static async STT.Task WriteSettings(Content content, HttpContext httpContext, string name, object settingsData)
    {
        if (content.Name.Equals(SETTINGSCONTAINERNAME, StringComparison.InvariantCultureIgnoreCase))
            throw GetCannotCreateUnderSettingsException(content.Path);
        if (0 < content.Path.IndexOf("/settings/", StringComparison.InvariantCultureIgnoreCase))
            throw GetCannotCreateUnderSettingsException(content.Path);

        Workspaces.Workspace workspace;
        Settings globalSettings;
        using (new SystemAccount())
        {
            workspace = GetWorkspace(content);
            if (workspace == null)
                throw new InvalidOperationException("Local settings cannot be written outside a workspace.");

            globalSettings = GetSettingsByName<Settings>(name, Identifiers.RootPath);
            if (globalSettings == null)
                throw new InvalidOperationException($"Cannot write local settings {name} if it is not created " +
                                                    $"in the the global settings folder ({Repository.SettingsFolderPath})");
        }

        if (!await IsCurrentUserInRoleAsync(SettingsRole.Editor, name, content.ContentHandler, workspace, globalSettings, httpContext.RequestAborted))
            throw new InvalidContentActionException($"Not enough permission for write local settings {name} " +
                                                    $"for the requested path: {content.Path}");

        using (new SystemAccount())
        {
            var settings = await EnsureSettingsContentAsync(content, name, httpContext.RequestAborted)
                .ConfigureAwait(false);
            var settingsText = JsonConvert.SerializeObject(settingsData);
            settings.Binary.SetStream(RepositoryTools.GetStreamFromString(settingsText));
            await settings.SaveAsync(httpContext.RequestAborted).ConfigureAwait(false);
        }
    }

    private static Workspace GetWorkspace(Content content)
    {
        if (content.ContentHandler is not GenericContent gc)
            return null;
        return gc.Workspace as Workspace;
    }

    private static async Task<bool> IsCurrentUserInRoleAsync(SettingsRole role, string settingsName,
        Node contentHandler, Workspace workspace, Settings globalSettings, CancellationToken cancel)
    {
        var user = User.Current;
        if (user.Id == Identifiers.SystemUserId)
            return true;

        using var _ = new SystemAccount();

        if (user.IsInGroup(Group.Administrators))
            return true;

        if (!(contentHandler is GenericContent gc))
            return false;

        IGroup group = null;
        workspace ??= gc.Workspace as Workspace;
        if (workspace != null)
        {
            string roleName = role switch
            {
                SettingsRole.Reader => "Readers",
                SettingsRole.Editor => "Editors",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
            };

            group = await GetLocalGroupAsync(workspace, $"{settingsName}{roleName}", cancel).ConfigureAwait(false);
        }

        return group == null
            ? HasEnoughPermission(user, globalSettings ?? GetSettingsByName<Settings>(settingsName, Identifiers.RootPath), role)
            : user.IsInGroup(group);
    }
    private static async Task<IGroup> GetLocalGroupAsync(Workspace workspace, string groupName, CancellationToken cancel)
    {
        while (true)
        {
            var group = await workspace.GetLocalGroup(groupName, cancel);
            if (group != null)
                return group;

            var parent = workspace.Parent as GenericContent;
            workspace = parent?.Workspace as Workspace;
            if (workspace == null)
                return null;
        }
    }
    private static bool HasEnoughPermission(IUser user, Settings globalSettings, SettingsRole role)
    {
        PermissionType permissionType;
        switch (role)
        {
            case SettingsRole.Reader: permissionType = PermissionType.Open; break;
            case SettingsRole.Editor: permissionType = PermissionType.Save; break;
            default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }
        return globalSettings.Security.HasPermission(user, permissionType);
    }

    private static async Task<Settings> EnsureSettingsContentAsync(Content content, string name, CancellationToken cancel)
    {
        var settingsContentName = name + "." + EXTENSION;
        var basePath = content.Id == Identifiers.PortalRootId ? Repository.SystemFolderPath : content.Path;
        var settingsContainerPath = RepositoryPath.Combine(basePath, Repository.SettingsFolderName);
        var settingsPath = RepositoryPath.Combine(settingsContainerPath, settingsContentName);
        var settings = await Node.LoadAsync<Settings>(settingsPath, cancel).ConfigureAwait(false);

        if (settings == null)
        {
            var settingsContainer = await Node.LoadNodeAsync(settingsContainerPath, cancel).ConfigureAwait(false);
            if (settingsContainer == null)
            {
                settingsContainer = new SystemFolder(content.ContentHandler)
                {
                    Name = Repository.SettingsFolderName,
                    DisplayName = Repository.SettingsFolderName
                };
                await settingsContainer.SaveAsync(cancel).ConfigureAwait(false);
            }

            settings = new Settings(settingsContainer)
            {
                Name = settingsContentName,
                DisplayName = settingsContentName
            };
        }

        return settings;
    }

    private static Exception GetCannotCreateUnderSettingsException(string contentPath)
    {
        return new InvalidContentActionException("Cannot create settings under any 'Settings' folder. " +
                                                 "Choose a parent content. Requested path: " + contentPath);
    }
}
