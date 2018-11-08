using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Contains sharing-related OData actions.
    /// </summary>
    public static class SharingActions
    {
        /// <summary>
        /// Gets a list of all sharing records on a content.
        /// </summary>
        [ODataFunction]
        public static object GetSharing(Content content)
        {
            var gc = EnsureContent(content);

            //UNDONE: security: make sure the client does not get info without permission (e.g. user/group ids)
            //UNDONE: create a strongly typed result instead of double serialization
            return gc.Sharing.Items.Select(shi => (Dictionary<string, object>) JsonConvert.DeserializeObject(
                JsonConvert.SerializeObject(SafeSharingData.Create(shi)),
                typeof(Dictionary<string, object>))).ToList();
        }

        /// <summary>
        /// Shares a content with somebody.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="token">An identifier token: an email address, a username or a user or group id.</param>
        /// <param name="level">What permissions will the user get for the content.</param>
        /// <param name="mode">Whether the content will be accessible for other users.</param>
        /// <param name="sendNotification">Whether a notification email should be sent to the target user.</param>
        /// <returns>A sharing record representing the new share.</returns>
        [ODataAction]
        public static object Share(Content content, string token, SharingLevel level, SharingMode mode, bool sendNotification)
        {
            var gc = EnsureContent(content);

            //UNDONE: security: make sure the client does not get info without permission (e.g. user/group ids)
            return SafeSharingData.Create(gc.Sharing.Share(token, level, mode, sendNotification));
        }
        /// <summary>
        /// Remove a sharing record from a content.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="id">Identifier of a sharing record.</param>
        /// <returns>Returns true if the system has found and removed the sharing record.</returns>
        [ODataAction]
        public static object RemoveSharing(Content content, string id)
        {
            var gc = EnsureContent(content);

            return gc.Sharing.RemoveSharing(id);
        }

        private static GenericContent EnsureContent(Content content)
        {
            // This method makes sure that the content exists and it can be shared.
            // Authorization is done on the application content (RequiredPermissions).

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (!(content.ContentHandler is GenericContent gc))
                throw new InvalidOperationException(
                    $"Sharing works only on generic content not on {content.ContentType.Name}. Path: {content.Path}");

            return gc;
        }
    }
}
