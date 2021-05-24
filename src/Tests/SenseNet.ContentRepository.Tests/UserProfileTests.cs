using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class UserProfileTests : TestBase
    {
        [TestMethod]
        public void UserProfile_Create()
        {
            Test(repoBuilder =>
            {
                // switch UserProfilesEnabled config value ON
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"sensenet:identityManagement:UserProfilesEnabled", "true"}
                    })
                    .Build();

                repoBuilder.UseConfiguration(config);
            }, () =>
            {
                CreateProfileTemplate();
                
                var parentPath = RepositoryPath.Combine(RepositoryStructure.ImsFolderPath, IdentityManagement.BuiltInDomainName);
                var parent = Node.LoadNode(parentPath);
                var user1 = new User(parent)
                {
                    Name = "sample-sam",
                    LoginName = "samplesam@example.com",
                    Email = "samplesam@example.com"
                };
                user1.Save();

                // check if all items in the newly created profile subtree have the user as their Owner
                foreach (var content in Content.All.Where(c => c.InTree(user1.ProfilePath)))
                {
                    Assert.AreEqual(user1.Id, content.ContentHandler.OwnerId, $"Unexpected owner " +
                                                                              $"({content.ContentHandler.Owner.Path}) of " +
                                                                              $"content {content.Path}");
                }

                // check if the user got an explicit entry on the profile
                Assert.IsTrue(user1.Profile.Security.GetExplicitEntries()
                    .Any(ace =>
                    {
                        if (ace.IdentityId != user1.Id || ace.LocalOnly)
                            return false;
                        return (ace.AllowBits & PermissionType.Open.Mask) == PermissionType.Open.Mask;
                    }), "Permission for the user on the profile is missing or incorrect.");

                // create a test file as an admin
                var folder = Node.LoadNode(RepositoryPath.Combine(user1.ProfilePath, "DocLib/f1"));
                var file = new File(folder);
                file.Save();

                // the user should see the file but cannot delete it
                Assert.IsTrue(file.OwnerId == Identifiers.AdministratorUserId);
                Assert.IsTrue(file.CreatedById == Identifiers.AdministratorUserId);
                Assert.IsTrue(file.Security.HasPermission(user1, PermissionType.Open),
                    "User does not have Open permission on a file created under their profile by the admin.");

                Assert.IsFalse(file.Security.HasPermission(user1, PermissionType.Delete),
                    "User (incorrectly) has Delete permission on a file created under their profile by the admin.");
            });
        }

        private void CreateProfileTemplate()
        {
            RepositoryTools.CreateStructure("/Root/ContentTemplates/UserProfile", "SystemFolder");
            RepositoryTools.CreateStructure("/Root/ContentTemplates/UserProfile/UserProfile", "UserProfile");
            RepositoryTools.CreateStructure("/Root/ContentTemplates/UserProfile/UserProfile/DocLib", "DocumentLibrary");
            RepositoryTools.CreateStructure("/Root/ContentTemplates/UserProfile/UserProfile/DocLib/f1/f2", "Folder");
        }
    }
}
