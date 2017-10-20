using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SenseNet.ContentRepository.Storage;
using System.Web;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Versioning;
using System.Globalization;
using System.Linq;
using SenseNet.Diagnostics;
using System.Security.Cryptography;
using SenseNet.ApplicationModel;
using System.Web.Hosting;
using SenseNet.ContentRepository.Storage.Security;
using Newtonsoft.Json;
using SenseNet.Security;
using SenseNet.Search;
using System.Diagnostics;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.TaskManagement.Core;

namespace SenseNet.ContentRepository
{
    public static class RepositoryTools
    {
        public static string GetStreamString(Stream stream)
        {
            StreamReader sr = new StreamReader(stream);
            stream.Position = 0;
            return sr.ReadToEnd();
        }
        public static Stream GetStreamFromString(string textData)
        {
            var stream = new MemoryStream();

            // Write to the stream only if the text is not empty, because writing an empty
            // string in UTF-8 format would result in a 3 bytes length stream.
            if (!string.IsNullOrEmpty(textData))
            {
                var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.Write(textData);
                writer.Flush();

                stream.Position = 0;
            }

            return stream;
        }

        public static CultureInfo GetUICultureByNameOrDefault(string cultureName)
        {
            CultureInfo cultureInfo = null;

            if (!String.IsNullOrEmpty(cultureName))
            {
                cultureInfo = (from c in CultureInfo.GetCultures(CultureTypes.AllCultures)
                               where c.Name == cultureName
                               select c).FirstOrDefault();
            }
            if (cultureInfo == null)
                cultureInfo = CultureInfo.CurrentUICulture;

            return cultureInfo;
        }

        public static string GetVersionString(Node node)
        {
            string extraText = string.Empty;
            switch (node.Version.Status)
            {
                case VersionStatus.Pending: extraText = HttpContext.GetGlobalResourceObject("Portal", "Approving") as string; break;
                case VersionStatus.Draft: extraText = HttpContext.GetGlobalResourceObject("Portal", "Draft") as string; break;
                case VersionStatus.Locked:
                    var lockedByName = node.Lock.LockedBy == null ? "" : node.Lock.LockedBy.Name;
                    extraText = string.Concat(HttpContext.GetGlobalResourceObject("Portal", "CheckedOutBy") as string, " ", lockedByName);
                    break;
                case VersionStatus.Approved: extraText = HttpContext.GetGlobalResourceObject("Portal", "Public") as string; break;
                case VersionStatus.Rejected: extraText = HttpContext.GetGlobalResourceObject("Portal", "Reject") as string; break;
            }

            var content = node as GenericContent;
            var vmode = VersioningType.None;
            if (content != null)
                vmode = content.VersioningMode;

            if (vmode == VersioningType.None)
                return extraText;
            if (vmode == VersioningType.MajorOnly)
                return string.Concat(node.Version.Major, " ", extraText);
            return string.Concat(node.Version.Major, ".", node.Version.Minor, " ", extraText);
        }

        public static string CalculateMD5(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            using (var stream = new MemoryStream(bytes))
            {
                return CalculateMD5(stream, 64 * 1024);
            }
        }

        public static string CalculateMD5(Stream stream, int bufferSize)
        {
            MD5 md5Hasher = MD5.Create();

            byte[] buffer = new byte[bufferSize];
            int readBytes;

            while ((readBytes = stream.Read(buffer, 0, bufferSize)) > 0)
            {
                md5Hasher.TransformBlock(buffer, 0, readBytes, buffer, 0);
            }

            md5Hasher.TransformFinalBlock(new byte[0], 0, 0);

            var result = md5Hasher.Hash.Aggregate(string.Empty, (full, next) => full + next.ToString("x2"));
            return result;
        }

        private static readonly char[] _availableRandomChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// Generates a random string consisting of <paramref name="length">length</paramref> number of characters, using RNGCryptoServiceProvider.
        /// </summary>
        /// <param name="length">The length of the generated string.</param>
        /// <returns>A string consisting of random characters.</returns>
        public static string GetRandomString(int length)
        {
            return GetRandomString(length, _availableRandomChars);
        }

        /// <summary>
        /// Generates a random string consisting of <paramref name="length">length</paramref> number of characters, using RNGCryptoServiceProvider.
        /// </summary>
        /// <param name="length">The length of the generated string.</param>
        /// <param name="availableCharacters">Characters that can be used in the random string.</param>
        /// <returns>A string consisting of random characters.</returns>
        public static string GetRandomString(int length, char[] availableCharacters)
        {
            if (availableCharacters == null)
                throw new ArgumentNullException("availableCharacters");
            if (availableCharacters.Length == 0)
                throw new ArgumentException("Available characters array must contain at least one character.");

            var rng = new RNGCryptoServiceProvider();
            var random = new byte[length];
            rng.GetNonZeroBytes(random);

            var buffer = new char[length];
            var characterTableLength = availableCharacters.Length;

            for (var index = 0; index < length; index++)
            {
                buffer[index] = availableCharacters[random[index] % characterTableLength];
            }

            return new string(buffer);
        }

        /// <summary>
        /// Generates a random string using RNGCryptoServiceProvider. The length of the string will be bigger
        /// than <paramref name="byteLength">byteLength</paramref> because the result bytes will be converted to string using Base64 conversion.
        /// </summary>
        /// <param name="byteLength">The length of the randomly generated byte array that will be converted to string.</param>
        /// <returns>A string consisting of random characters.</returns>
        public static string GetRandomStringBase64(int byteLength)
        {
            var randomBytes = new byte[byteLength];
            var rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Converts the given datetime to a datetime in UTC format. If it is already in UTC, there will be 
        /// no conversion. Undefined datetime will be considered as UTC. A duplicate of this method exists 
        /// in the Storage layer.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        internal static DateTime ConvertToUtcDateTime(DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Local:
                    return dateTime.ToUniversalTime();
                case DateTimeKind.Utc:
                    return dateTime;
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                default:
                    throw new InvalidOperationException("Unknown datetime kind: " + dateTime.Kind);
            }
        }

        public static bool IsExecutableExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            return Repository.ExecutableExtensions.Any(e => string.Compare(e, extension.Trim('.'), StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        public static bool IsExecutableType(NodeType nodeType)
        {
            if (nodeType == null)
                return false;

            return Repository.ExecutableFileTypeNames.Any(tn => string.Compare(tn, nodeType.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        public static void AssertArgumentNull(object value, string name)
        {
            if (value == null)
                throw new ArgumentNullException(name);
        }

        public static string GetClientIpAddress()
        {
            if (HttpContext.Current == null)
                return string.Empty;

            var clientIpAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(clientIpAddress))
                return clientIpAddress;

            clientIpAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            if (!string.IsNullOrEmpty(clientIpAddress))
                return clientIpAddress;

            return HttpContext.Current.Request.UserHostAddress ?? string.Empty;
        }

        // Structure building ==================================================================

        public static Content CreateStructure(string path)
        {
            return CreateStructure(path, "Folder");
        }

        public static Content CreateStructure(string path, string containerTypeName)
        {
            // check path validity before calling the recursive method
            if (string.IsNullOrEmpty(path))
                return null;

            RepositoryPath.CheckValidPath(path);

            return EnsureContainer(path, containerTypeName);
        }

        private static Content EnsureContainer(string path, string containerTypeName)
        {
            if (Node.Exists(path))
                return null;

            var name = RepositoryPath.GetFileName(path);
            var parentPath = RepositoryPath.GetParentPath(path);

            // recursive call to create parent containers
            EnsureContainer(parentPath, containerTypeName);

            return CreateContent(parentPath, name, containerTypeName);
        }

        private static Content CreateContent(string parentPath, string name, string typeName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");

            var parent = Node.LoadNode(parentPath);

            if (parent == null)
                throw new ApplicationException("Parent does not exist: " + parentPath);

            // don't use admin account here, that should be 
            // done in the calling 'client' code if needed
            var content = Content.CreateNew(typeName, parent, name);
            content.Save();

            return content;
        }

        // Diagnostics =========================================================================

        public static string CollectExceptionMessages(Exception ex)
        {
            var sb = new StringBuilder();
            var e = ex;
            while (e != null)
            {
                sb.AppendLine(e.Message).AppendLine(e.StackTrace).AppendLine("-----------------");
                e = e.InnerException;
            }
            return sb.ToString();
        }


        /// <summary>
        /// Checks all IFolder objects in the repository and returns all paths where AllowedChildTypes is empty. Paths are categorized by content type names.
        /// This method is allowed to call as Generic OData Application.
        /// </summary>
        /// <param name="root">Subtree to check. Null means /Root content</param>
        /// <returns>Paths where AllowedChildTypes is empty categorized by content type names.</returns>
        [ODataFunction]
        public static Dictionary<string, List<string>> CheckAllowedChildTypesOfFolders(Content root)
        {
            var result = new Dictionary<string, List<string>>();
            var rootPath = root != null ? root.Path : Identifiers.RootPath;
            foreach (var node in NodeEnumerator.GetNodes(rootPath))
            {
                if (!(node is IFolder))
                    continue;

                var gc = node as GenericContent;
                if (gc == null)
                    continue;

                var t = node.NodeType.Name;
                if (t == "SystemFolder" || t == "Folder" || t == "Page")
                    continue;

                if (gc.GetAllowedChildTypeNames().Count() > 0)
                    continue;

                if (!result.ContainsKey(t))
                    result.Add(t, new List<string> { gc.Path });
                else
                    result[t].Add(gc.Path);
            }
            return result;
        }
        
        [ODataFunction]
        public static IEnumerable<Content> GetListOfAllContentTypes(Content content)
        {
            return ContentType.GetContentTypes().Select(ct => Content.Create(ct));
        }

        [ODataFunction]
        public static IEnumerable<Content> GetAllowedChildTypesFromCTD(Content content)
        {
            return content.ContentType.AllowedChildTypes.Select(ct => Content.Create(ct));
        }

        /// <summary>
        /// Returns a path list containing items that have explicit security entry for Everyone group but does not have explicit security entry for Visitor user.
        /// </summary>
        /// <param name="root">Examination scope.</param>
        /// <returns></returns>
        [ODataFunction]
        public static IEnumerable<string> MissingExplicitEntriesOfVisitorComparedToEveryone(Content root)
        {
            var visitorId = User.Visitor.Id;
            var everyoneId = Group.Everyone.Id;
            var result = new List<string>();
            foreach (var node in NodeEnumerator.GetNodes(root.Path))
            {
                var hasEveryoneEntry = false;
                var hasVisitorEntry = false;
                foreach (var entry in node.Security.GetExplicitEntries())
                {
                    if (entry.IdentityId == everyoneId)
                        hasEveryoneEntry = true;
                    if (entry.IdentityId == visitorId)
                        hasVisitorEntry = true;
                }
                if (hasEveryoneEntry && !hasVisitorEntry)
                    result.Add(node.Path);
            }
            return result;
        }
        [ODataAction]
        public static string CopyExplicitEntriesOfEveryoneToVisitor(Content root, string[] exceptList)
        {
            var visitorId = User.Visitor.Id;
            var everyoneId = Group.Everyone.Id;
            var except = exceptList.Select(p => p.ToLower()).ToList();
            var ctx = SecurityHandler.SecurityContext;
            var aclEd = SecurityHandler.CreateAclEditor(ctx);
            foreach (var path in MissingExplicitEntriesOfVisitorComparedToEveryone(root))
            {
                if (!except.Contains(path.ToLower()))
                {
                    var node = Node.LoadNode(path);
                    var aces = ctx.GetExplicitEntries(node.Id, new[] { everyoneId });
                    foreach (var ace in aces)
                    {
                        aclEd.Set(node.Id, visitorId, ace.LocalOnly, ace.AllowBits, ace.DenyBits);
                    }
                }
            }
            aclEd.Apply();
            return "Ok";
        }

        /// <summary>
        /// Goes through the files in a directory (optionally also files in subdirectories) both in the file system and the repository.
        /// Returns true if the given path was a directory, false if it wasn't.
        /// </summary>
        public static bool RecurseFilesInVirtualPath(string path, bool includesubdirs, Action<string> action, bool skipRepo = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (action == null)
                throw new ArgumentNullException("action");
            if (path.StartsWith("http://") || path.StartsWith("https://") || path.ContainsIllegalCharacters())
                return false;

            var nodeHead = NodeHead.Get(path);
            var isFolder = nodeHead != null && nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom("Folder");
            var fsPath = HostingEnvironment.MapPath(path);

            // Take care of folders in the repository
            if (isFolder && !skipRepo)
            {
                // Find content items
                var contents = Content.All.DisableAutofilters()
                    .Where(c => (includesubdirs ? c.InTree(nodeHead.Path) : c.InFolder(nodeHead.Path)) && c.TypeIs(typeof(File).Name))
                    .OrderBy(c => c.Index);

                // Add paths
                foreach (var c in contents)
                    action(c.Path);
            }

            // Take care of folders in the file system
            if (!string.IsNullOrEmpty(fsPath) && Directory.Exists(fsPath))
            {
                // Add files
                foreach (var virtualPath in Directory.GetFiles(fsPath).Select(GetVirtualPath))
                {
                    action(virtualPath);
                }

                // Recurse subdirectories
                if (includesubdirs)
                {
                    foreach (var virtualPath in Directory.GetDirectories(fsPath).Select(GetVirtualPath))
                    {
                        RecurseFilesInVirtualPath(virtualPath, includesubdirs, action, true);
                    }
                }

                isFolder = true;
            }

            return isFolder;
        }

        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars().Concat(new[] {'?', '&', '#'}).ToArray();
        /// <summary>
        /// Checks whether the path contains characters that are considered illegal in a file system path. 
        /// Used before mapping a virtual path to a server file system path.
        /// </summary>
        private static bool ContainsIllegalCharacters(this string path)
        {
            return path.IndexOfAny(InvalidPathChars) >= 0;
        }

        private static string GetVirtualPath(string physicalPath)
        {
            return physicalPath.Replace(HostingEnvironment.ApplicationPhysicalPath, HostingEnvironment.ApplicationVirtualPath).Replace(@"\", "/");
        }

        // ======================================================================================

        [ODataAction]
        public static void TakeOwnership(Content content, string userOrGroup)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            Content target = null;
            if (!String.IsNullOrEmpty(userOrGroup))
            {
                target = Content.LoadByIdOrPath(userOrGroup);
                if (target == null)
                    throw new ArgumentException("The parameter cannot be recognized as a path or an Id: " + userOrGroup);
            }

            if (SecurityHandler.HasPermission(content.Id, PermissionType.TakeOwnership))
            {
                if (target == null)
                {
                    // if the input string was null or empty
                    content["Owner"] = User.Current;
                }
                else
                {
                    if (target.ContentHandler is Group)
                        content["Owner"] = target.ContentHandler as Group;
                    else if (target.ContentHandler is User)
                        content["Owner"] = target.ContentHandler as User;
                    else
                        throw new ArgumentException("The parameter cannot be recognized as a User or a Group: " + userOrGroup);
                }

                content.Save();
            }
        }

        [ODataAction]
        public static string TakeLockOver(Content content, string user)
        {
            content.ContentHandler.Lock.TakeLockOver(GetUserFromString(user));

            return "Ok";
        }

        private static User GetUserFromString(string user)
        {
            User targetUser = null;
            if (!String.IsNullOrEmpty(user))
            {
                int userId;
                if (Int32.TryParse(user, out userId))
                    targetUser = Node.LoadNode(userId) as User;
                else
                    if (RepositoryPath.IsValidPath(user) == RepositoryPath.PathResult.Correct)
                        targetUser = Node.LoadNode(user) as User;
                    else
                        throw new ArgumentException("The 'user' parameter cannot be recognized as a path or an Id: " + user);
                if (targetUser == null)
                    throw new ArgumentException("User not found by the parameter: " + user);
            }
            return targetUser;
        }

        public static class OData
        {
            public static string CreateSingleContentUrl(Content content, string operationName = null)
            {
                return string.Format("/" + Configuration.Services.ODataServiceToken + "{0}('{1}'){2}",
                    content.ContentHandler.ParentPath,
                    content.Name,
                    string.IsNullOrEmpty(operationName) ? string.Empty : RepositoryPath.PathSeparator + operationName);
            }
        }

        [ODataFunction]
        public static SenseNet.Security.Messaging.SecurityActivityHistory GetRecentSecurityActivities(Content content)
        {
            return SecurityHandler.SecurityContext.GetRecentActivities();
        }
        [ODataFunction]
        public static IndexingActivityHistory GetRecentIndexingActivities(Content content)
        {
            return IndexingActivityHistory.GetHistory();
        }
        [ODataAction]
        public static IndexingActivityHistory ResetRecentIndexingActivities(Content content)
        {
            return IndexingActivityHistory.Reset();
        }


        [ODataFunction]
        public static object CheckIndexIntegrity(Content content, bool recurse)
        {
            return IntegrityChecker.CheckIndexIntegrity(content?.Path, recurse);
        }
        [ODataFunction]
        public static SecurityConsistencyResult CheckSecurityConsistency(Content content)
        {
            var groups = SenseNet.ContentRepository.Storage.Search.NodeQuery.QueryNodesByType(NodeType.GetByName("Group"), false).Identifiers;
            var ous = SenseNet.ContentRepository.Storage.Search.NodeQuery.QueryNodesByType(NodeType.GetByName("OrganizationalUnit"), false).Identifiers;
            var allGroups = groups.Union(ous).ToArray();
            var allIds = SenseNet.ContentRepository.Storage.Search.NodeQuery.QueryNodesByPath("/", false).Identifiers;

            return CheckSecurityConsistency(allIds, allGroups);
        }

        /// <summary>
        /// Slow method for mapping all the possible inconsistencies in the 
        /// repository and the security component's stored and cached values.
        /// </summary>
        /// <param name="contentIds">List of all content ids in the repository.</param>
        /// <param name="groupIds">List of all the security containers in the repository. It will be enumerated once.</param>
        private static SecurityConsistencyResult CheckSecurityConsistency(IEnumerable<int> contentIds, IEnumerable<int> groupIds)
        {
            var result = new SecurityConsistencyResult();
            result.StartTimer();

            var secCachedEntities = SecurityHandler.GetCachedEntities();

            CheckSecurityEntityConsistency(contentIds, secCachedEntities, result);
            CheckMembershipConsistency(groupIds, result);
            CheckAceConsistency(result, secCachedEntities);

            result.StopTimer();

            return result;
        }
        private static void CheckSecurityEntityConsistency(IEnumerable<int> contentIds, IDictionary<int, SecurityEntity> secCachedEntities, SecurityConsistencyResult result)
        {
            var secDbEntities = SecurityHandler.SecurityContext.DataProvider.LoadSecurityEntities().ToList(); // convert to list, because we will modify this collection
            var foundEntities = new List<StoredSecurityEntity>();

            foreach (var contentId in contentIds)
            {
                var nh = NodeHead.Get(contentId);

                // content exists in the index but not in the db (deleted manually from the db)
                if (nh == null)
                {
                    result.AddMissingEntityFromRepository(contentId);
                    continue;
                }

                var secEntity = secDbEntities.FirstOrDefault(se => se.Id == contentId);
                if (secEntity == null || secEntity.ParentId != nh.ParentId || secEntity.OwnerId != nh.OwnerId)
                {
                    // not found in the security db, or found it but with different properties
                    result.AddMissingEntityFromSecurityDb(nh);
                    continue;
                }

                // move correctly found entities to a temp list
                foundEntities.Add(secEntity);
                secDbEntities.Remove(secEntity);
            }

            // the remaining ones are not in SN repo
            foreach (var secEntity in secDbEntities)
            {
                result.AddMissingEntityFromRepository(secEntity.Id);
            }

            // find entities that are in db but not in memory
            foreach (var secDbEntityId in secDbEntities.Concat(foundEntities).Select(dbe => dbe.Id).Except(secCachedEntities.Keys))
            {
                result.AddMissingEntityFromSecurityCache(secDbEntityId);
            }

            // find entities that are in memory but not in db
            foreach (var cachedEntityId in secCachedEntities.Keys.Except(secDbEntities.Concat(foundEntities).Select(dbe => dbe.Id)))
            {
                result.AddMissingEntityFromSecurityDb(cachedEntityId);
            }
        }
        private static void CheckMembershipConsistency(IEnumerable<int> groupIds, SecurityConsistencyResult result)
        {
            var secuCache = SecurityHandler.SecurityContext.GetCachedMembershipForConsistencyCheck();
            var secuDb = SecurityHandler.SecurityContext.DataProvider.GetMembershipForConsistencyCheck();

            var repo = new List<long>();
            foreach (var head in groupIds.Select(NodeHead.Get).Where(h => h != null))
            {
                var groupIdBase = Convert.ToInt64(head.Id) << 32;
                var userMembers = new List<int>();
                var groupMembers = new List<int>();

                CollectSecurityIdentityChildren(head, userMembers, groupMembers);

                foreach (var userId in userMembers)
                    repo.Add(groupIdBase + userId);

                foreach (var groupId in groupMembers)
                    repo.Add(groupIdBase + groupId);
            }

            // ---------------------------------------------------------

            var missingInSecuCache = repo.Except(secuCache);
            foreach (var relation in missingInSecuCache)
                result.AddMissingMembershipFromCache(unchecked((int)(relation >> 32)), unchecked((int)(relation & 0xFFFFFFFF)));

            var missingInSecuDb = repo.Except(secuDb);
            foreach (var relation in missingInSecuDb)
                result.AddMissingMembershipFromSecurityDb(unchecked((int)(relation >> 32)), unchecked((int)(relation & 0xFFFFFFFF)));

            var unknownInSecuCache = secuCache.Except(repo);
            foreach (var relation in unknownInSecuCache)
                result.AddUnknownMembershipInCache(unchecked((int)(relation >> 32)), unchecked((int)(relation & 0xFFFFFFFF)));

            var unknownInSecuDb = secuDb.Except(repo);
            foreach (var relation in unknownInSecuDb)
                result.AddUnknownMembershipInSecurityDb(unchecked((int)(relation >> 32)), unchecked((int)(relation & 0xFFFFFFFF)));

            // ---------------------------------------------------------

            IEnumerable<long> missingInFlattening, unknownInFlattening;
            SecurityHandler.SecurityContext.GetFlatteningForConsistencyCheck(out missingInFlattening, out unknownInFlattening);

            foreach (var relation in missingInFlattening)
                result.AddMissingRelationFromFlattenedUsers(unchecked((int)(relation >> 32)), unchecked((int)(relation & 0xFFFFFFFF)));

            foreach (var relation in unknownInFlattening)
                result.AddMissingRelationFromFlattenedUsers(unchecked((int)(relation >> 32)), unchecked((int)(relation & 0xFFFFFFFF)));
        }
        private static void CollectSecurityIdentityChildren(NodeHead head, ICollection<int> userIds, ICollection<int> groupIds)
        {
            // collect physical children (applies for orgunits)
            foreach (var childHead in ContentQuery.Query(SafeQueries.InFolder, QuerySettings.AdminSettings, head.Path).Identifiers.Select(NodeHead.Get).Where(h => h != null))
            {
                // in case of identity types: simply add them to the appropriate collection and move on
                if (childHead.GetNodeType().IsInstaceOfOrDerivedFrom("User"))
                {
                    if (!userIds.Contains(childHead.Id))
                        userIds.Add(childHead.Id);
                }
                else if (childHead.GetNodeType().IsInstaceOfOrDerivedFrom("Group") ||
                    childHead.GetNodeType().IsInstaceOfOrDerivedFrom("OrganizationalUnit"))
                {
                    if (!groupIds.Contains(childHead.Id))
                        groupIds.Add(childHead.Id);
                }
                else
                {
                    // collect identities recursively (if we haven't visited this group yet)
                    if (!groupIds.Contains(childHead.Id))
                        CollectSecurityIdentityChildren(childHead, userIds, groupIds);
                }
            }

            // collect group members
            if (head.GetNodeType().IsInstaceOfOrDerivedFrom("Group"))
            {
                var group = Node.Load<Group>(head.Id);
                foreach (var memberGroup in group.GetMemberGroups())
                {
                    if (!groupIds.Contains(memberGroup.Id))
                        groupIds.Add(memberGroup.Id);
                }
                foreach (var memberUser in group.GetMemberUsers())
                {
                    if (!userIds.Contains(memberUser.Id))
                        userIds.Add(memberUser.Id);
                }
            }
        }
        private static void CheckAceConsistency(SecurityConsistencyResult result, IDictionary<int, SecurityEntity> secCachedEntities)
        {
            // Checks whether every ACE in the security db is valid for the repository: EntityId and IdentityId are 
            // exist as SecurityEntity.
            var storedAces = SecurityHandler.SecurityContext.DataProvider.LoadAllPermissionEntries();
            foreach (var storedAce in storedAces)
            {
                if (!secCachedEntities.ContainsKey(storedAce.EntityId))
                    result.AddInvalidAceMissingEntity(storedAce);
                if (!secCachedEntities.ContainsKey(storedAce.IdentityId))
                    result.AddInvalidAceMissingIdentity(storedAce);
            }
        }

        [ODataAction]
        public static void Ad2PortalSyncFinalizer(Content content, SnTaskResult result)
        {
            SnTaskManager.OnTaskFinished(result);

            // not enough information
            if (result.Task == null)
                return;

            try
            {
                if (!string.IsNullOrEmpty(result.ResultData))
                {
                    dynamic resultData = JsonConvert.DeserializeObject(result.ResultData);

                    SnLog.WriteInformation("AD sync finished. See details below.", EventId.DirectoryServices,
                        properties: new Dictionary<string, object>
                        {
                            {"SyncedObjects", resultData.SyncedObjects},
                            {"ObjectErrorCount", resultData.ObjectErrorCount},
                            {"ErrorCount", resultData.ErrorCount},
                            {"ElapsedTime", resultData.ElapsedTime}
                        });
                }
                else
                {
                    SnLog.WriteWarning("AD sync finished with no results.", EventId.DirectoryServices);
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, "Error during AD sync finalizer.", EventId.DirectoryServices);
            }

            // the task was executed successfully without an error message
            if (result.Successful && result.Error == null)
                return;

            SnLog.WriteError("Error during AD sync. " + result.Error);
        }
    }

    public struct SecurityMembershipInfo
    {
        public int GroupId { get; private set; }
        public int MemberId { get; private set; }
        public string GroupPath { get; private set; }
        public string MemberPath { get; private set; }

        public SecurityMembershipInfo(int groupId, int userId)
            : this()
        {
            GroupId = groupId;
            MemberId = userId;

            var gnh = NodeHead.Get(GroupId);
            var mnh = NodeHead.Get(MemberId);

            if (gnh != null)
                GroupPath = gnh.Path;
            if (mnh != null)
                MemberPath = mnh.Path;
        }

        // ====================================================================================== Equality implementation

        public override bool Equals(Object obj)
        {
            return obj is SecurityMembershipInfo && this == (SecurityMembershipInfo)obj;
        }
        public override int GetHashCode()
        {
            return GroupId.GetHashCode() ^ MemberId.GetHashCode();
        }
        public static bool operator ==(SecurityMembershipInfo x, SecurityMembershipInfo y)
        {
            return x.GroupId == y.GroupId && x.MemberId == y.MemberId;
        }
        public static bool operator !=(SecurityMembershipInfo x, SecurityMembershipInfo y)
        {
            return !(x == y);
        }
    }

    public struct SecurityEntityInfo
    {
        public int Id { get; private set; }
        public int ParentId { get; private set; }
        public int OwnerId { get; private set; }
        public string Path { get; private set; }

        private NodeHead _nodeHead;

        // ====================================================================================== Constructors

        public SecurityEntityInfo(int contentId)
            : this()
        {
            Id = contentId;

            var head = NodeHead.Get(Id);
            if (head != null)
            {
                Path = head.Path;
                ParentId = head.ParentId;
                OwnerId = head.OwnerId;

                _nodeHead = head;
            }
        }

        public SecurityEntityInfo(NodeHead nodeHead)
            : this()
        {
            Id = nodeHead.Id;
            Path = nodeHead.Path;
            ParentId = nodeHead.ParentId;
            OwnerId = nodeHead.OwnerId;

            _nodeHead = nodeHead;
        }

        // ====================================================================================== Helper API

        public bool IsSkippableContent()
        {
            if (_nodeHead == null)
                return false;

            return SenseNet.Preview.DocumentPreviewProvider.Current.IsPreviewOrThumbnailImage(_nodeHead);
        }

        // ====================================================================================== Equality implementation

        public override bool Equals(Object obj)
        {
            return obj is SecurityEntityInfo && this == (SecurityEntityInfo)obj;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ParentId.GetHashCode() ^ OwnerId.GetHashCode();
        }
        public static bool operator ==(SecurityEntityInfo x, SecurityEntityInfo y)
        {
            return x.Id == y.Id && x.ParentId == y.ParentId && x.OwnerId == y.OwnerId;
        }
        public static bool operator !=(SecurityEntityInfo x, SecurityEntityInfo y)
        {
            return !(x == y);
        }
    }

    public class StoredAceDebugInfo
    {
        public int EntityId { get; set; }
        public int IdentityId { get; set; }
        public bool LocalOnly { get; set; }
        public ulong AllowBits { get; set; }
        public ulong DenyBits { get; set; }
        public string StringView { get; set; }

        public StoredAceDebugInfo(StoredAce ace)
        {
            this.EntityId = ace.EntityId;
            this.IdentityId = ace.IdentityId;
            this.LocalOnly = ace.LocalOnly;
            this.AllowBits = ace.AllowBits;
            this.DenyBits = ace.DenyBits;
            this.StringView = ace.ToString();
        }
    }

    public class SecurityConsistencyResult
    {
        public bool IsConsistent
        {
            get { return IsMembershipConsistent && IsEntityStructureConsistent && IsAcesConsistent; }
        }

        public bool IsMembershipConsistent
        {
            get
            {
                return MissingMembershipsFromCache.Count == 0 && UnknownMembershipInCache.Count == 0 &&
                    MissingMembershipsFromSecurityDb.Count == 0 && UnknownMembershipInSecurityDb.Count == 0 &&
                    MissingRelationFromFlattenedUsers.Count == 0 && UnknownRelationInFlattenedUsers.Count == 0;
            }
        }

        public bool IsEntityStructureConsistent
        {
            get
            {
                return MissingEntitiesFromRepository.Count == 0 &&
                    MissingEntitiesFromSecurityCache.Count == 0 &&
                    MissingEntitiesFromSecurityDb.Count == 0;
            }
        }

        public bool IsAcesConsistent
        {
            get
            {
                return InvalidACE_MissingEntity.Count == 0 &&
                    InvalidACE_MissingIdentity.Count == 0;
            }
        }

        public TimeSpan ElapsedTime
        {
            get { return _consistencyStopper.Elapsed; }
        }

        private Stopwatch _consistencyStopper;

        public List<SecurityEntityInfo> MissingEntitiesFromRepository { get; private set; }
        public List<SecurityEntityInfo> MissingEntitiesFromSecurityDb { get; private set; }
        public List<SecurityEntityInfo> MissingEntitiesFromSecurityCache { get; private set; }

        public List<SecurityMembershipInfo> MissingMembershipsFromCache { get; private set; }
        public List<SecurityMembershipInfo> UnknownMembershipInSecurityDb { get; private set; }
        public List<SecurityMembershipInfo> MissingMembershipsFromSecurityDb { get; private set; }
        public List<SecurityMembershipInfo> UnknownMembershipInCache { get; private set; }
        public List<SecurityMembershipInfo> MissingRelationFromFlattenedUsers { get; private set; }
        public List<SecurityMembershipInfo> UnknownRelationInFlattenedUsers { get; private set; }

        public List<StoredAceDebugInfo> InvalidACE_MissingEntity { get; private set; }
        public List<StoredAceDebugInfo> InvalidACE_MissingIdentity { get; private set; }

        public SecurityConsistencyResult()
        {
            MissingEntitiesFromRepository = new List<SecurityEntityInfo>();
            MissingEntitiesFromSecurityDb = new List<SecurityEntityInfo>();
            MissingEntitiesFromSecurityCache = new List<SecurityEntityInfo>();

            MissingMembershipsFromCache = new List<SecurityMembershipInfo>();
            UnknownMembershipInCache = new List<SecurityMembershipInfo>();
            MissingMembershipsFromSecurityDb = new List<SecurityMembershipInfo>();
            UnknownMembershipInSecurityDb = new List<SecurityMembershipInfo>();
            MissingRelationFromFlattenedUsers = new List<SecurityMembershipInfo>();
            UnknownRelationInFlattenedUsers = new List<SecurityMembershipInfo>();

            InvalidACE_MissingEntity = new List<StoredAceDebugInfo>();
            InvalidACE_MissingIdentity = new List<StoredAceDebugInfo>();
        }

        public void AddMissingMembershipFromCache(int groupId, int memberId)
        {
            AddMembershipInfoToList(groupId, memberId, MissingMembershipsFromCache);
        }
        public void AddUnknownMembershipInCache(int groupId, int memberId)
        {
            AddMembershipInfoToList(groupId, memberId, UnknownMembershipInCache);
        }
        public void AddMissingMembershipFromSecurityDb(int groupId, int memberId)
        {
            AddMembershipInfoToList(groupId, memberId, MissingMembershipsFromSecurityDb);
        }
        public void AddUnknownMembershipInSecurityDb(int groupId, int memberId)
        {
            AddMembershipInfoToList(groupId, memberId, UnknownMembershipInSecurityDb);
        }
        public void AddMissingRelationFromFlattenedUsers(int groupId, int memberId)
        {
            AddMembershipInfoToList(groupId, memberId, MissingRelationFromFlattenedUsers);
        }
        public void AddUnknownRelationInFlattenedUsers(int groupId, int memberId)
        {
            AddMembershipInfoToList(groupId, memberId, UnknownRelationInFlattenedUsers);
        }

        public void AddMissingEntityFromSecurityDb(NodeHead head)
        {
            AddMissingEntityToList(head, MissingEntitiesFromSecurityDb);
        }
        public void AddMissingEntityFromSecurityDb(int contentId)
        {
            AddMissingEntityToList(contentId, MissingEntitiesFromSecurityDb);
        }
        public void AddMissingEntityFromSecurityCache(int contentId)
        {
            AddMissingEntityToList(contentId, MissingEntitiesFromSecurityCache);
        }
        public void AddMissingEntityFromRepository(int contentId)
        {
            var sei = new SecurityEntityInfo(contentId);

            // workaround for non-indexed content (preview images): skip those items
            if (sei.IsSkippableContent())
                return;

            AddMissingEntityToList(sei, MissingEntitiesFromRepository);
        }

        private static void AddMembershipInfoToList(int groupId, int memberId, IList<SecurityMembershipInfo> membershipInfoList)
        {
            var smi = new SecurityMembershipInfo(groupId, memberId);

            if (membershipInfoList.All(c => c != smi))
                membershipInfoList.Add(smi);
        }
        private void AddMissingEntityToList(int contentId, IList<SecurityEntityInfo> entityInfoList)
        {
            AddMissingEntityToList(new SecurityEntityInfo(contentId), entityInfoList);
        }
        private void AddMissingEntityToList(SecurityEntityInfo entity, IList<SecurityEntityInfo> entityInfoList)
        {            
            if (entity != null && !entityInfoList.Any(c => c == entity))
                entityInfoList.Add(entity);
        }
        private void AddMissingEntityToList(NodeHead head, IList<SecurityEntityInfo> entityInfoList)
        {
            var sei = new SecurityEntityInfo(head);

            if (!entityInfoList.Any(c => c == sei))
                entityInfoList.Add(sei);
        }

        public void AddInvalidAceMissingEntity(StoredAce storedAce)
        {
            InvalidACE_MissingEntity.Add(new StoredAceDebugInfo(storedAce));
        }
        public void AddInvalidAceMissingIdentity(StoredAce storedAce)
        {
            InvalidACE_MissingIdentity.Add(new StoredAceDebugInfo(storedAce));
        }

        public void StartTimer()
        {
            _consistencyStopper = Stopwatch.StartNew();
        }

        public void StopTimer()
        {
            _consistencyStopper.Stop();
        }

    }
}
