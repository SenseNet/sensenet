using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage
{
    public interface IContentNamingProvider
    {
        string GenerateNewName(string nameBase, IContentType contentType, Node parent);
        string GenerateNameFromDisplayName(string originalName, string displayName);
        void AssertNameIsValid(string name);
        string GetNextNameSuffix(string currentName, int parentNodeId = 0);
        int GetNameBaseAndSuffix(string name, out string nameBase);
    }
}
