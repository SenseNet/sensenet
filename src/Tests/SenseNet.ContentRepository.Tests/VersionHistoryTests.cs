using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class VersionHistoryTests : TestBase
    {
        [TestMethod]
        public void Versioning_History_LongHistory()
        {
            Test(() =>
            {
                //SnTrace.Test.Write("<?--------------------------------- 01 Start");
                const string fileName = "LongHistory.txt";
                const string fileBinary = @"Lorem ipsum ...";
                var root = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                root.Save();
                var test = new File(root) {Name = fileName};
                test.Binary.SetStream(RepositoryTools.GetStreamFromString(fileBinary));
                test.Binary.FileName = fileName;
                test.Save();

                //------------ Approving: False, Versioning: None
                test.VersioningMode = VersioningType.None;
                test.ApprovingMode = ApprovingType.False;
                //SnTrace.Test.Write("<?--------------------------------- Versioning: None, Approving False");


                test.Description = "Init_Value";
                //SnTrace.Test.Write("<?--------------------------------- 02 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A", "#1");
                AssertPropertyValues(test, false, "Init_Value", "#1");

                //SnTrace.Test.Write("<?--------------------------------- 03 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.L", "#2");

                test.Description = "After-CheckOut";
                test.TrashDisabled = true;

                //SnTrace.Test.Write("<?--------------------------------- 04 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.L", "#3");
                AssertPropertyValues(test, true, "After-CheckOut", "#3");

                //SnTrace.Test.Write("<?--------------------------------- 05 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A", "#4");

                //SnTrace.Test.Write("<?--------------------------------- 06 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.L", "#5");

                test.Description = "Before-Undo";
                test.TrashDisabled = false;

                //SnTrace.Test.Write("<?--------------------------------- 07 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.L", "#6");
                AssertPropertyValues(test, false, "Before-Undo", "#6");

                //SnTrace.Test.Write("<?--------------------------------- 08 UndoCheckOut");
                test.UndoCheckOut();
                AssertVersionHistory(test, "V1.0.A", "#7");
                AssertPropertyValues(test, true, "After-CheckOut", "#7");

                //------------ Approving: False, Versioning: Major
                test.VersioningMode = VersioningType.MajorOnly;
                test.ApprovingMode = ApprovingType.False;
                //SnTrace.Test.Write("<?--------------------------------- Versioning: MajorOnly, Approving False");

                //SnTrace.Test.Write("<?--------------------------------- 09 Save");
                test.Save(SavingMode.KeepVersion);
                AssertVersionHistory(test, "V1.0.A", "#8");

                //SnTrace.Test.Write("<?--------------------------------- 10 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.L", "#9");

                //SnTrace.Test.Write("<?--------------------------------- 11 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.L", "#10");

                //SnTrace.Test.Write("<?--------------------------------- 12 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A", "#11");

                test.Description = "OFF-Major-BeforeCheckOut";
                test.TrashDisabled = false;

                //SnTrace.Test.Write("<?--------------------------------- 13 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A", "#12");
                AssertPropertyValues(test, false, "OFF-Major-BeforeCheckOut", "#12");

                //SnTrace.Test.Write("<?--------------------------------- 14 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.L", "#13");

                test.Description = "OFF-Major-BeforeUndo";
                test.TrashDisabled = true;

                //SnTrace.Test.Write("<?--------------------------------- 15 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.L", "#14");
                AssertPropertyValues(test, true, "OFF-Major-BeforeUndo", "#14");

                //SnTrace.Test.Write("<?--------------------------------- 16 UndoCheckOut");
                test.UndoCheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A", "#15");
                AssertPropertyValues(test, false, "OFF-Major-BeforeCheckOut", "#15");

                //------------ Approving: False, Versioning: MajorAndMinor
                test.VersioningMode = VersioningType.MajorAndMinor;
                test.ApprovingMode = ApprovingType.False;
                //SnTrace.Test.Write("<?--------------------------------- Versioning: MajorAndMinor, Approving False");

                //SnTrace.Test.Write("<?--------------------------------- 17 Save");
                test.Save(SavingMode.KeepVersion);

                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A", "#16");

                //SnTrace.Test.Write("<?--------------------------------- 18 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.L", "#17");

                //SnTrace.Test.Write("<?--------------------------------- 19 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.L", "#18");

                //SnTrace.Test.Write("<?--------------------------------- 20 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D", "#19");

                //SnTrace.Test.Write("<?--------------------------------- 21 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.L", "#20");

                //SnTrace.Test.Write("<?--------------------------------- 22 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.L", "#21");

                //SnTrace.Test.Write("<?--------------------------------- 23 UndoCheckOut");
                test.UndoCheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D", "#22");

                //SnTrace.Test.Write("<?--------------------------------- 24 Save");
                test.Save(SavingMode.RaiseVersionAndLock);
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.L", "#23");

                //SnTrace.Test.Write("<?--------------------------------- 25 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D", "#24");

                //SnTrace.Test.Write("<?--------------------------------- 26 Save");
                test.Save(SavingMode.RaiseVersionAndLock);
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.L", "#25");

                //SnTrace.Test.Write("<?--------------------------------- 27 UndoCheckOut");
                test.UndoCheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D", "#26");

                //SnTrace.Test.Write("<?--------------------------------- 28 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.L", "#27");

                //SnTrace.Test.Write("<?--------------------------------- 29 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.L", "#28");

                //SnTrace.Test.Write("<?--------------------------------- 30 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.D", "#29");

                //SnTrace.Test.Write("<?--------------------------------- 31 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.D V3.4.L", "#30");

                //SnTrace.Test.Write("<?--------------------------------- 32 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.D V3.4.L", "#31");

                //SnTrace.Test.Write("<?--------------------------------- 33 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.D V3.4.D", "#32");

                //------------ Approving: True, Versioning: None
                test.VersioningMode = VersioningType.None;
                test.ApprovingMode = ApprovingType.True;
                //SnTrace.Test.Write("<?--------------------------------- Versioning: None, Approving True");

                //SnTrace.Test.Write("<?--------------------------------- 34 Save");
                test.Save(SavingMode.KeepVersion);
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.D V3.4.D", "#33");

                //SnTrace.Test.Write("<?--------------------------------- 35 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D V3.3.D V3.4.D V4.0.L", "#34");

                test.Description = "ON-None-Before-CheckOut";
                test.TrashDisabled = false;

                //SnTrace.Test.Write("<?--------------------------------- 36 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.P", "#35");
                AssertPropertyValues(test, false, "ON-None-Before-CheckOut", "#35");

                //SnTrace.Test.Write("<?--------------------------------- 37 Reject");
                test.Reject();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.R", "#36");

                //SnTrace.Test.Write("<?--------------------------------- 38 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.R V5.0.L", "#37");

                //SnTrace.Test.Write("<?--------------------------------- 39 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.R V5.0.L", "#38");

                //SnTrace.Test.Write("<?--------------------------------- 40 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V4.0.R V5.0.P", "#39");

                //SnTrace.Test.Write("<?--------------------------------- 41 Approve");
                test.Approve();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A", "#40");

                //------------ Approving: True, Versioning: MajorAndMinor
                test.VersioningMode = VersioningType.MajorAndMinor;
                test.ApprovingMode = ApprovingType.True;
                //SnTrace.Test.Write("<?--------------------------------- Versioning: MajorAndMinor, Approving True");

                //SnTrace.Test.Write("<?--------------------------------- 42 Save");
                test.Save();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D", "#41");

                //SnTrace.Test.Write("<?--------------------------------- 43 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.L", "#42");

                test.Description = "ON-MajorMinor-BeforeCheckIn";
                test.TrashDisabled = true;

                //SnTrace.Test.Write("<?--------------------------------- 44 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.D", "#43");
                AssertPropertyValues(test, true, "ON-MajorMinor-BeforeCheckIn", "#43");

                //SnTrace.Test.Write("<?--------------------------------- 45 Publish");
                test.Publish();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.P", "#44");
                AssertPropertyValues(test, true, "ON-MajorMinor-BeforeCheckIn", "#44");

                //SnTrace.Test.Write("<?--------------------------------- 46 Reject");
                test.Reject();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.R", "#45");

                //SnTrace.Test.Write("<?--------------------------------- 47 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.R V3.3.L", "#46");

                //SnTrace.Test.Write("<?--------------------------------- 48 Publish");
                test.Publish();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.R V3.3.P", "#47");

                //SnTrace.Test.Write("<?--------------------------------- 49 Approve");
                test.Approve();
                AssertVersionHistory(test, "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.R V4.0.A", "#48");

                //this must never change
                const string oldHistory = "V1.0.A V2.0.A V3.0.A V3.1.D V3.2.R ";

                //SnTrace.Test.Write("<?--------------------------------- 50 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.L", "#49");

                //SnTrace.Test.Write("<?--------------------------------- 51 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D", "#50");

                //SnTrace.Test.Write("<?--------------------------------- 52 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.L", "#51");

                //SnTrace.Test.Write("<?--------------------------------- 53 Publish");
                test.Publish();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.P", "#52");

                //SnTrace.Test.Write("<?--------------------------------- 54 Reject");
                test.Reject();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R", "#53");

                //SnTrace.Test.Write("<?--------------------------------- 55 Publish");
                test.Publish();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.P", "#54");

                //SnTrace.Test.Write("<?--------------------------------- 56 Reject");
                test.Reject();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.R", "#54");

                //SnTrace.Test.Write("<?--------------------------------- 57 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.R V4.4.L", "#55");

                //------------ Approving: True, Versioning: Major
                test.VersioningMode = VersioningType.MajorOnly;
                test.ApprovingMode = ApprovingType.True;
                //SnTrace.Test.Write("<?--------------------------------- Versioning: MajorOnly, Approving True");

                //SnTrace.Test.Write("<?--------------------------------- 58 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.R V4.4.P", "#56");

                //SnTrace.Test.Write("<?--------------------------------- 59 Reject");
                test.Reject();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.R V4.4.R", "#57");

                //SnTrace.Test.Write("<?--------------------------------- 60 CheckOut");
                test.CheckOut();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.R V4.4.R V5.0.L", "#58");

                test.Description = "ON-Major-Before-CheckIn";
                test.TrashDisabled = false;

                //SnTrace.Test.Write("<?--------------------------------- 61 CheckIn");
                test.CheckIn();
                AssertVersionHistory(test, oldHistory + "V4.0.A V4.1.D V4.2.R V4.3.R V4.4.R V5.0.P", "#59");
                AssertPropertyValues(test, false, "ON-Major-Before-CheckIn", "#59");

                //SnTrace.Test.Write("<?--------------------------------- 62 Approve");
                test.Approve();
                AssertVersionHistory(test, oldHistory + "V4.0.A V5.0.A", "#60");
                AssertPropertyValues(test, false, "ON-Major-Before-CheckIn", "#60");
            });
        }

        [TestMethod]
        public void Versioning_History_Bug1308Test()
        {
            Test(() =>
            {
                const string fileName = "bug1308.xml";
                const string fileBinary =
                    @"<?xml version='1.0' encoding='utf-8'?> <ContentType><Fields /></ContentType>";

                var root = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                root.Save();
                var file = new File(root) {Name = fileName};
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileBinary));
                file.Binary.FileName = fileName;
                file.Save();

                //1. checkout
                file.CheckOut();

                //2. checkin
                var exceptionOccured = false;
                try
                {
                    file.UndoCheckOut();
                }
                catch (NullReferenceException)
                {
                    exceptionOccured = true;
                }

                // assert
                var errorMessage = String.Concat("Version history should be V1.0.A instead of: ",
                    GetVersionHistoryString(NodeHead.Get(file.Id)));
                Assert.IsFalse(exceptionOccured, String.Concat("An exception occured during execution.", errorMessage));
                AssertVersionHistory(file, "V1.0.A", errorMessage);
            });
        }

        //==================================================================== Helper methods 

        private static void AssertVersionHistory(Node node, string expectedHistory, string message)
        {
            //SnTrace.Test.Write("<?VersionInfo.Expectation: " + expectedHistory);

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (string.IsNullOrEmpty(expectedHistory))
                throw new ArgumentNullException(nameof(expectedHistory));

            var head = NodeHead.Get(node.Id);
            var actualHistory = GetVersionHistoryString(head);

            Assert.AreEqual(expectedHistory, actualHistory, "Wrong version history " + message);
        }
        private static void AssertPropertyValues(GenericContent gc, bool trashValue, string descValue, string message)
        {
            if (gc == null)
                throw new ArgumentNullException(nameof(gc));

            Assert.AreEqual(trashValue, gc.TrashDisabled, "Wrong TrashDisabled value " + message);
            Assert.AreEqual(descValue, gc.Description, "Wrong Description value " + message);
        }
        private static string GetVersionHistoryString(NodeHead head)
        {
            return String.Join(" ", (from version in head.Versions
                select version.VersionNumber.ToString()).ToArray());
        }
    }
}
