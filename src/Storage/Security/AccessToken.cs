using System;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class AccessToken
    {
        public int Id { get; internal set; }

        public string Value { get; internal set; }

        public int UserId { get; internal set; }
        public int ContentId { get; internal set; }
        public string Feature { get; internal set; }

        public DateTime CreationDate { get; internal set; }
        public DateTime ExpirationDate { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
