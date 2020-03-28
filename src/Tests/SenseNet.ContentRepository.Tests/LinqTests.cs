using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Tests;
// ReSharper disable RedundantBoolCompare

namespace SenseNet.ContentRepository.Tests
{
    // Testmethod name convention: Linq_   query compilation tests
    //                             LinqEx_ execution tests e.g. First, ElementAt, Min etc.
    [TestClass]
    public class LinqTests : TestBase
    {
        [ContentHandler]
        public class RefTestNode : GenericContent, IFolder
        {
            public static string ContentTypeDefinition = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='RefTestNode' parentType='GenericContent' handler='" + typeof(RefTestNode).FullName + @"' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
	<Fields>
		<Field name='Mother' type='Reference'>
			<Configuration>
				<AllowedTypes>
					<Type>RefTestNode</Type>
				</AllowedTypes>
			</Configuration>
		</Field>
		<Field name='Neighbors' type='Reference'>
			<Configuration>
				<AllowMultiple>true</AllowMultiple>
				<AllowedTypes>
					<Type>RefTestNode</Type>
				</AllowedTypes>
			</Configuration>
		</Field>
	</Fields>
</ContentType>
";

            public RefTestNode(Node parent) : base(parent, "RefTestNode") { }
            public RefTestNode(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
            protected RefTestNode(NodeToken nt) : base(nt) { }

            #region Properties

            [RepositoryProperty("Wife", RepositoryDataType.Reference)]
            public RefTestNode Wife
            {
                get => GetReference<RefTestNode>("Wife");
                set => SetReference("Wife", value);
            }

            [RepositoryProperty("Husband", RepositoryDataType.Reference)]
            public RefTestNode Husband
            {
                get => GetReference<RefTestNode>("Husband");
                set => SetReference("Husband", value);
            }

            [RepositoryProperty("Mother", RepositoryDataType.Reference)]
            public RefTestNode Mother
            {
                get => GetReference<RefTestNode>("Mother");
                set => SetReference("Mother", value);
            }

            [RepositoryProperty("Father", RepositoryDataType.Reference)]
            public RefTestNode Father
            {
                get => GetReference<RefTestNode>("Father");
                set => SetReference("Father", value);
            }

            [RepositoryProperty("Daughter", RepositoryDataType.Reference)]
            public RefTestNode Daughter
            {
                get => GetReference<RefTestNode>("Daughter");
                set => SetReference("Daughter", value);
            }

            [RepositoryProperty("Son", RepositoryDataType.Reference)]
            public RefTestNode Son
            {
                get => GetReference<RefTestNode>("Son");
                set => SetReference("Son", value);
            }

            [RepositoryProperty("Sister", RepositoryDataType.Reference)]
            public RefTestNode Sister
            {
                get => GetReference<RefTestNode>("Sister");
                set => SetReference("Sister", value);
            }

            [RepositoryProperty("Brother", RepositoryDataType.Reference)]
            public RefTestNode Brother
            {
                get => GetReference<RefTestNode>("Brother");
                set => SetReference("Brother", value);
            }

            [RepositoryProperty("NickName", RepositoryDataType.String)]
            public string NickName
            {
                get => GetProperty<string>("NickName");
                set => this["NickName"] = value;
            }
            [RepositoryProperty("Age", RepositoryDataType.Int)]
            public int Age
            {
                get => GetProperty<int>("Age");
                set => this["Age"] = value;
            }


            [RepositoryProperty("Neighbors", RepositoryDataType.Reference)]
            public IEnumerable<Node> Neighbors
            {
                get => GetReferences("Neighbors");
                set => SetReferences("Neighbors", value);
            }

            #endregion

            public override object GetProperty(string name)
            {
                switch (name)
                {
                    case "Wife":
                        return Wife;
                    case "Husband":
                        return Husband;
                    case "Mother":
                        return Mother;
                    case "Father":
                        return Father;
                    case "Daughter":
                        return Daughter;
                    case "Son":
                        return Son;
                    case "Sister":
                        return Sister;
                    case "Brother":
                        return Brother;
                    case "NickName":
                        return NickName;
                    case "Age":
                        return Age;
                    default:
                        return base.GetProperty(name);
                }
            }
            public override void SetProperty(string name, object value)
            {
                switch (name)
                {
                    case "Wife":
                        Wife = (RefTestNode)value;
                        break;
                    case "Husband":
                        Husband = (RefTestNode)value;
                        break;
                    case "Mother":
                        Mother = (RefTestNode)value;
                        break;
                    case "Father":
                        Father = (RefTestNode)value;
                        break;
                    case "Daughter":
                        Daughter = (RefTestNode)value;
                        break;
                    case "Son":
                        Son = (RefTestNode)value;
                        break;
                    case "Sister":
                        Sister = (RefTestNode)value;
                        break;
                    case "Brother":
                        Brother = (RefTestNode)value;
                        break;
                    case "NickName":
                        NickName = (string)value;
                        break;
                    case "Age":
                        Age = (int)value;
                        break;
                    default:
                        base.SetProperty(name, value);
                        break;
                }
            }

            //================================================ IFolder

            public virtual IEnumerable<Node> Children => GetChildren();

            public virtual int ChildCount => GetChildCount();
        }


        [TestMethod]
        public void Linq_AsQueryable()
        {
            Assert.IsTrue(Content.All is IQueryable<Content>);

            var allContent = Content.All;
            var allContent2 = Content.All;
            Assert.AreNotSame(allContent, allContent2);

            var asQueryable = allContent.AsQueryable();
            Assert.AreSame(allContent, asQueryable);
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_IdEquality()
        {
            Test(() =>
            {
                var expected = "Id:42";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Id == 42)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Id == 42 select c));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_IdRange_Order()
        {
            Test(() =>
            {
                var expected = "Id:<4 .SORT:Id";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Id < 4).OrderBy(c => c.Id)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Id < 4 orderby c.Id select c));

                expected = "Id:<=4 .SORT:Id";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Id <= 4).OrderBy(c => c.Id)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Id <= 4 orderby c.Id select c));

                expected = "Id:<=4 .REVERSESORT:Id";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Id <= 4).OrderByDescending(c => c.Id)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Id <= 4 orderby c.Id descending select c));

                expected = "+Id:>1 +Id:<=4 .SORT:Id";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Id <= 4 && c.Id > 1).OrderBy(c => c.Id)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Id <= 4 && c.Id > 1 orderby c.Id select c));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_SingleNegativeTerm()
        {
            Test(() =>
            {
                Assert.AreEqual("-Id:42 +Id:>0", GetQueryString(Content.All.Where(c => c.Id != 42)));
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_StartsWithEndsWithContains()
        {
            Test(() =>
            {
                Assert.AreEqual("Name:car*", GetQueryString(Content.All.Where(c => c.Name.StartsWith("Car"))));
                Assert.AreEqual("Name:*r2", GetQueryString(Content.All.Where(c => c.Name.EndsWith("r2"))));
                Assert.AreEqual("Name:*ro*", GetQueryString(Content.All.Where(c => c.Name.Contains("ro"))));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_CaseSensitivity()
        {
            Test(() =>
            {
                var expected = "Name:admin .SORT:Id";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Name == "admin").OrderBy(c => c.Id)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Name == "admin" orderby c.Id select c));

                expected = "Name:admin .SORT:Id";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.Name == "Admin").OrderBy(c => c.Id)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where c.Name == "Admin" orderby c.Id select c));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_EmptyString()
        {
            Test(() =>
            {
                Assert.AreEqual("DisplayName:''", GetQueryString(Content.All.Where(c => c.DisplayName == "")));
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NullString()
        {
            Test(() =>
            {
                var expected = "DisplayName:''";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => (string)c["DisplayName"] == null)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All where (string)c["DisplayName"] == null select c));
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_DateTime()
        {
            Test(() =>
            {
                var d0 = DateTime.UtcNow.AddDays(-2);

                // ModificationDate:<'2345-06-07 08:09:10.0000'
                var q1 = GetQueryString(Content.All.Where(c => c.ModificationDate < DateTime.UtcNow.AddDays(-2)));
                q1 = q1.Substring(19, 24);
                var d1 = DateTime.Parse(q1);

                Assert.IsTrue(d1 - d0 < TimeSpan.FromSeconds(1));

                var q2 = GetQueryString(from c in Content.All where c.ModificationDate < DateTime.UtcNow.AddDays(-2) select c);
                q2 = q2.Substring(19, 24);
                var d2 = DateTime.Parse(q2);

                Assert.IsTrue(d2 - d0 < TimeSpan.FromSeconds(1));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NegativeTerm()
        {
            Test(() =>
            {
                Assert.AreEqual("-Id:2 +Id:<=4", GetQueryString(Content.All.Where(c => c.Id <= 4 && c.Id != 2)));
                Assert.AreEqual("-Id:2 +Id:>0", GetQueryString(Content.All.Where(c => c.Id != 2)));
                Assert.AreEqual("-Id:2 +Id:>0", GetQueryString(Content.All.Where(c => c.Id > 0 && c.Id != 2)));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_Bool()
        {
            Test(() =>
            {
                var q = GetQueryString(Content.All.Where(c => c.IsFolder == true));
                Assert.AreEqual("IsFolder:yes", q);

                q = GetQueryString(Content.All.Where(c => c.IsFolder == false));
                Assert.AreEqual("IsFolder:no", q);

                q = GetQueryString(Content.All.Where(c => c.IsFolder != true));
                Assert.AreEqual("IsFolder:no", q);

                q = GetQueryString(Content.All.Where(c => c.IsFolder));
                Assert.AreEqual("IsFolder:yes", q);

                q = GetQueryString(Content.All.Where(c => (bool) c["Hidden"]));
                Assert.AreEqual("Hidden:yes", q);

                q = GetQueryString(Content.All.OfType<Workspace>().Where(c => c.IsWallContainer));
                Assert.AreEqual("+TypeIs:workspace +IsWallContainer:yes", q);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_Bool_Negation()
        {
            Test(() =>
            {
                var q = GetQueryString(Content.All.Where(c => !c.IsFolder));
                Assert.AreEqual("IsFolder:no", q);

                q = GetQueryString(Content.All.Where(c => c.IsFolder != true));
                Assert.AreEqual("IsFolder:no", q);

                // ReSharper disable once NegativeEqualityExpression
                q = GetQueryString(Content.All.Where(c => !(c.IsFolder == true)));
                Assert.AreEqual("IsFolder:no", q);

                q = GetQueryString(Content.All.Where(c => !(bool) c["Hidden"]));
                Assert.AreEqual("Hidden:no", q);

                q = GetQueryString(Content.All.OfType<Workspace>().Where(c => !c.IsWallContainer));
                Assert.AreEqual("+TypeIs:workspace +IsWallContainer:no", q);

                q =
                    GetQueryString(
                        Content.All.Where(c => !((Workspace) c.ContentHandler).IsWallContainer));
                Assert.AreEqual("IsWallContainer:no", q);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_Negation()
        {
            Test(() =>
            {
                var q = GetQueryString(Content.All.Where(c => c.Index != 42));
                Assert.AreEqual("-Index:42 +Id:>0", q);

                // ReSharper disable once NegativeEqualityExpression
                q = GetQueryString(Content.All.Where(c => !(c.Index == 42)));
                Assert.AreEqual("-Index:42 +Id:>0", q);

                // ReSharper disable once NegativeEqualityExpression
                q = GetQueryString(Content.All.Where(c => !(!(c.Index == 42) && !c.IsFolder)));
                Assert.AreEqual("-(+IsFolder:no -Index:42) +Id:>0", q);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_SingleReference()
        {
            Test(() =>
            {
                /**/ContentTypeInstaller.InstallContentType(RefTestNode.ContentTypeDefinition);

                var root = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                root.Save();

                var mother1 = Content.CreateNew("RefTestNode", root, Guid.NewGuid().ToString()).ContentHandler;
                SaveNode(mother1);

                Assert.AreEqual(
                    $"+TypeIs:reftestnode +Mother:{mother1.Id}",
                    GetQueryString(Content.All.OfType<RefTestNode>().Where(c => c.Mother == mother1)));
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_MultiReference()
        {
            Test(() =>
            {
                /**/ContentTypeInstaller.InstallContentType(RefTestNode.ContentTypeDefinition);

                //var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                //root.Save();
                var node = Content.CreateNew("Folder", Repository.Root, Guid.NewGuid().ToString()).ContentHandler;
                SaveNode(node);

                Assert.AreEqual(
                    $"Neighbors:{node.Id}",
                    GetQueryString(Content.All.Where(c => ((RefTestNode) c.ContentHandler).Neighbors.Contains(node))));
            });
        }

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_EmptyReference()
        //{
        //    Content[] result;
        //    string expected, actual;
        //    QueryResult qresult;

        //    var mother1 = Node.LoadNode(TestRoot2.Path + "/Mother1");
        //    var mother2 = Node.LoadNode(TestRoot2.Path + "/Mother2");
        //    var child1 = Node.LoadNode(TestRoot2.Path + "/Child1");
        //    var child2 = Node.LoadNode(TestRoot2.Path + "/Child2");
        //    var child3 = Node.LoadNode(TestRoot2.Path + "/Child3");

        //    qresult = ContentQuery.Query(String.Concat("+Mother:null +InTree:", TestRoot2.Path, " .AUTOFILTERS:OFF"));
        //    result = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot2) && ((RefTestNode)c.ContentHandler).Mother == null).OrderBy(c => c.Name).ToArray();
        //    Assert.IsTrue(result.Length == 3, String.Format("#5: count is {0}, expected: 3", result.Length));
        //    expected = String.Concat(child3.Id, ", ", mother1.Id, ", ", mother2.Id);
        //    actual = String.Join(", ", result.Select(x => x.Id));
        //    Assert.IsTrue(expected == actual, String.Format("#6: actual is {0}, expected: {1}", actual, expected));

        //    qresult = ContentQuery.Query(String.Concat("-Mother:null +InTree:", TestRoot2.Path, " +TypeIs:reftestnode .AUTOFILTERS:OFF"));
        //    result = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot2) && ((RefTestNode)c.ContentHandler).Mother != null && c.ContentHandler is RefTestNode).OrderBy(c => c.Name).ToArray();
        //    Assert.IsTrue(result.Length == 2, String.Format("#5: count is {0}, expected: 2", result.Length));
        //    expected = String.Concat(child1.Id, ", ", child2.Id);
        //    actual = String.Join(", ", result.Select(x => x.Id));
        //    Assert.IsTrue(expected == actual, String.Format("#6: actual is {0}, expected: {1}", actual, expected));
        //}

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_Children()
        //{
        //    var folderName = "Linq_Children_test";
        //    var folder = Folder.Load<Folder>(RepositoryPath.Combine(TestRoot.Path, folderName));
        //    if (folder == null)
        //    {
        //        folder = new Folder(TestRoot) { Name = folderName };
        //        folder.Save();
        //        for (int i = 0; i < 4; i++)
        //        {
        //            var content = Content.CreateNew("Car", folder, "Car" + i);
        //            content.ContentHandler.Index = i;
        //            content.Save();
        //        }
        //    }
        //    var folderContent = Content.Create(folder);

        //    var enumerable = folderContent.Children.DisableAutofilters().Where(c => c.Index < 2).OrderBy(c => c.Name);
        //    var result = enumerable.ToArray();

        //    var paths = result.Select(c => c.Path).ToArray();

        //    Assert.IsTrue(result.Length == 2, String.Format("result.Length is {0}, expected: 2.", result.Length));
        //    Assert.IsTrue(result[0].Name == "Car0", String.Format("result[0].Name is {0}, expected: 'Car0'.", result[0].Name));
        //    Assert.IsTrue(result[1].Name == "Car1", String.Format("result[1].Name is {0}, expected: 'Car1'.", result[1].Name));

        //}

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_Children_Count()
        //{
        //    if (ContentQuery.Query(".AUTOFILTERS:OFF .COUNTONLY Infolder:" + TestRoot.Path).Count == 0)
        //        for (int i = 0; i < 3; i++)
        //            Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString()).Save();
        //    var r = ContentQuery.Query(".AUTOFILTERS:OFF InFolder:" + TestRoot.Path);
        //    var expected = r.Count;
        //    var content = Content.Create(TestRoot);
        //    var actual = content.Children.DisableAutofilters().Count();
        //    Assert.AreEqual(expected, actual);
        //}

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_InFolder()
        {
            Test(() =>
            {
                var root = Content.CreateNew("Folder", Repository.Root, "Folder1").ContentHandler;
                SaveNode(root);

                var expected = "InFolder:/root/folder1/cars";
                Assert.AreEqual(expected, GetQueryString(Content.All.Where(c => c.InFolder(root.Path + "/Cars"))));
                Assert.AreEqual(expected, GetQueryString(from x in Content.All where x.InFolder(root.Path + "/Cars") select x));
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_InTree()
        {
            Test(() =>
            {
                Assert.AreEqual("InTree:" + Repository.ImsFolder.Path.ToLowerInvariant(), GetQueryString(Content.All.Where(c => c.InTree(Repository.ImsFolder))));
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        [SuppressMessage("ReSharper", "UseIsOperator.1")]
        [SuppressMessage("ReSharper", "UseMethodIsInstanceOfType")]
        public void Linq_TypeFilter_Strong()
        {
            Test(() =>
            {
                var name = Guid.NewGuid().ToString();
                var root = Content.CreateNew("Folder", Repository.Root, name).ContentHandler;
                SaveNode(root);

                //-- type that handles one content type
                var expected = $"+TypeIs:group +InTree:'/root/{name}' .SORT:Name .AUTOFILTERS:OFF";
                Assert.AreEqual(expected, GetQueryString(Content.All.DisableAutofilters().Where(c => c.InTree(root) && c.ContentHandler is Group).OrderBy(c => c.Name)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All.DisableAutofilters() where c.InTree(root) && c.ContentHandler is Group orderby c.Name select c));

                Assert.AreEqual(expected, GetQueryString(Content.All.DisableAutofilters().Where(c => c.InTree(root) && typeof(Group).IsAssignableFrom(c.ContentHandler.GetType())).OrderBy(c => c.Name)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All.DisableAutofilters() where c.InTree(root) && typeof(Group).IsAssignableFrom(c.ContentHandler.GetType()) orderby c.Name select c));

                //-- type that handles more than one content type
                expected = $"+TypeIs:genericcontent +InTree:'/root/{name}/cars' .SORT:Name .AUTOFILTERS:OFF";
                Assert.AreEqual(expected, GetQueryString(Content.All.DisableAutofilters().Where(c => c.InTree(root.Path + "/Cars") && c.ContentHandler is GenericContent).OrderBy(c => c.Name)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All.DisableAutofilters() where c.InTree(root.Path + "/Cars") && c.ContentHandler is GenericContent orderby c.Name select c));

                Assert.AreEqual(expected, GetQueryString(Content.All.DisableAutofilters().Where(c => c.InTree(root.Path + "/Cars") && typeof(GenericContent).IsAssignableFrom(c.ContentHandler.GetType())).OrderBy(c => c.Name)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All.DisableAutofilters() where c.InTree(root.Path + "/Cars") && typeof(GenericContent).IsAssignableFrom(c.ContentHandler.GetType()) orderby c.Name select c));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_TypeFilter_String()
        {
            Test(() =>
            {
                Assert.AreEqual("+Id:>0 +Type:group", GetQueryString(Content.All.Where(c => c.ContentType.Name == "Group" && c.Id > 0)));
                Assert.AreEqual("Type:group", GetQueryString(Content.All.Where(c => c.ContentType == ContentType.GetByName("Group"))));
                Assert.AreEqual("Type:car", GetQueryString(Content.All.Where(c => c.Type("Car"))));
                Assert.AreEqual("TypeIs:car", GetQueryString(Content.All.Where(c => c.TypeIs("Car"))));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_ConditionalOperator()
        {
            Test(() =>
            {
                // First operand of the conditional operator is a constant
                var bool1 = true;
                Assert.AreEqual("DisplayName:car", GetQueryString(Content.All.Where(c => bool1 ? c.DisplayName == "Car" : c.Index == 42)));

                var bool2 = false;
                Assert.AreEqual("Index:42", GetQueryString(Content.All.Where(c => bool2 ? c.DisplayName == "Car" : c.Index == 42)));

                // First operand is not a constant
                Assert.AreEqual("(+Index:85 -Type:car) (+DisplayName:ferrari +Type:car)",
                    GetQueryString(Content.All.Where(c => c.Type("Car") ? c.DisplayName == "Ferrari" : c.Index == 85)));

                Assert.AreEqual("(+Index:85 -Type:car) (+DisplayName:'my nice ferrari' +Type:car)",
                    GetQueryString(Content.All.Where(c => c.Type("Car") ? c.DisplayName == "My nice Ferrari" : c.Index == 85)));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_FieldWithIndexer()
        {
            Test(() =>
            {
                var root = Content.CreateNew("Folder", Repository.Root, "Folder1").ContentHandler;
                SaveNode(root);

                var expected = "+DisplayName:porsche +InTree:/root/folder1 .SORT:Name .AUTOFILTERS:OFF";
                Assert.AreEqual(expected, GetQueryString(
                    Content.All.DisableAutofilters()
                        .Where(c => c.InTree(root) && (string) c["DisplayName"] == "Porsche")
                        .OrderBy(c => c.Name)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All.DisableAutofilters()
                                                         where c.InTree(root) && (string) c["DisplayName"] == "Porsche"
                                                         orderby c.Name
                                                         select c));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_Boolean()
        {
            Test(() =>
            {
                var root = Content.CreateNew("Folder", Repository.Root, "Folder1").ContentHandler;
                SaveNode(root);

                var expected = "+((+DisplayName:ferrari +Index:4) (+DisplayName:porsche +Index:2)) +InTree:/root/folder1 .SORT:Name .AUTOFILTERS:OFF";
                Assert.AreEqual(expected, GetQueryString(Content.All.DisableAutofilters().Where(c => c.InTree(root) && (((int)c["Index"] == 2 && (string)c["DisplayName"] == "Porsche") || ((int)c["Index"] == 4 && (string)c["DisplayName"] == "Ferrari"))).OrderBy(c => c.Name)));
                Assert.AreEqual(expected, GetQueryString(from c in Content.All.DisableAutofilters() where c.InTree(root) && (((int)c["Index"] == 2 && (string)c["DisplayName"] == "Porsche") || ((int)c["Index"] == 4 && (string)c["DisplayName"] == "Ferrari")) orderby c.Name select c));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_AndOrPrecedence()
        {
            Test(() =>
            {
                var root = Repository.Root;
                Assert.AreEqual("+(Index:3 (+Index:2 +TypeIs:group)) +InTree:/root",
                    GetQueryString(
                        Content.All.Where(
                            c => c.InTree(root) && (c.ContentHandler is Group && c.Index == 2 || c.Index == 3))));
                Assert.AreEqual("+((+TypeIs:group +Index:3) Index:2) +InTree:/root",
                    GetQueryString(
                        Content.All.Where(
                            c => c.InTree(root) && (c.Index == 2 || c.Index == 3 && c.ContentHandler is Group))));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_OrderBy()
        {
            Test(() =>
            {
                var root = Content.CreateNew("Folder", Repository.Root, "Folder1").ContentHandler;
                SaveNode(root);

                Assert.AreEqual("+TypeIs:folder +InTree:/root/folder1 .SORT:Index .REVERSESORT:Name .AUTOFILTERS:OFF", GetQueryString(
                    Content.All.DisableAutofilters()
                        .Where(c => c.InTree(root) && c.TypeIs("Folder"))
                        .OrderBy(c => c.Index)
                        .ThenByDescending(c => c.Name)));
            });
        }

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_SelectSimple()
        //{
        //    var names = String.Join(", ",
        //        Content.All
        //        .Where(c => c.Id < 10).OrderBy(c => c.Name)
        //        .AsEnumerable()
        //        .Select(c => c.Name)
        //        );
        //    Assert.AreEqual("Admin, Administrators, BuiltIn, Everyone, IMS, Owners, Portal, Root, Visitor", names);
        //}
        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_Select_WithoutAsEnumerable()
        //{
        //    try
        //    {
        //        var x = String.Join(", ", Content.All.Where(c => c.Id < 10).OrderBy(c => c.Name).Select(c => c.Name));
        //        Assert.Fail("An error must be thrown with exclamation: Use AsEnumerable ...");
        //    }
        //    catch (NotSupportedException e)
        //    {
        //        if (!e.Message.Contains("AsEnumerable"))
        //            Assert.Fail("Exception message does not contain 'AsEnumerable'");
        //    }
        //}
        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_SelectNew()
        //{
        //    var x = Content.All.Where(c => c.Id < 10).OrderBy(c => c.Id).AsEnumerable().Select(c => new { Id = c.Id, c.Name }).ToArray();
        //    var y = String.Join(", ", x.Select(a => String.Concat(a.Id, ", ", a.Name)));
        //    Assert.AreEqual("1, Admin, 2, Root, 3, IMS, 4, BuiltIn, 5, Portal, 6, Visitor, 7, Administrators, 8, Everyone, 9, Owners", y);
        //}

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_OfType()
        {
            Test(() =>
            {
                Assert.AreEqual("TypeIs:contenttype .AUTOFILTERS:OFF",
                    GetQueryString(Content.All.DisableAutofilters().OfType<ContentType>()));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_TakeSkip()
        {
            Test(() =>
            {
                Assert.AreEqual("IsFolder:yes .TOP:5 .SKIP:8", GetQueryString(Content.All.Where(c => c.IsFolder == true).Skip(8).Take(5)));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_CombiningQueries()
        {
            Test(() =>
            {
                var childrenDef = new ChildrenDefinition
                {
                    PathUsage = PathUsageMode.InFolderOr,
                    ContentQuery = "Id:>42",
                    EnableAutofilters = FilterStatus.Disabled,
                    Skip = 18,
                    Top = 15
                };
                var expr = Content.All.Where(c => c.IsFolder == true).Skip(8).Take(5).Expression;
                var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                var expected = "(+IsFolder:yes +Id:>42) InFolder:/root/fakepath .TOP:15 .SKIP:18 .AUTOFILTERS:OFF";
                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_ReplaceTemplates()
        {
            Test(() =>
            {
                var childrenDef = new ChildrenDefinition
                {
                    PathUsage = PathUsageMode.InFolderOr,
                    ContentQuery = "CreationDate:<@@CurrentDay@@"
                };
                var expr = Content.All.Where(c => c.IsFolder == true).Expression;
                var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                var expected = $"(+IsFolder:yes +CreationDate:<'{DateTime.UtcNow.Date:yyyy-MM-dd} 00:00:00.0000') InFolder:/root/fakepath";

                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_API()
        {
            Test(() =>
            {
                ContentSet<Content>[] contentSets =
                {
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2).Skip(5),
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2).Take(5),
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2).OrderBy(c => c.Name),
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2).OrderByDescending(c => c.Name),
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2).OrderBy(c => c.Name).ThenBy(c => c.Id),
                    (ContentSet<Content>) Content.All.Where(c => c.Id == 2).OrderBy(c => c.Name).ThenByDescending(c => c.Id),
                    (ContentSet<Content>) Content.All.EnableAutofilters().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.DisableAutofilters().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.EnableLifespan().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.DisableLifespan().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.EnableAutofilters().EnableLifespan().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.EnableAutofilters().DisableLifespan().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.DisableAutofilters().EnableLifespan().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.DisableAutofilters().DisableLifespan().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.EnableLifespan().EnableAutofilters().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.DisableLifespan().EnableAutofilters().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.EnableLifespan().DisableAutofilters().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.DisableLifespan().DisableAutofilters().Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.SetExecutionMode(QueryExecutionMode.Default).Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.SetExecutionMode(QueryExecutionMode.Strict).Where(c => c.Id == 2),
                    (ContentSet<Content>) Content.All.SetExecutionMode(QueryExecutionMode.Quick).Where(c => c.Id == 2),
                };
                var queries = new string[contentSets.Length];
                for (var i = 0; i < contentSets.Length; i++)
                    queries[i] =
                        SnExpression.BuildQuery(contentSets[i].Expression, typeof(Content), contentSets[i].ContextPath,
                            contentSets[i].ChildrenDefinition).ToString();

                var expected = @"Id:2
Id:2 .SKIP:5
Id:2 .TOP:5
Id:2 .SORT:Name
Id:2 .REVERSESORT:Name
Id:2 .SORT:Name .SORT:Id
Id:2 .SORT:Name .REVERSESORT:Id
Id:2
Id:2 .AUTOFILTERS:OFF
Id:2 .LIFESPAN:ON
Id:2
Id:2 .LIFESPAN:ON
Id:2
Id:2 .AUTOFILTERS:OFF .LIFESPAN:ON
Id:2 .AUTOFILTERS:OFF
Id:2 .LIFESPAN:ON
Id:2
Id:2 .AUTOFILTERS:OFF .LIFESPAN:ON
Id:2 .AUTOFILTERS:OFF
Id:2
Id:2
Id:2 .QUICK";

                var actual = String.Join("\r\n", queries);
                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_ExecutionMode_Quick()
        {
            Test(() =>
            {
                ContentSet<Content>[] contentSets =
                {
                    (ContentSet<Content>) Content.All.SetExecutionMode(QueryExecutionMode.Default).Where(c => c.Id < 42),
                    (ContentSet<Content>) Content.All.SetExecutionMode(QueryExecutionMode.Strict).Where(c => c.Id < 42),
                    (ContentSet<Content>) Content.All.SetExecutionMode(QueryExecutionMode.Quick).Where(c => c.Id < 42),
                };
                var queries = new string[contentSets.Length];
                for (var i = 0; i < contentSets.Length; i++)
                    queries[i] =
                        SnExpression.BuildQuery(contentSets[i].Expression, typeof(Content), contentSets[i].ContextPath,
                            contentSets[i].ChildrenDefinition).ToString();

                var expected = @"Id:<42
Id:<42
Id:<42 .QUICK";
                var actual = String.Join("\r\n", queries);
                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        [SuppressMessage("ReSharper", "UnusedVariable")]
        public void Linq_Error_NotConstants()
        {
            Test(() =>
            {
                try { var x = Content.All.Where(c => c.DisplayName.StartsWith(c.Name)).ToArray(); Assert.Fail("#1 Exception wasn't thrown"); } catch (NotSupportedException) { }
                try { var x = Content.All.Where(c => c.DisplayName.EndsWith(c.Name)).ToArray(); Assert.Fail("#2 Exception wasn't thrown"); } catch (NotSupportedException) { }
                try { var x = Content.All.Where(c => c.DisplayName.Contains(c.Name)).ToArray(); Assert.Fail("#3 Exception wasn't thrown"); } catch (NotSupportedException) { }

                try { var x = Content.All.Where(c => c.Type(c.DisplayName)).ToArray(); Assert.Fail("#4 Exception wasn't thrown"); } catch (NotSupportedException) { }
                try { var x = Content.All.Where(c => c.TypeIs(c.DisplayName)).ToArray(); Assert.Fail("#5 Exception wasn't thrown"); } catch (NotSupportedException) { }
                try { var x = Content.All.Where(c => c.InFolder(c.WorkspacePath)).ToArray(); Assert.Fail("#6 Exception wasn't thrown"); } catch (NotSupportedException) { }
                try { var x = Content.All.Where(c => c.InTree(c.WorkspacePath)).ToArray(); Assert.Fail("#7 Exception wasn't thrown"); } catch (NotSupportedException) { }
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_All()
        {
            Test(() =>
            {
                var expectedNonSystemCount = CreateSafeContentQuery(
                    "InTree:/Root .COUNTONLY .AUTOFILTERS:ON", QuerySettings.Default).Execute().Count;
                var expectedAllCount = CreateSafeContentQuery(
                    "InTree:/Root .COUNTONLY .AUTOFILTERS:OFF", QuerySettings.Default).Execute().Count;

                // ---------------- TEST CASE 1: All non-system contents in ad-hoc order
                var contentArray = Content.All.ToArray();
                // ASSERT
                Assert.AreEqual(expectedNonSystemCount, contentArray.Length);
                contentArray = contentArray.OrderBy(c => c.Id).ToArray();
                for (int i = 1; i < contentArray.Length; i++)
                    Assert.IsTrue(contentArray[i - 1].Id < contentArray[i].Id);

                // ---------------- TEST CASE 2: All contents in ad-hoc order
                contentArray = Content.All.DisableAutofilters().ToArray();
                // ASSERT
                Assert.AreEqual(expectedAllCount, contentArray.Length);
                contentArray = contentArray.OrderBy(c => c.Id).ToArray();
                for (int i = 1; i < contentArray.Length; i++)
                    Assert.IsTrue(contentArray[i - 1].Id < contentArray[i].Id);

                // ---------------- TEST CASE 3: All non-system contents + sort
                contentArray = Content.All.OrderByDescending(c => c.Id).ToArray();
                // ASSERT
                Assert.AreEqual(expectedNonSystemCount, contentArray.Length);
                for (int i = 1; i < contentArray.Length; i++)
                    Assert.IsTrue(contentArray[i - 1].Id > contentArray[i].Id);

                // ---------------- TEST CASE 4: All contents + sort
                contentArray = Content.All.DisableAutofilters().OrderByDescending(c => c.Id).ToArray();
                // ASSERT
                Assert.AreEqual(expectedAllCount, contentArray.Length);
                for (int i = 1; i < contentArray.Length; i++)
                    Assert.IsTrue(contentArray[i - 1].Id > contentArray[i].Id);
            });
        }

        /* ------------------------------------------------------------------------------------------- */

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_OptimizeBooleans()
        {
            Test(() =>
            {
                var childrenDef = new ChildrenDefinition {PathUsage = PathUsageMode.InFolderAnd};
                //var expr = Content.All.Where(c => c.Path != "/Root/A" && c.Path != "/Root/B" && c.Path != "/Root/C" && c.Type("Folder") && c.InFolder(folder)).Expression;
                var expr =
                    Content.All.Where(c => c.Name != "A" && c.Name != "B" && c.Name != "C" && c.TypeIs("Folder"))
                        .Expression;
                var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                var expected = "+(+TypeIs:folder -Name:c -Name:b -Name:a) +InFolder:/root/fakepath";
                Assert.AreEqual(expected, actual);
            });
        }

        /* ------------------------------------------------------------------------------------------- */

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_AspectField()
        {
            var aspectName = "Linq_AspectField_Aspect1";
            var fieldName = "Field1";
            var fieldValue = "fieldvalue";
            var aspectDefinition =
                $@"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
  <Fields>
    <AspectField name='{fieldName}' type='ShortText' />
  </Fields>
</AspectDefinition>";

            Test(() =>
            {
                var aspect = new Aspect(Repository.AspectsFolder)
                {
                    Name = aspectName,
                    AspectDefinition = aspectDefinition
                };
                aspect.Save();

                Assert.AreEqual($"{aspectName}.{ fieldName}:{ fieldValue}",
                GetQueryString(Content.All.OfType<Content>().Where(c => (string)c[$"{aspectName}.{fieldName}"] == fieldValue)));
            });
        }

        /* ========================================================================================== Linq_NotSupported_ */

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Select_New()
        {
            AsEnumerableError("Select", () =>
            {
                var queryable = Content.All.Where(c => c.Id < 10)
                    .Select(c => new { c.Id, c.Path });
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Select_Content()
        {
            AsEnumerableError("Select", () =>
            {
                var unused = Content.All.Where(c => false)
                    .Select(c => c.ContentHandler).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_SelectMany()
        {
            AsEnumerableError("SelectMany", () =>
            {
                var unused = Content.All.Where(c => false)
                    .SelectMany(c => c.Versions).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_SelectManySelect()
        {
            AsEnumerableError("SelectMany", () =>
            {
                var unused = Content.All.Where(c => true)
                    .SelectMany(c => c.Versions)
                    .Select(n => Content.Create(n)).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Min()
        {
            AsEnumerableError("Min", () =>
            {
                var unused = Content.All.Where(c => true).Min(c => c.Id);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Max()
        {
            AsEnumerableError("Max", () =>
            {
                var unused = Content.All.Where(c => true).Max(c => c.Id);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Sum()
        {
            AsEnumerableError("Sum", () =>
            {
                var unused = Content.All.Where(c => true).Sum(c => c.Id);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Average()
        {
            AsEnumerableError("Average", () =>
            {
                var unused = Content.All.Where(c => true).Average(c => c.Id);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_SkipWhile()
        {
            AsEnumerableError("SkipWhile", () =>
            {
                var unused = Content.All.Where(c => true).SkipWhile(c => false).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_TakeWhile()
        {
            AsEnumerableError("TakeWhile", () =>
            {
                var unused = Content.All.Where(c => true).TakeWhile(c => false).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Join_NonameOutput()
        {
            AsEnumerableError("Join", () =>
            {
                var unused = Content.All.Where(c => c.Id < 10000)
                    .Join(
                        Content.All,
                        user => user.Id,
                        doc => doc.ContentHandler.ModifiedBy.Id,
                        (user, doc) => new { UserId = user.Id, DocId = doc.Id })
                    .Where(item => item.UserId == 42);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Join_ContentOutput()
        {
            AsEnumerableError("Join", () =>
            {
                var unused = Content.All.Where(c => false)
                    .Join(
                        Content.All,
                        user => user.Id,
                        doc => doc.ContentHandler.ModifiedBy.Id,
                        (user, doc) => user)
                    .Where(user => user.Id == 42)
                    .ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Aggregate()
        {
            AsEnumerableError("Aggregate", () =>
            {
                var unused = Content.All.Where(c => false).Aggregate(0, (a, b) => a + b.Id);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Cast()
        {
            AsEnumerableError("Cast", () =>
            {
                var unused = Content.All.Where(c => false).Cast<Content>().ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Distinct()
        {
            AsEnumerableError("Distinct", () =>
            {
                var unused = Content.All.Where(c => false).Distinct().ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Concat()
        {
            AsEnumerableError("Concat", () =>
            {
                var unused = Content.All.Where(c => false).Concat(new Content[0]).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Union()
        {
            AsEnumerableError("Union", () =>
            {
                var unused = Content.All.Where(c => false).Union(new Content[0]).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Intersect()
        {
            AsEnumerableError("Intersect", () =>
            {
                var unused = Content.All.Where(c => false).Intersect(new Content[0]).ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Except()
        {
            AsEnumerableError("Except", () =>
            {
                var unused = Content.All.Where(c => false).Except(new Content[0]).ToArray();
            });
        }


        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_GroupBy()
        {
            AsEnumerableError("GroupBy", () =>
            {
                var unused = Content.All.Where(c => false).GroupBy(c => c);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_GroupJoin()
        {
            AsEnumerableError("GroupJoin", () =>
            {
                var unused = Content.All.Where(c => false)
                    .GroupJoin(new Content[0], a => a, b => b, (c, d) => new {A = c, B = d});
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_All()
        {
            AsEnumerableError("All", () =>
            {
                var unused = Content.All.Where(c => false).All(c => true);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Reverse()
        {
            AsEnumerableError("Reverse", () =>
            {
                var unused = Content.All.Where(c => false).Reverse().ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_SequenceEqual()
        {
            AsEnumerableError("SequenceEqual", () =>
            {
                var unused = Content.All.Where(c => false).SequenceEqual(new Content[0]);
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_DefaultIfEmpty()
        {
            AsEnumerableError("DefaultIfEmpty", () =>
            {
                var unused = Content.All.Where(c => false).DefaultIfEmpty().ToArray();
            });
        }
        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_DefaultIfEmptyDefault()
        {
            AsEnumerableError("DefaultIfEmpty", () =>
            {
                Content defaultContent = null;
                var unused = Content.All.Where(c => false).DefaultIfEmpty(defaultContent).ToArray();
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_NotSupported_Zip()
        {
            AsEnumerableError("Zip", () =>
            {
                var unused = Content.All.Where(c => false).Zip(new Content[0], (a, b) => a).ToArray();
            });
        }


        /* ========================================================================================== LinqEx_ */
        /* When the framework calls Provider.Execute */

        [TestMethod, TestCategory("IR, LINQ")]
        public void LinqEx_FirstLastSingle()
        {
            Test(() =>
            {
                // ======== First()
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).First().Id);
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderByDescending(c => c.Id).First(c => c.Id < 6).Id);
                // empty
                InvalidOperationTest(() => { Content.All.Where(c => c.Id < 0).First(); });
                InvalidOperationTest(() => { Content.All.First(c => c.Id < 0); });

                // ======== FirstOrDefault()
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).FirstOrDefault().Id);
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderByDescending(c => c.Id).FirstOrDefault(c => c.Id < 6).Id);
                // empty
                Assert.IsNull(Content.All.Where(c => c.Id < 0).FirstOrDefault());
                Assert.IsNull(Content.All.Where(c => c.Id < 0).FirstOrDefault(c => c.Id < 4));

                // ======== Last()
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 6).OrderBy(c => c.Id).Last().Id);
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).Last(c => c.Id < 6).Id);
                // empty
                InvalidOperationTest(() => { Content.All.Where(c => c.Id < 0).Last(); });
                InvalidOperationTest(() => { Content.All.Last(c => c.Id < 0); });

                // ======== LastOrDefault()
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 6).OrderBy(c => c.Id).LastOrDefault().Id);
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).LastOrDefault(c => c.Id < 6).Id);
                // empty
                Assert.IsNull(Content.All.Where(c => c.Id < 0).LastOrDefault());
                Assert.IsNull(Content.All.Where(c => c.Id < 0).LastOrDefault(c => c.Id < 4));


                // ======== Single()
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id == 5).Single().Id);
                Assert.AreEqual(5, Content.All.DisableAutofilters().Single(c => c.Id == 5).Id);
                // empty
                InvalidOperationTest(() => { Content.All.Where(c => c.Id < 0).Single(); });
                InvalidOperationTest(() => { Content.All.Single(c => c.Id < 0); });
                // more than one
                InvalidOperationTest(() => { Content.All.Where(c => c.Id < 6).Single(); });
                InvalidOperationTest(() => { Content.All.Single(c => c.Id < 6); });

                // ======== SingleOrDefault()
                Assert.AreEqual(5, Content.All.DisableAutofilters().Where(c => c.Id == 5).SingleOrDefault().Id);
                Assert.AreEqual(5, Content.All.DisableAutofilters().SingleOrDefault(c => c.Id == 5).Id);
                // empty
                Assert.IsNull(Content.All.Where(c => c.Id < 0).SingleOrDefault());
                Assert.IsNull(Content.All.SingleOrDefault(c => c.Id < 0));
                // more than one
                InvalidOperationTest(() => { Content.All.Where(c => c.Id < 6).SingleOrDefault(); });
                InvalidOperationTest(() => { Content.All.SingleOrDefault(c => c.Id < 6); });
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void LinqEx_ElementAt()
        {
            Test(() =>
            {
                var x = 2;
                // ======== ElementAt()
                Assert.AreEqual(6, Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).ElementAt(3 + x).Id);
                // less
                ArgumentOutOfRangeTest(() => { Content.All.DisableAutofilters().Where(c => c.Id < 3).OrderBy(c => c.Id).ElementAt(5); });
                // empty
                ArgumentOutOfRangeTest(() => { Content.All.DisableAutofilters().Where(c => c.Id < 0).OrderBy(c => c.Id).ElementAt(5); });

                // ======== ElementAtOrDefault()
                Assert.AreEqual(6, Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).ElementAtOrDefault(5).Id);
                // less
                Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Id < 3).OrderBy(c => c.Id).ElementAtOrDefault(5));
                // empty
                Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Id < 0).OrderBy(c => c.Id).ElementAtOrDefault(5));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void LinqEx_Any()
        {
            Test(() =>
            {
                Assert.IsFalse(Content.All.DisableAutofilters().Where(c => c.Id == 0).Any());
                Assert.IsTrue(Content.All.DisableAutofilters().Where(c => c.Id == 1).Any());
                Assert.IsTrue(Content.All.DisableAutofilters().Where(c => c.Id > 0).Any());

                Assert.IsFalse(Content.All.DisableAutofilters().Any(c => c.Id == 0));
                Assert.IsTrue(Content.All.DisableAutofilters().Any(c => c.Id == 1));
                Assert.IsTrue(Content.All.DisableAutofilters().Any(c => c.Id > 0));
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void LinqEx_CountOnly()
        {
            Test(() =>
            {
                Assert.AreEqual(9, Content.All.DisableAutofilters().Where(c => c.Id < 10).Count());
                Assert.AreEqual(9, Content.All.DisableAutofilters().Count(c => c.Id < 10));

                Assert.AreEqual(9L, Content.All.DisableAutofilters().Where(c => c.Id < 10).LongCount());
                Assert.AreEqual(9L, Content.All.DisableAutofilters().LongCount(c => c.Id < 10));
            });
        }

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void LinqEx_CountIsDeferred()
        //{
        //    string log = null;
        //    try
        //    {
        //        ContentSet<Content>.TracingEnabled = true;
        //        var count = Content.All.DisableAutofilters().Where(c => c.InFolder(TestRoot)).Count();
        //        log = ContentSet<Content>.TraceLog.ToString();
        //    }
        //    finally
        //    {
        //        ContentSet<Content>.TraceLog.Clear();
        //        ContentSet<Content>.TracingEnabled = false;
        //    }
        //    Assert.IsTrue(log.Contains(".COUNTONLY"));
        //}

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void LinqEx_OfTypeAndFirst()
        //{
        //    var email = "admin@b.c";
        //    var user = new User(User.Administrator.Parent);
        //    user.Name = "testuser129";
        //    user.Email = email;
        //    user.Save();

        //    var result = Content.All.OfType<User>().FirstOrDefault(c => c.InTree(Repository.ImsFolderPath) && c.Email == email);
        //    Assert.IsTrue(result != null);
        //}
        //[TestMethod, TestCategory("IR, LINQ")]
        //public void LinqEx_OfTypeAndWhere()
        //{
        //    string path = "/Root/IMS/BuiltIn/Portal";
        //    User user = User.Administrator;
        //    var ok = Content.All.OfType<Group>().Where(g => g.InTree(path)).AsEnumerable().Any(g => user.IsInGroup(g));
        //    Assert.IsTrue(ok);
        //}

        ///* ------------------------------------------------------------------------------------------- */ 


        //[TestMethod, TestCategory("IR, LINQ")]
        //public void LinqEx_Error_UnknownField()
        //{
        //    try
        //    {
        //        var x = Content.All.Where(c => (int)c["UnknownField"] == 42).ToArray();
        //        Assert.Fail("The expected InvalidOperationException was not thrown.");
        //    }
        //    catch (InvalidOperationException e)
        //    {
        //        var msg = e.Message;
        //        Assert.IsTrue(msg.ToLower().Contains("unknown field"), "Error message does not contain: 'unknown field'.");
        //        Assert.IsTrue(msg.Contains("UnknownField"), "Error message does not contain the field name: 'UnknownField'.");
        //    }
        //}


        /* ========================================================================================== bugz */

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_BugReproduction_OptimizeBooleans_1()
        {
            Test(() =>
            {
                // +(TypeIs:group TypeIs:user) +InFolder:/root/ims/builtin/demo/managers

                var childrenDef = new ChildrenDefinition {PathUsage = PathUsageMode.InFolderAnd};
                var expr = Content.All.Where(c => c.ContentHandler is Group || c.ContentHandler is User).Expression;
                var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                var expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
                Assert.AreEqual(expected, actual);

                childrenDef = new ChildrenDefinition {PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "Id:>0"};
                expr = Content.All.Where(c => c.ContentHandler is Group || c.ContentHandler is User).Expression;
                actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                expected = "+(TypeIs:user TypeIs:group) +Id:>0 +InFolder:/root/fakepath";
                Assert.AreEqual(expected, actual);

                childrenDef = new ChildrenDefinition {PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "TypeIs:user TypeIs:group"};
                actual = SnExpression.BuildQuery(null, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
                Assert.AreEqual(expected, actual);

                childrenDef = new ChildrenDefinition {PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "+(TypeIs:user TypeIs:group)"};
                actual = SnExpression.BuildQuery(null, typeof(Content), "/Root/FakePath", childrenDef).ToString();
                expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_BugReproduction_SnNotSupported_1()
        {
            const string SystemFolder = "SystemFolder";
            const string File = "File";
            Test(() =>
            {
                Repository.Root
                    .CreateChild("TestRoot", SystemFolder, out Node testRoot)
                    .CreateChild("Folder1", SystemFolder)
                    .CreateChild("File1", File);
                testRoot
                    .CreateChild("Folder2", SystemFolder)
                    .CreateChild("File2", File);

                var content = Content.All.DisableAutofilters()
                    .FirstOrDefault(c => c.InTree(testRoot.Path) && c.Type(File));

                Assert.IsNotNull(content);
            });
        }

        [TestMethod, TestCategory("IR, LINQ")]
        public void Linq_BugReproduction_StartsWith_1()
        {
            Test(() =>
            {
                // based on original bug report

                Assert.AreEqual("Path:/root/ims*",
                    GetQueryString(Content.All.Where(c => c.Path.StartsWith("/Root/IMS"))));

                // more test cases

                Assert.AreEqual("Path:*/mydoc",
                    GetQueryString(Content.All.Where(c => c.Path.EndsWith("/MyDoc"))));
                Assert.AreEqual("Path:*/views/*",
                    GetQueryString(Content.All.Where(c => c.Path.Contains("/Views/"))));

            });
        }


        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_Bug_StackOverflow()
        //{
        //    // There was a bug in a customer code that caused StackOverflowException but in Sense/Net this case has never been reproduced.

        //    var aspectName = "Aspect_Linq_Bug_StackOverflow";
        //    var aspect = Aspect.LoadAspectByName(aspectName);
        //    if (aspect == null)
        //    {
        //        aspect = new Aspect(Repository.AspectsFolder) { Name = aspectName };
        //        aspect.Save();
        //    }


        //    aspect = Content.All.DisableAutofilters().OfType<Aspect>().Where(x => x.Name == aspectName).FirstOrDefault();
        //    Assert.IsNotNull(aspect);

        //    aspect.ForceDelete();

        //    aspect = Content.All.DisableAutofilters().OfType<Aspect>().Where(x => x.Name == aspectName).FirstOrDefault();
        //    Assert.IsNull(aspect);
        //}

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_Bug_ChildrenBatchLoadInsteadOfOneByOne()
        //{
        //    var folderNode = new SystemFolder(TestRoot) { Name = Guid.NewGuid().ToString() };
        //    folderNode.Save();
        //    for (int i = 0; i < 10; i++)
        //        (new SystemFolder(folderNode) { Name = Guid.NewGuid().ToString() }).Save();
        //    ContentRepository.Storage.Caching.Dependency.PathDependency.FireChanged(folderNode.Path);
        //    var content = Content.Load(folderNode.Id);

        //    string log = null;
        //    using (var loggedDataProvider = new LoggedDataProvider())
        //    {
        //        var count = content.Children.DisableAutofilters().DisableLifespan().OfType<GenericContent>().AsEnumerable().Count();
        //        log = loggedDataProvider._GetLogAndClear();
        //    }

        //    var lines = log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        //    var loadNodeHead = lines.Count(l => l.StartsWith("LoadNodeHead("));
        //    var loadNodeHeads = lines.Count(l => l.StartsWith("LoadNodeHeads("));
        //    var loadNodes = lines.Count(l => l.StartsWith("LoadNodes("));

        //    Assert.AreEqual(0, loadNodeHead);
        //    Assert.AreEqual(1, loadNodeHeads);
        //    Assert.AreEqual(1, loadNodes);
        //}

        //[TestMethod, TestCategory("IR, LINQ")]
        //public void Linq_Bug_BooleanFieldIsQueryable()
        //{
        //    var webContentName = "Linq_Bug_NewBooleanFieldIsQueryable_WebContent_Lifespan";
        //    var fieldName = "EnableLifespan";
        //    DeleteIfExists(RepositoryPath.Combine(TestRoot.Path, webContentName));
        //    var content = Content.CreateNew("WebContent", TestRoot, webContentName);
        //    content.Save();
        //    SenseNet.Search.Indexing.LuceneManager.Commit(true);
        //    var contentId = content.Id;

        //    Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] == true).FirstOrDefault());
        //    Assert.IsNotNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] != true).FirstOrDefault());
        //    Assert.IsNotNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] == false).FirstOrDefault());
        //    Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] != false).FirstOrDefault());

        //    content = Content.Load(contentId);
        //    content[fieldName] = false;
        //    content.Save();
        //    SenseNet.Search.Indexing.LuceneManager.Commit(true);

        //    Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] == true).FirstOrDefault());
        //    Assert.IsNotNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] != true).FirstOrDefault());
        //    Assert.IsNotNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] == false).FirstOrDefault());
        //    Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] != false).FirstOrDefault());

        //    content = Content.Load(contentId);
        //    content[fieldName] = true;
        //    content.Save();
        //    SenseNet.Search.Indexing.LuceneManager.Commit(true);

        //    Assert.IsNotNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] == true).FirstOrDefault());
        //    Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] != true).FirstOrDefault());
        //    Assert.IsNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] == false).FirstOrDefault());
        //    Assert.IsNotNull(Content.All.DisableAutofilters().Where(c => c.Name == webContentName && (bool)c[fieldName] != false).FirstOrDefault());
        //}
        //private void DeleteIfExists(string path)
        //{
        //    var node = Node.LoadNode(path);
        //    if (node != null)
        //        node.ForceDelete();
        //}

        ////[TestMethod, TestCategory("IR, LINQ")]
        ////public void Linq_ContentSet_CountVsTotalCount()
        ////{
        ////    var qtextBase = "+Name:(a* b*) +TypeIs:ContentType .AUTOFILTERS:OFF";
        ////    var realLength = ContentQuery.Query(qtextBase).Identifiers.Count();
        ////    if (realLength < 11)
        ////        Assert.Inconclusive("Lenght of base set must be greater than 10.");

        ////    //---------------------------------------------

        ////    var top = 5;
        ////    var qtext = qtextBase + " .TOP:" + top;
        ////    var cset = new ContentSet<Content>(new ChildrenDefinition
        ////    {
        ////        AllChildren = false,
        ////        ContentQuery = qtext,
        ////        Top = 10
        ////    }, null);

        ////    var setLength = cset.Count();
        ////    var resultLength = cset.ToArray().Length;

        ////    Assert.AreEqual(top, setLength);
        ////    Assert.AreEqual(top, resultLength);

        ////    //---------------------------------------------

        ////    top = realLength + 5;
        ////    qtext = "+Name:(a* b*) +TypeIs:ContentType .TOP:" + top + " .AUTOFILTERS:OFF";
        ////    cset = new ContentSet<Content>(new ChildrenDefinition
        ////    {
        ////        AllChildren = false,
        ////        ContentQuery = qtext,
        ////        Top = 10
        ////    }, null);

        ////    setLength = cset.Count();
        ////    resultLength = cset.ToArray().Length;

        ////    Assert.AreEqual(realLength, setLength);
        ////    Assert.AreEqual(realLength, resultLength);

        ////}

        //=========================================================================================================

        private string GetQueryString<T>(IQueryable<T> queryable)
        {
            var cs = queryable.Provider as ContentSet<T>;
            return cs?.GetCompiledQuery().ToString();
        }

        private void SaveNode(Node node)
        {
            foreach (var observer in NodeObserver.GetObserverTypes())
                node.DisableObserver(observer);
            node.Save();
        }

        private void InvalidOperationTest(Action action)
        {
            try
            {
                action();
                Assert.Fail("InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException e)
            {
                // expected exception
            }
        }
        private void ArgumentOutOfRangeTest(Action action)
        {
            try
            {
                action();
                Assert.Fail("ArgumentOutOfRangeException was not thrown.");
            }
            catch (ArgumentOutOfRangeException e)
            {
                // expected exception
            }
        }
        private void AsEnumerableError(string expectedMethodName, Action action)
        {
            var message = string.Empty;
            try
            {
                action();
                Assert.Fail("NotSupportedException was not thrown.");
            }
            catch (SnNotSupportedException e)
            {
                message = e.Message;
                // expected exception
            }
            catch (NotSupportedException e)
            {
                message = e.Message;
                // expected exception
            }

            // $"Cannot resolve an expression. Use 'AsEnumerable()' method before calling '{lastMethodName}' method");
            var actualMethodName = message.Split('\'')[3];
            Assert.AreEqual(expectedMethodName, actualMethodName);
        }

    }
}
