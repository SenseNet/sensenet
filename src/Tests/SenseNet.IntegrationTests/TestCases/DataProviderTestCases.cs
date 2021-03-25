using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.IntegrationTests.Infrastructure;
using SR = SenseNet.ContentRepository.SR;

namespace SenseNet.IntegrationTests.TestCases
{
    public class DataProviderTestCases : TestCaseBase
    {
        public void DP_RefProp_Install(Func<Node, PropertyType, int[]> getReferencesFromDatabase)
        {
            Cache.Reset();

            IsolatedIntegrationTest(() =>
            {
                var group = Group.Administrators;
                var expectedIds = group.Members.Select(x => x.Id).ToList();
                var propertyType = ActiveSchema.PropertyTypes["Members"];
                var fromDb = getReferencesFromDatabase(group, propertyType);

                // ASSERT
                Assert.IsNotNull(fromDb);
                Assert.AreEqual(2, fromDb.Length);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", fromDb.OrderBy(x => x)));
            });
        }
        public void DP_RefProp_Insert(Func<Node, PropertyType, int[]> getReferencesFromDatabase)
        {
            IsolatedIntegrationTest(() =>
            {
                var propertyType = ActiveSchema.PropertyTypes["Members"];
                var expectedIds = new List<int>();

                var user1 = new User(OrganizationalUnit.Portal) {Name = "User-1", Email = "user1@example.com", Enabled = true};
                user1.Save();
                expectedIds.Add(user1.Id);
                var user2 = new User(OrganizationalUnit.Portal) {Name = "User-2", Email = "user2@example.com", Enabled = true};
                user2.Save();
                expectedIds.Add(user2.Id);

                // ACTION
                var group1 = new Group(OrganizationalUnit.Portal) {Name = "Group-1", Members = new[] {user1, user2}};
                group1.Save();

                // ASSERT
                var after = getReferencesFromDatabase(group1, propertyType);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }
        public void DP_RefProp_Update(Func<Node, PropertyType, int[]> getReferencesFromDatabase)
        {
            Cache.Reset();

            IsolatedIntegrationTest(() =>
            {
                var group = Group.Administrators;
                var expectedIds = group.Members.Select(x => x.Id).ToList();
                var propertyType = ActiveSchema.PropertyTypes["Members"];

                var user = new User(OrganizationalUnit.Portal) {Name = "User-1", Email = "user1@example.com", Enabled = true};
                user.Save();
                expectedIds.Add(user.Id);

                // ACTION
                Group.Administrators.AddMember(user);

                // ASSERT
                var after = getReferencesFromDatabase(group, propertyType);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }
        public void DP_RefProp_Delete(Func<Node, PropertyType, int[]> getReferencesFromDatabase)
        {
            IsolatedIntegrationTest(() =>
            {
                var propertyType = ActiveSchema.PropertyTypes["Members"];

                var user1 = new User(OrganizationalUnit.Portal) {Name = "User-1", Email = "user1@example.com", Enabled = true};
                user1.Save();
                var user2 = new User(OrganizationalUnit.Portal) {Name = "User-2", Email = "user2@example.com", Enabled = true};
                user2.Save();
                var group1 = new Group(OrganizationalUnit.Portal) {Name = "Group-1", Members = new[] { user1, user2 }};
                group1.Save();

                // ACTION
                group1 = Node.Load<Group>(group1.Id);
                group1.ClearReference(propertyType);
                group1.Save();

                // ASSERT
                var after = getReferencesFromDatabase(group1, propertyType);
                Assert.IsNull(after);
            });
        }
    }
}
