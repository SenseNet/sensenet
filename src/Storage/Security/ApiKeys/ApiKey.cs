using System;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    public class ApiKey
    {
        public string Value { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
