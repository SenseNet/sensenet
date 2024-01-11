using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SenseNet.ContentRepository
{
    public partial class Content
    {
        [Obsolete("Use Content.Create instead")]
        public Content()
        {

        }

        /// <summary>
        /// Validates and saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository with considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid 
        ///         throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> after the saving
        /// its version is depends its <see cref="SenseNet.ContentRepository.GenericContent.VersioningMode">VersioningMode</see> setting.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        [Obsolete("Use async version instead.", true)]
        public void Save()
        {
            SaveAsync(true, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void Save(bool validOnly)
        {
            SaveAsync(validOnly, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void Save(SavingMode mode)
        {
            SaveAsync(mode, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository with considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If passed <paramref name="validOnly">validOnly</paramref> parameter is true  and <c>Content</c> is not valid 
        ///         throws an <see cref="InvalidContentException">InvalidContentException</see>
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> after the saving
        /// its version is depends its <see cref="SenseNet.ContentRepository.GenericContent.VersioningMode">VersioningMode</see> setting.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <paramref name="validOnly"> is true  and<c>Content</c> is invalid.</exception>
        [Obsolete("Use async version instead.", true)]
        public void Save(bool validOnly, SavingMode mode)
        {
            SaveAsync(validOnly, mode, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ends the multistep saving process and makes the content available for modification.
        /// </summary>
        [Obsolete("Use async version instead.", true)]
        public void FinalizeContent()
        {
            FinalizeContentAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates and saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository without considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If passed <paramref name="validOnly">validOnly</paramref> parameter is true  and <c>Content</c> is not valid 
        ///         throws an <see cref="InvalidContentException">InvalidContentException</see>
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// After the saving the version of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> will not changed.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <paramref name="validOnly"> is true  and<c>Content</c> is invalid.</exception>
        [Obsolete("Use async version instead.", true)]
        public void SaveSameVersion(bool validOnly)
        {
            SaveSameVersionAsync(validOnly, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void SaveExplicitVersion(bool validOnly = true)
        {
            SaveExplicitVersionAsync(CancellationToken.None, validOnly).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates and publishes the wrapped <c>ContentHandler</c> if it is a <c>GenericContent</c> otherwise saves it normally.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        ///         If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        ///         the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> calls its
        ///         <see cref="SenseNet.ContentRepository.GenericContent.Publish">Publish</see> method otherwise saves it normally.
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        [Obsolete("Use async version instead.", true)]
        public void Publish()
        {
            PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void Approve()
        {
            ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void Reject()
        {
            RejectAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// 
        /// If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        /// 
        /// If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> calls its
        /// <see cref="SenseNet.ContentRepository.GenericContent.CheckIn">CheckIn</see> method otherwise calls the
        /// <see cref="SenseNet.ContentRepository.Storage.Node.Lock.Unlock">Unlock</see> method with
        /// <c><see cref="SenseNet.ContentRepository.Storage.VersionStatus">VersionStatus</see>.Public</c> and 
        /// <c><see cref="SenseNet.ContentRepository.Storage.VersionRaising">VersionRaising</see>.None</c> parameters.
        /// 
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        /// 		If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// 		the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> calls its
        /// 		<see cref="SenseNet.ContentRepository.GenericContent.CheckIn">CheckIn</see> method otherwise calls the
        /// 		<see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>'s
        /// 		<see cref="SenseNet.ContentRepository.Storage.Security.LockHandler.Unlock(VersionStatus, VersionRaising)">Lock.Unlock</see> method with
        /// 		<c><see cref="SenseNet.ContentRepository.Storage.VersionStatus">VersionStatus</see>.Public</c> and 
        /// 		<c><see cref="SenseNet.ContentRepository.Storage.VersionRaising">VersionRaising</see>.None</c> parameters.
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        [Obsolete("Use async version instead.", true)]
        public void CheckIn()
        {
            CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void CheckOut()
        {
            CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void UndoCheckOut()
        {
            UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead.", true)]
        public void ForceUndoCheckOut()
        {
            ForceUndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use ForceDelete method instead", true)]
        public static void DeletePhysical(int contentId)
        {
            Node.ForceDelete(contentId);
        }

        [Obsolete("Use ForceDelete method instead", true)]
        public static void DeletePhysical(string path)
        {
            Node.ForceDelete(path);
        }

        [Obsolete("Use async version instead")]
        public static void Delete(int contentId)
        {
            DeleteAsync(contentId, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead")]
        public static void Delete(string path)
        {
            DeleteAsync(path, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead")]
        public void Delete(bool byPassTrash)
        {
            DeleteAsync(byPassTrash, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead")]
        public void Delete()
        {
            DeleteAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes the <see cref="Content"/> permanently.
        /// </summary>
        [Obsolete("Use async version instead")]
        public void ForceDelete()
        {
            ForceDeleteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the Node and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        /// <param name="contentId">Identifier of the Node that will be deleted.</param>
        [Obsolete("Use async version instead")]
        public static void ForceDelete(int contentId)
        {
            ForceDeleteAsync(contentId, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes the Node and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        /// <param name="path">The path of the Node that will be deleted.</param>
        [Obsolete("Use async version instead")]
        public static void ForceDelete(string path)
        {
            ForceDeleteAsync(path, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [Obsolete("Use the methods of the ContentNamingProvider class instead")]
        public static string GenerateNameFromTitle(string parent, string title)
        {
            return ContentNamingProvider.GetNameFromDisplayName(title);
        }

        [Obsolete("Use the methods of the ContentNamingProvider class instead")]
        public static string GenerateNameFromTitle(string title)
        {
            return ContentNamingProvider.GetNameFromDisplayName(title);
        }

        /// <summary>
        /// Returns all conventional (non-virtual) actions available on the Content.
        /// </summary>
        /// <returns>An IEnumerable&lt;ActionBase&gt;</returns>
        [Obsolete("Use the Actions property instead")]
        public IEnumerable<ActionBase> GetContentActions()
        {
            return GetActions();
        }

        /// <summary>
        /// Rebuilds the index document of a content and optionally of all documents in the whole subtree. 
        /// In case the value of <value>rebuildLevel</value> is <value>IndexOnly</value> the index document is refreshed 
        /// based on the already existing extracted data stored in the database. This is a significantly faster method 
        /// and it is designed for cases when only the place of the content in the tree has changed or the index got corrupted.
        /// The <value>DatabaseAndIndex</value> algorithm will reindex the full content than update the index in the
        /// external index provider the same way as the light-weight algorithm.
        /// </summary>
        /// <param name="recursive">Whether child content should be reindexed or not. Default: false.</param>
        /// <param name="rebuildLevel">The algorithm selector. Value can be <value>IndexOnly</value> or <value>DatabaseAndIndex</value>. Default: <value>IndexOnly</value></param>
        [Obsolete("Use async version instead.", true)]
        public void RebuildIndex(bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            RebuildIndexAsync(CancellationToken.None, recursive, rebuildLevel).GetAwaiter().GetResult();
        }

    }
}
