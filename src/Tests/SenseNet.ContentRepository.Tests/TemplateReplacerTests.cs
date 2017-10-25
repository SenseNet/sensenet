using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using System.Web;
using SenseNet.Search.Parser;
using SenseNet.ContentRepository.Schema;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class TemplateReplacerTests : TestBase
    {
        //private TestContext testContextInstance;
        //public override TestContext TestContext
        //{
        //    get { return testContextInstance; }
        //    set { testContextInstance = value; }
        //}

        private const string TestSiteName = "TempRepTestSite";

        private static string TestSitePath
        {
            get { return RepositoryPath.Combine("/Root", TestSiteName); }
        }

        /* ================================================== Test infrastructure */

        //[ClassCleanup]
        //public static void RemoveSite()
        //{
        //    CleanupTestSite();
        //}


        /* ================================================== Test methods */

        /*private static readonly string DateFormat = "yyyy-MM-dd HH:mm:ss";*/

        [TestMethod]
        public void ContentQueryTemplateReplacer_CurrentUser()
        {
            //CreateSafeContentQuery("CreatedBy:@@CurrentUser@@ .TOP:1");

            Test(
                true,
                builder => { builder.UseTraceCategories("Query"); },
                () =>
                {
                    if (1 != User.Current.Id)
                        Assert.Inconclusive("User.Current.Id need to be 1");

                    var text = "@@CurrentUser@@";
                    var expected = User.Current.Id.ToString();
                    Assert.AreEqual(expected, TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), text));

                    text = " @@CurrentUser@@ ";
                    expected = string.Format(" {0} ", User.Current.Id);
                    Assert.AreEqual(expected, TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), text));

                    text = "@@CurrentUser.Path@@";
                    Assert.AreEqual(User.Current.Path, TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), text));
                });
        }
        [TestMethod]
        public void ContentQueryTemplateReplacer_CurrentDate()
        {
            var text = "@@CurrentDate@@";
            AssertDate(text, DateTime.UtcNow.Date, "CurrentDate is invalid.");
        }

        [TestMethod]
        public void ContentQueryTemplateReplacer_3()
        {
            Test(
                true,
                builder => { builder.UseTraceCategories("Query"); },
                () =>
                {
                    var text = "ABC @@CurrentUser@@ DEF";
                    var expected = string.Format("ABC {0} DEF", User.Current.Id);
                    Assert.AreEqual(expected, TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), text));

                    text = "ABC @@CurrentUser@@ DEF @@CurrentUser.Path@@ GHI";
                    expected = string.Format("ABC {0} DEF {1} GHI", User.Current.Id, User.Current.Path);
                    Assert.AreEqual(expected, TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), text));
                });
        }

    [TestMethod]
        public void ContentQueryTemplateReplacer_DateWithModifier()
        {
            // short syntax-------------------------------------------------------------

            AssertDate("@@CurrentDate+30s@@", DateTime.UtcNow.Date.AddSeconds(30), "Date+s is incorrect.");
            AssertDate("@@CurrentDate-30s@@", DateTime.UtcNow.Date.AddSeconds(-30), "Date-s is incorrect.");
            AssertDate("@@CurrentDate+30seconds@@", DateTime.UtcNow.Date.AddSeconds(30), "Date+seconds is incorrect.");
            AssertDate("@@CurrentDate-30seconds@@", DateTime.UtcNow.Date.AddSeconds(-30), "Date-seconds is incorrect.");

            AssertDate("@@CurrentDate+30m@@", DateTime.UtcNow.Date.AddMinutes(30), "Date+m is incorrect.");
            AssertDate("@@CurrentDate-30m@@", DateTime.UtcNow.Date.AddMinutes(-30), "Date-m is incorrect.");
            AssertDate("@@CurrentDate+30minutes@@", DateTime.UtcNow.Date.AddMinutes(30), "Date+minutes is incorrect.");
            AssertDate("@@CurrentDate-30minutes@@", DateTime.UtcNow.Date.AddMinutes(-30), "Date-minutes is incorrect.");

            AssertDate("@@CurrentDate+3h@@", DateTime.UtcNow.Date.AddHours(3), "Date+h is incorrect.");
            AssertDate("@@CurrentDate-3h@@", DateTime.UtcNow.Date.AddHours(-3), "Date-h is incorrect.");
            AssertDate("@@CurrentDate+3hours@@", DateTime.UtcNow.Date.AddHours(3), "Date+hours is incorrect.");
            AssertDate("@@CurrentDate-3hours@@", DateTime.UtcNow.Date.AddHours(-3), "Date-hours is incorrect.");

            AssertDate("@@CurrentDate+3d@@", DateTime.UtcNow.Date.AddDays(3), "Date+d is incorrect.");
            AssertDate("@@CurrentDate-3d@@", DateTime.UtcNow.Date.AddDays(-3), "Date-d is incorrect.");
            AssertDate("@@CurrentDate+3days@@", DateTime.UtcNow.Date.AddDays(3), "Date+days is incorrect.");
            AssertDate("@@CurrentDate-3days@@", DateTime.UtcNow.Date.AddDays(-3), "Date-days is incorrect.");

            AssertDate("@@CurrentDate+5Workdays@@", DateTime.UtcNow.Date.AddWorkdays(5), "Date+workdays is incorrect.");
            AssertDate("@@CurrentDate-15Workdays@@", DateTime.UtcNow.Date.AddWorkdays(-15), "Date-workdays is incorrect.");

            AssertDate("@@CurrentDate+3weeks@@", DateTime.UtcNow.Date.AddDays(21), "Date+weeks is incorrect.");
            AssertDate("@@CurrentDate-3weeks@@", DateTime.UtcNow.Date.AddDays(-21), "Date-weeks is incorrect.");

            AssertDate("@@CurrentDate+3month@@", DateTime.UtcNow.Date.AddMonths(3), "Date+month is incorrect.");
            AssertDate("@@CurrentDate-3month@@", DateTime.UtcNow.Date.AddMonths(-3), "Date-month is incorrect.");
            AssertDate("@@CurrentDate+3months@@", DateTime.UtcNow.Date.AddMonths(3), "Date+months is incorrect.");
            AssertDate("@@CurrentDate-3months@@", DateTime.UtcNow.Date.AddMonths(-3), "Date-months is incorrect.");

            AssertDate("@@CurrentDate+3y@@", DateTime.UtcNow.Date.AddYears(3), "Date+y is incorrect.");
            AssertDate("@@CurrentDate-3y@@", DateTime.UtcNow.Date.AddYears(-3), "Date-y is incorrect.");
            AssertDate("@@CurrentDate+3years@@", DateTime.UtcNow.Date.AddYears(3), "Date+years is incorrect.");
            AssertDate("@@CurrentDate-3years@@", DateTime.UtcNow.Date.AddYears(-3), "Date-years is incorrect.");

            // method syntax------------------------------------------------------------

            AssertDate("@@CurrentDate.AddHours(12)@@", DateTime.UtcNow.Date.AddHours(12), "Date.AddHours is incorrect.");
            AssertDate("@@CurrentDate.AddHours(-12)@@", DateTime.UtcNow.Date.AddHours(-12), "Date.AddHours is incorrect.");
            AssertDate("@@CurrentDate.AddDays(3)@@", DateTime.UtcNow.Date.AddDays(3), "Date.AddDays is incorrect.");
            AssertDate("@@CurrentDate.AddDays(-3)@@", DateTime.UtcNow.Date.AddDays(-3), "Date.AddDays is incorrect.");
            AssertDate("@@CurrentDate.AddMonths(3)@@", DateTime.UtcNow.Date.AddMonths(3), "Date.AddMonths is incorrect.");
            AssertDate("@@CurrentDate.AddMonths(-3)@@", DateTime.UtcNow.Date.AddMonths(-3), "Date.AddMonths is incorrect.");
            AssertDate("@@CurrentDate.AddYears(3)@@", DateTime.UtcNow.Date.AddYears(3), "Date.AddYears is incorrect.");
            AssertDate("@@CurrentDate.AddYears(-3)@@", DateTime.UtcNow.Date.AddYears(-3), "Date.AddYears is incorrect.");

            AssertDate("@@CurrentDate.PlusDays(3)@@", DateTime.UtcNow.Date.AddDays(3), "Date.PlusDays is incorrect.");
            AssertDate("@@CurrentDate.PlusDays(-3)@@", DateTime.UtcNow.Date.AddDays(-3), "Date.PlusDays is incorrect.");

            AssertDate("@@CurrentDate.MinusDays(3)@@", DateTime.UtcNow.Date.AddDays(-3), "Date.MinusDays is incorrect.");
            AssertDate("@@CurrentDate.MinusDays(-3)@@", DateTime.UtcNow.Date.AddDays(3), "Date.MinusDays is incorrect.");
            AssertDate("@@CurrentDate.SubtractDays(3)@@", DateTime.UtcNow.Date.AddDays(-3), "Date.SubtractDays is incorrect.");
            AssertDate("@@CurrentDate.SubtractDays(-3)@@", DateTime.UtcNow.Date.AddDays(3), "Date.SubtractDays is incorrect.");

            AssertDate("@@CurrentDate.AddWorkdays(5)@@", DateTime.UtcNow.Date.AddWorkdays(5), "AddWorkdays is incorrect.");

            // default units
            AssertDate("@@CurrentWeek.Add(6)@@", GetStartOfWeek(DateTime.UtcNow).AddDays(6 * 7), "Week.Add is incorrect.");
            AssertDate("@@CurrentMonth.Add(3)@@", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(3), "Month.Add is incorrect.");
            AssertDate("@@CurrentYear.Add(2)@@", new DateTime(DateTime.UtcNow.Year + 2, 1, 1), "Year.Add is incorrect.");
        }

        [TestMethod]
        public void ContentQueryTemplateReplacer_DateTemplates()
        {
            AssertDate("@@CurrentWeek@@", GetStartOfWeek(DateTime.UtcNow), "CurrentWeek is incorrect.");
            AssertDate("@@CurrentWeek+3weeks@@", GetStartOfWeek(DateTime.UtcNow).AddDays(3 * 7), "CurrentWeek is incorrect.");
            AssertDate("@@CurrentMonth@@", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), "CurrentMonth is incorrect.");
            AssertDate("@@CurrentYear@@", new DateTime(DateTime.UtcNow.Year, 1, 1), "CurrentYear is incorrect.");
            AssertDate("@@Yesterday@@", DateTime.UtcNow.Date.AddDays(-1), "Yesterday is incorrect.");
            AssertDate("@@CurrentDay@@", DateTime.UtcNow.Date, "CurrentDay is incorrect.");
            AssertDate("@@CurrentDate@@", DateTime.UtcNow.Date, "CurrentDate is incorrect.");
            AssertDate("@@Today@@", DateTime.UtcNow.Date, "Today is incorrect.");
            AssertDate("@@Tomorrow@@", DateTime.UtcNow.Date.AddDays(1), "Tomorrow is incorrect.");

            AssertDate("@@CurrentMonth+1week@@", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 8), "CurrentMonth+ is incorrect.");
            AssertDate("@@CurrentYear+1@@", new DateTime(DateTime.UtcNow.Year + 1, 1, 1), "CurrentYear+ is incorrect.");
            AssertDate("@@CurrentYear+15d@@", new DateTime(DateTime.UtcNow.Year, 1, 16), "CurrentYear+ is incorrect.");

            AssertDate("@@NextWeek@@", GetStartOfWeek(DateTime.UtcNow).AddDays(7), "NextWeek is incorrect.");
            AssertDate("@@NextMonth@@", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1), "NextMonth is incorrect.");
            AssertDate("@@NextYear@@", new DateTime(DateTime.UtcNow.Year, 1, 1).AddYears(1), "NextYear is incorrect.");

            AssertDate("@@NextWeek+3day@@", GetStartOfWeek(DateTime.UtcNow).AddDays(10), "NextWeek+ is incorrect.");
            AssertDate("@@NextMonth-1hours@@", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1).AddHours(-1), "NextMonth- is incorrect.");
            AssertDate("@@NextYear+3months@@", new DateTime(DateTime.UtcNow.Year, 1, 1).AddYears(1).AddMonths(3), "NextYear+ is incorrect.");

            AssertDate("@@PreviousWeek@@", GetStartOfWeek(DateTime.UtcNow).AddDays(-7), "PreviousWeek is incorrect.");
            AssertDate("@@PreviousMonth@@", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1), "PreviousMonth is incorrect.");
            AssertDate("@@PreviousYear@@", new DateTime(DateTime.UtcNow.Year, 1, 1).AddYears(-1), "PreviousYear is incorrect.");

            AssertDate("@@NextWorkday@@", DateTime.UtcNow.Date.AddWorkdays(1), "NextWorkday is incorrect.");
            AssertDate("@@NextWorkday+12h@@", DateTime.UtcNow.Date.AddWorkdays(1).AddHours(12), "NextWorkday+ is incorrect.");
            AssertDate("@@CurrentDate+5workdays@@", DateTime.UtcNow.Date.AddWorkdays(5), "+workdays is incorrect.");
        }

        [TestMethod]
        public void ContentQueryTemplateReplacer_Properties()
        {
            Test(
             true,
             builder => { builder.UseTraceCategories("Query"); },
             () =>
             {
                 using (new SystemAccount())
                 {
                     SecurityHandler.CreateAclEditor()
                        .Allow(Identifiers.PortalRootId, User.Administrator.Id, false, PermissionType.PermissionTypes)
                        .Apply();
                     User.Administrator.CreationDate = new DateTime(2001, 01, 01);
                 }

                 var node = Repository.Root;

                 //var date1 = ((User)User.Current).CreationDate;
                 var date1 = node.CreationDate;

                 AssertDate("@@CurrentUser.CreationDate@@", date1, "CU.CreationDate is incorrect.");
                 AssertDate("@@CurrentUser.CreationDate+3minutes@@", date1.AddMinutes(3), "CU.CreationDate is incorrect.");

                 date1 = node.Owner.CreationDate;
                 AssertDate("@@CurrentUser.Owner.CreationDate@@", date1, "CU.Owner.CreationDate is incorrect.");
                 AssertDate("@@CurrentUser.Owner.CreationDate-3days@@", date1.AddDays(-3), "CU.Owner.CreationDate is incorrect.");

                 var index = ((User)User.Current).Index;
                 Assert.AreEqual(index.ToString(), TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), "@@CurrentUser.Index@@"), "Index is incorrect.");
                 Assert.AreEqual((index + 5).ToString(), TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), "@@CurrentUser.Index+5@@"), "Index+ is incorrect.");
                 Assert.AreEqual((index + 5).ToString(), TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), "@@CurrentUser.Index.Add(5)@@"), "Index.Add is incorrect.");

                 // method syntax------------------------------------------------------------

                 date1 = node.Owner.CreationDate;
                 AssertDate("@@CurrentUser.Owner.CreationDate.AddMonths(3)@@", date1.AddMonths(3), "CU.Owner.AddMonths is incorrect.");
                 AssertDate("@@CurrentUser.Owner.CreationDate.SubtractDays(3)@@", date1.AddDays(-3), "CU.Owner.CreationDate.SubtractDays is incorrect.");
                 AssertDate("@@CurrentUser.Owner.CreationDate.AddDays(-3)@@", date1.AddDays(-3), "CU.Owner.CreationDate.AddDays(-) is incorrect.");
             });
        }

        private static void AssertDate(string text, DateTime expectedDate, string message)
        {
            var expected = "'" + expectedDate.ToString(CultureInfo.InvariantCulture) + "'";
            Assert.AreEqual(expected, TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), text), message);
        }

        /* ================================================== Helper methods */

        //private static void CreatePortalContext()
        //{
        //    CreateTestSite();

        //    const string pagePath = "/fakesiteforms/Root/System/alma.jpg/";

        //    var simulatedOutput = new System.IO.StringWriter();
        //    var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, "", simulatedOutput, "localhost");
        //    var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
        //    var portalContext = PortalContext.Create(simulatedHttpContext);
        //}

        //private static void CreateTestSite()
        //{
        //    var node = Node.LoadNode(TestSitePath);
        //    if (node != null)
        //        return;

        //    var site = new Site(Repository.Root) { Name = TestSiteName };
        //    var urlList = new Dictionary<string, string>(3)
        //                      {
        //                          {"localhost/fakesiteforms", "Forms"},
        //                          {"localhost/fakesitewindows", "Windows"},
        //                          {"localhost/fakesitenone", "None"}
        //                      };
        //    site.UrlList = urlList;
        //    site.Save();
        //}

        //private static void CleanupTestSite()
        //{
        //    var node = Node.LoadNode(TestSitePath);
        //    if (node != null)
        //        node.ForceDelete();
        //}

        private static DateTime GetStartOfWeek(DateTime date)
        {
            var diff = date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            if (diff < 0)
                diff += 7;

            return date.AddDays(-1 * diff).Date;
        }
    }
}
