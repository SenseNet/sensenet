using System;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Represents an access token for various authentication or authorization purposes.
    /// </summary>
    public class AccessToken
    {
        /// <summary>
        /// Gets the general database identifier of the object.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets the value of the token.
        /// </summary>
        public string Value { get; internal set; }

        /// <summary>
        /// Gets the Id of the owner User.
        /// </summary>
        public int UserId { get; internal set; }

        /// <summary>
        /// Gets the associated Id of the content.
        /// The value is 0 if there is no association.
        /// </summary>
        public int ContentId { get; internal set; }

        /// <summary>
        /// Gets the associated feature name if there is one.
        /// Default value is null.
        /// </summary>
        public string Feature { get; internal set; }

        /// <summary>
        /// Gets the cretion date of the token.
        /// </summary>
        public DateTime CreationDate { get; internal set; }

        /// <summary>
        /// Gets or sets the expiration date of the token.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Returns the string representation of the token.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }
    }
}
