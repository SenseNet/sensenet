using System;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Packaging.Steps.Internal;
using SenseNet.Packaging.Steps;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class UpgradeProviderConfigurationTests : TestBase
    {
        private static StringBuilder _log;

        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }

        [TestMethod]
        public void Step_UpgradeProviderConfig_DeleteUnnecessaryElements()
        {
            #region var config = ....
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
  </configSections>
  <unity>
    <typeAliases>
      <!-- Provider aliases -->
      <typeAlias alias='IApplicationCache' type='SenseNet.ContentRepository.Storage.AppModel.IApplicationCache, SenseNet.Storage' />
      <!--<typeAlias alias='IActionFactory' type='SenseNet.ContentRepository.IActionFactory, SenseNet.ContentRepository' />-->
      <typeAlias alias='ISearchEngine' type='SenseNet.ContentRepository.Storage.Search.ISearchEngine, SenseNet.Storage' />
      <!-- Provider implementation aliases -->
      <typeAlias alias='IApplicationCacheImpl' type='SenseNet.ContentRepository.ApplicationCache, SenseNet.ContentRepository' />
      <!--<typeAlias alias='IActionFactoryImpl' type='SenseNet.Portal.PortalActionLinkManager, SenseNet.Portal' />-->
      <typeAlias alias='ISearchEngineImpl' type='SenseNet.Search.LuceneSearchEngine, SenseNet.ContentRepository' />
      <!--  -->
      <typeAlias alias='MembershipExtenderBase' type='SenseNet.ContentRepository.Storage.Security.MembershipExtenderBase, SenseNet.Storage' />
      <typeAlias alias='MembershipExtender' type='SenseNet.ContentRepository.Storage.Security.DefaultMembershipExtender, SenseNet.Storage' />
      <typeAlias alias='PurgeUrlCollector' type='SenseNet.Portal.Virtualization.PurgeUrlCollector, SenseNet.Portal' />
      <typeAlias alias='RelatedPurgeUrlCollector' type='SenseNet.Hir24Web.Code.RelatedPurgeUrlCollector, SenseNet.Hir24Web' />
      <!-- Scenario aliases -->
      <typeAlias alias='GenericScenario' type='SenseNet.ApplicationModel.GenericScenario, SenseNet.Portal' />
      <typeAlias alias='SurveyScenario' type='SenseNet.ApplicationModel.SurveyScenario, SenseNet.Portal' />
      <typeAlias alias='SurveySettingsScenario' type='SenseNet.ApplicationModel.SurveySettingsScenario, SenseNet.Portal' />
      <typeAlias alias='VotingSettingsScenario' type='SenseNet.ApplicationModel.VotingSettingsScenario, SenseNet.Portal' />
      <typeAlias alias='VotingAddFieldScenario' type='SenseNet.ApplicationModel.VotingAddFieldScenario, SenseNet.Portal' />
      <typeAlias alias='WorkspaceActions' type='SenseNet.ApplicationModel.WorkspaceActionsScenario, SenseNet.Portal' />
      <typeAlias alias='ListActions' type='SenseNet.ApplicationModel.ListActionsScenario, SenseNet.Portal' />
      <typeAlias alias='New' type='SenseNet.ApplicationModel.NewScenario, SenseNet.Portal' />
      <typeAlias alias='ListItem' type='SenseNet.ApplicationModel.ListItemScenario, SenseNet.Portal' />
      <typeAlias alias='Settings' type='SenseNet.ApplicationModel.SettingsScenario, SenseNet.Portal' />
      <typeAlias alias='Views' type='SenseNet.ApplicationModel.ViewsScenario, SenseNet.Portal' />
      <typeAlias alias='AddField' type='SenseNet.ApplicationModel.AddFieldScenario, SenseNet.Portal' />
      <typeAlias alias='ActionBase' type='SenseNet.ApplicationModel.ActionBase, SenseNet.ContentRepository' />
      <!--<typeAlias alias='UrlAction' type='SenseNet.ApplicationModel.UrlAction, SenseNet.Portal' />-->
      <typeAlias alias='UrlAction' type='SenseNet.Hir24Web.Code.Actions.MultiSiteUrlAction, SenseNet.Hir24Web' />
      <typeAlias alias='UploadAction' type='SenseNet.ApplicationModel.UploadAction, SenseNet.Portal' />
      <typeAlias alias='UserProfileAction' type='SenseNet.ApplicationModel.UserProfileAction, SenseNet.Portal' />
      <typeAlias alias='PurgeFromProxyAction' type='SenseNet.ApplicationModel.PurgeFromProxyAction, SenseNet.Portal' />
      <typeAlias alias='ServiceAction' type='SenseNet.ApplicationModel.ServiceAction, SenseNet.Portal' />
      <typeAlias alias='ClientAction' type='SenseNet.ApplicationModel.ClientAction, SenseNet.Portal' />
      <typeAlias alias='WebdavOpenAction' type='SenseNet.ApplicationModel.WebdavOpenAction, SenseNet.Portal' />
      <typeAlias alias='WebdavBrowseAction' type='SenseNet.ApplicationModel.WebdavBrowseAction, SenseNet.Portal' />
      <typeAlias alias='ContentTypeAction' type='SenseNet.ApplicationModel.ContentTypeAction, SenseNet.Portal' />
      <typeAlias alias='CopyToAction' type='SenseNet.ApplicationModel.CopyToAction, SenseNet.Portal' />
      <typeAlias alias='CopyBatchAction' type='SenseNet.ApplicationModel.CopyBatchAction, SenseNet.Portal' />
      <typeAlias alias='MoveToAction' type='SenseNet.ApplicationModel.MoveToAction, SenseNet.Portal' />
      <typeAlias alias='MoveBatchAction' type='SenseNet.ApplicationModel.MoveBatchAction, SenseNet.Portal' />
      <typeAlias alias='DeleteBatchAction' type='SenseNet.ApplicationModel.DeleteBatchAction, SenseNet.Portal' />
      <typeAlias alias='ContentLinkBatchAction' type='SenseNet.ApplicationModel.ContentLinkBatchAction, SenseNet.Portal, Version=1.0.0.0, Culture=neutral' />
      <typeAlias alias='CopyAppLocalAction' type='SenseNet.ApplicationModel.CopyAppLocalAction, SenseNet.Portal' />
      <typeAlias alias='DeleteLocalAppAction' type='SenseNet.ApplicationModel.DeleteLocalAppAction, SenseNet.Portal' />
      <typeAlias alias='BinarySpecialAction' type='SenseNet.ApplicationModel.BinarySpecialAction, SenseNet.Portal' />
      <typeAlias alias='IViewManager' type='SenseNet.ContentRepository.IViewManager, SenseNet.ContentRepository' />
      <typeAlias alias='ViewManager' type='SenseNet.Portal.UI.ContentListViews.ViewManager, SenseNet.Portal' />
      <typeAlias alias='WorkflowsAction' type='SenseNet.ApplicationModel.WorkflowsAction, SenseNet.Portal' />
      <typeAlias alias='AbortWorkflowAction' type='SenseNet.ApplicationModel.AbortWorkflowAction, SenseNet.Portal' />
      <typeAlias alias='ShareAction' type='SenseNet.ApplicationModel.ShareAction, SenseNet.Portal' />
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
      <!--Custom Actions-->
      <typeAlias alias='FaceBookAction' type='SenseNet.Hir24Web.Code.Actions.FaceBookAction, SenseNet.Hir24Web' />
      <typeAlias alias='IwiwAction' type='SenseNet.Hir24Web.Code.Actions.IwiwAction, SenseNet.Hir24Web' />
      <typeAlias alias='StartlapAction' type='SenseNet.Hir24Web.Code.Actions.StartlapAction, SenseNet.Hir24Web' />
      <typeAlias alias='TwitterAction' type='SenseNet.Hir24Web.Code.Actions.TwitterAction, SenseNet.Hir24Web' />
      <typeAlias alias='CitromailAction' type='SenseNet.Hir24Web.Code.Actions.CitromailAction, SenseNet.Hir24Web' />
      <typeAlias alias='SiteToStartlapAction' type='SenseNet.Hir24Web.Code.Actions.SiteToStartlapAction, SenseNet.Hir24Web' />
      <typeAlias alias='LinkAction' type='SenseNet.Hir24Web.Code.Actions.LinkAction, SenseNet.Hir24Web' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <!-- Binding providers to implementations -->
          <type type='IApplicationCache' mapTo='IApplicationCacheImpl' />
          <!--<type type='IActionFactory' mapTo='IActionFactoryImpl' />-->
          <type type='ISearchEngine' mapTo='ISearchEngineImpl' />
          <!--  -->
          <type type='MembershipExtenderBase' mapTo='MembershipExtender'>
            <lifetime type='singleton' />
          </type>
          <type type='PurgeUrlCollector' mapTo='RelatedPurgeUrlCollector'>
            <lifetime type='singleton' />
          </type>
          <type type='ActionBase' mapTo='ActionBase' name='ActionBase' />
          <type type='ActionBase' mapTo='UrlAction' name='UrlAction' />
          <type type='ActionBase' mapTo='UploadAction' name='UploadAction' />
          <type type='ActionBase' mapTo='UserProfileAction' name='UserProfileAction' />
          <type type='ActionBase' mapTo='PurgeFromProxyAction' name='PurgeFromProxyAction' />
          <type type='ActionBase' mapTo='ServiceAction' name='ServiceAction' />
          <type type='ActionBase' mapTo='ClientAction' name='ClientAction' />
          <type type='ActionBase' mapTo='WebdavOpenAction' name='WebdavOpenAction' />
          <type type='ActionBase' mapTo='WebdavBrowseAction' name='WebdavBrowseAction' />
          <type type='ActionBase' mapTo='ContentTypeAction' name='ContentTypeAction' />
          <type type='ActionBase' mapTo='CopyToAction' name='CopyToAction' />
          <type type='ActionBase' mapTo='CopyBatchAction' name='CopyBatchAction' />
          <type type='ActionBase' mapTo='MoveToAction' name='MoveToAction' />
          <type type='ActionBase' mapTo='MoveBatchAction' name='MoveBatchAction' />
          <type type='ActionBase' mapTo='DeleteBatchAction' name='DeleteBatchAction' />
          <type type='ActionBase' mapTo='ContentLinkBatchAction' name='ContentLinkBatchAction' />
          <type type='ActionBase' mapTo='CopyAppLocalAction' name='CopyAppLocalAction' />
          <type type='ActionBase' mapTo='DeleteLocalAppAction' name='DeleteLocalAppAction' />
          <type type='ActionBase' mapTo='BinarySpecialAction' name='BinarySpecialAction' />
          <type type='ActionBase' mapTo='WorkflowsAction' name='WorkflowsAction' />
          <type type='ActionBase' mapTo='AbortWorkflowAction' name='AbortWorkflowAction' />
          <type type='ActionBase' mapTo='ShareAction' name='ShareAction' />
          <type name='ViewManager' type='IViewManager' mapTo='ViewManager'>
            <lifetime type='singleton' />
          </type>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
          <type type='ActionBase' mapTo='FaceBookAction' name='FaceBookAction' />
          <type type='ActionBase' mapTo='IwiwAction' name='IwiwAction' />
          <type type='ActionBase' mapTo='StartlapAction' name='StartlapAction' />
          <type type='ActionBase' mapTo='TwitterAction' name='TwitterAction' />
          <type type='ActionBase' mapTo='CitromailAction' name='CitromailAction' />
          <type type='ActionBase' mapTo='SiteToStartlapAction' name='SiteToStartlapAction' />
          <type type='ActionBase' mapTo='LinkAction' name='LinkAction' />
          <type name='GenericScenario' type='GenericScenario' mapTo='GenericScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='Actions' type='GenericScenario' mapTo='GenericScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='WorkspaceActions' type='GenericScenario' mapTo='WorkspaceActions'>
            <lifetime type='singleton' />
          </type>
          <type name='ListActions' type='GenericScenario' mapTo='ListActions'>
            <lifetime type='singleton' />
          </type>
          <type name='New' type='GenericScenario' mapTo='New'>
            <lifetime type='singleton' />
          </type>
          <type name='ListItem' type='GenericScenario' mapTo='ListItem'>
            <lifetime type='singleton' />
          </type>
          <type name='Settings' type='GenericScenario' mapTo='Settings'>
            <lifetime type='singleton' />
          </type>
          <type name='Views' type='GenericScenario' mapTo='Views'>
            <lifetime type='singleton' />
          </type>
          <type name='AddField' type='GenericScenario' mapTo='AddField'>
            <lifetime type='singleton' />
          </type>
          <type name='SurveyScenario' type='GenericScenario' mapTo='SurveyScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='SurveySettingsScenario' type='GenericScenario' mapTo='SurveySettingsScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='VotingSettingsScenario' type='GenericScenario' mapTo='VotingSettingsScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='VotingAddFieldScenario' type='GenericScenario' mapTo='VotingAddFieldScenario'>
            <lifetime type='singleton' />
          </type>
        </types>
      </container>
    </containers>
  </unity>
</configuration>
";
            #endregion
            #region var expected = ...
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
  </configSections>
  <unity>
    <typeAliases>
      <!-- Provider aliases -->
      <typeAlias alias='IApplicationCache' type='SenseNet.ContentRepository.Storage.AppModel.IApplicationCache, SenseNet.Storage' />
      <!--<typeAlias alias='IActionFactory' type='SenseNet.ContentRepository.IActionFactory, SenseNet.ContentRepository' />-->
      <typeAlias alias='ISearchEngine' type='SenseNet.ContentRepository.Storage.Search.ISearchEngine, SenseNet.Storage' />
      <!-- Provider implementation aliases -->
      <typeAlias alias='IApplicationCacheImpl' type='SenseNet.ContentRepository.ApplicationCache, SenseNet.ContentRepository' />
      <!--<typeAlias alias='IActionFactoryImpl' type='SenseNet.Portal.PortalActionLinkManager, SenseNet.Portal' />-->
      <typeAlias alias='ISearchEngineImpl' type='SenseNet.Search.LuceneSearchEngine, SenseNet.ContentRepository' />
      <!--  -->
      <typeAlias alias='MembershipExtenderBase' type='SenseNet.ContentRepository.Storage.Security.MembershipExtenderBase, SenseNet.Storage' />
      <typeAlias alias='MembershipExtender' type='SenseNet.ContentRepository.Storage.Security.DefaultMembershipExtender, SenseNet.Storage' />
      <typeAlias alias='PurgeUrlCollector' type='SenseNet.Portal.Virtualization.PurgeUrlCollector, SenseNet.Portal' />
      <typeAlias alias='RelatedPurgeUrlCollector' type='SenseNet.Hir24Web.Code.RelatedPurgeUrlCollector, SenseNet.Hir24Web' />
      <!-- Scenario aliases -->
      <!--<typeAlias alias='UrlAction' type='SenseNet.ApplicationModel.UrlAction, SenseNet.Portal' />-->
      <typeAlias alias='IViewManager' type='SenseNet.ContentRepository.IViewManager, SenseNet.ContentRepository' />
      <typeAlias alias='ViewManager' type='SenseNet.Portal.UI.ContentListViews.ViewManager, SenseNet.Portal' />
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
      <!--Custom Actions-->
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <!-- Binding providers to implementations -->
          <type type='IApplicationCache' mapTo='IApplicationCacheImpl' />
          <!--<type type='IActionFactory' mapTo='IActionFactoryImpl' />-->
          <type type='ISearchEngine' mapTo='ISearchEngineImpl' />
          <!--  -->
          <type type='MembershipExtenderBase' mapTo='MembershipExtender'>
            <lifetime type='singleton' />
          </type>
          <type type='PurgeUrlCollector' mapTo='RelatedPurgeUrlCollector'>
            <lifetime type='singleton' />
          </type>
          <type name='ViewManager' type='IViewManager' mapTo='ViewManager'>
            <lifetime type='singleton' />
          </type>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
        </types>
      </container>
    </containers>
  </unity>
</configuration>
";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("DeleteUnnecessareUnityElements", configXml);
            });
        }
        [TestMethod]
        public void Step_UpgradeProviderConfig_TransformProviderElements_Delete()
        {
            #region var config = ...
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='ISearchEngine' type='SenseNet.ContentRepository.Storage.Search.ISearchEngine, SenseNet.Storage' />
      <typeAlias alias='ISearchEngineImpl' type='SenseNet.Search.LuceneSearchEngine, SenseNet.ContentRepository' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <type type='ISearchEngine' mapTo='ISearchEngineImpl' />
        </types>
      </container>
    </containers>
  </unity>
</configuration>
";
            #endregion
            #region
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
  </configSections>
  <unity>
    <typeAliases>
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
        </types>
      </container>
    </containers>
  </unity>
</configuration>
";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("TransformProviderElements", configXml);
            });
        }
        [TestMethod]
        public void Step_UpgradeProviderConfig_TransformProviderElements_Move()
        {
            #region var config = ...
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='PurgeUrlCollector' type='SenseNet.Portal.Virtualization.PurgeUrlCollector, SenseNet.Portal' />
      <typeAlias alias='RelatedPurgeUrlCollector' type='SenseNet.Customization.Code.RelatedPurgeUrlCollector, SenseNet.Customization' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <type type='PurgeUrlCollector' mapTo='RelatedPurgeUrlCollector'>
            <lifetime type='singleton' />
          </type>
        </types>
      </container>
    </containers>
  </unity>
</configuration>
";
            #endregion
            #region expected = ...
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
        </types>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
      <add key='PurgeUrlCollector' value='SenseNet.Customization.Code.RelatedPurgeUrlCollector' />
    </providers>
  </sensenet>
</configuration>
";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("TransformProviderElements", configXml);
            });
        }
        [TestMethod]
        public void Step_UpgradeProviderConfig_CleanupUnitySection_Remove()
        {
            #region var config = ...
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='General' type='GeneralType, AssemblyName' />
      <typeAlias alias='Specific' type='SpecificType, AssemblyName' />
      <!-- Scenario aliases -->
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
        </types>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
    </providers>
  </sensenet>
</configuration>";
            #endregion
            #region expected = ...
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <sensenet>
    <providers>
    </providers>
  </sensenet>
</configuration>";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("Execute", configXml);
            });

            Assert.IsTrue(_log.ToString().Contains("Unity section is totally removed."));
        }
        [TestMethod]
        public void Step_UpgradeProviderConfig_CleanupUnitySection_KeepProviders()
        {
            #region var config = ...
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='General' type='GeneralType, AssemblyName' />
      <typeAlias alias='Specific' type='SpecificType, AssemblyName' />
      <!-- Scenario aliases -->
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
          <type name='Customization' type='General' mapTo='Specific' />
        </types>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
    </providers>
  </sensenet>
</configuration>";
            #endregion
            #region expected = ...
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='General' type='GeneralType, AssemblyName' />
      <typeAlias alias='Specific' type='SpecificType, AssemblyName' />
      <!-- Scenario aliases -->
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
          <type name='Customization' type='General' mapTo='Specific' />
        </types>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
    </providers>
  </sensenet>
</configuration>";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("Execute", configXml);
            });

            Assert.IsTrue(_log.ToString().Contains("Providers container is not removed."));
            Assert.IsTrue(_log.ToString().Contains("Unity section is not removed."));
        }
        [TestMethod]
        public void Step_UpgradeProviderConfig_CleanupUnitySection_KeepUnity()
        {
            #region var config = ...
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='General' type='GeneralType, AssemblyName' />
      <typeAlias alias='Specific' type='SpecificType, AssemblyName' />
      <!-- Scenario aliases -->
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
        </types>
      </container>
      <container name='Providers2'>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
    </providers>
  </sensenet>
</configuration>";
            #endregion
            #region expected = ...
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='General' type='GeneralType, AssemblyName' />
      <typeAlias alias='Specific' type='SpecificType, AssemblyName' />
      <!-- Scenario aliases -->
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
    </typeAliases>
    <containers>
      <container name='Providers2'>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
    </providers>
  </sensenet>
</configuration>";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("Execute", configXml);
            });

            Assert.IsTrue(_log.ToString().Contains("Providers container is removed."));
            Assert.IsTrue(_log.ToString().Contains("Unity section is not removed."));
        }
        [TestMethod]
        public void Step_UpgradeProviderConfig_Execute()
        {
            #region var config = ...
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='unity' type='Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration' />
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <unity>
    <typeAliases>
      <typeAlias alias='PurgeUrlCollector' type='SenseNet.Portal.Virtualization.PurgeUrlCollector, SenseNet.Portal' />
      <typeAlias alias='PurgeFromProxyAction' type='SenseNet.ApplicationModel.PurgeFromProxyAction, SenseNet.Portal' />
      <typeAlias alias='UserProfileAction' type='SenseNet.ApplicationModel.UserProfileAction, SenseNet.Portal' />
      <typeAlias alias='ShareAction' type='SenseNet.ApplicationModel.ShareAction, SenseNet.Portal' />
      <typeAlias alias='IApplicationCache' type='SenseNet.ContentRepository.Storage.AppModel.IApplicationCache, SenseNet.Storage' />
      <typeAlias alias='ISearchEngine' type='SenseNet.ContentRepository.Storage.Search.ISearchEngine, SenseNet.Storage' />
      <typeAlias alias='IApplicationCacheImpl' type='SenseNet.ContentRepository.ApplicationCache, SenseNet.ContentRepository' />
      <typeAlias alias='ISearchEngineImpl' type='SenseNet.Search.LuceneSearchEngine, SenseNet.ContentRepository' />
      <!-- Scenario aliases -->
      <typeAlias alias='GenericScenario' type='SenseNet.ApplicationModel.GenericScenario, SenseNet.Portal' />
      <typeAlias alias='SurveyScenario' type='SenseNet.ApplicationModel.SurveyScenario, SenseNet.Portal' />
      <typeAlias alias='QuizSurveyScenario' type='SenseNet.Customization.Quiz.QuizSurveyScenario, SenseNet.Customization' />
      <typeAlias alias='SurveySettingsScenario' type='SenseNet.ApplicationModel.SurveySettingsScenario, SenseNet.Portal' />
      <typeAlias alias='VotingSettingsScenario' type='SenseNet.ApplicationModel.VotingSettingsScenario, SenseNet.Portal' />
      <typeAlias alias='VotingAddFieldScenario' type='SenseNet.ApplicationModel.VotingAddFieldScenario, SenseNet.Portal' />
      <typeAlias alias='WorkspaceActions' type='SenseNet.ApplicationModel.WorkspaceActionsScenario, SenseNet.Portal' />
      <typeAlias alias='ListActions' type='SenseNet.ApplicationModel.ListActionsScenario, SenseNet.Portal' />
      <typeAlias alias='New' type='SenseNet.ApplicationModel.NewScenario, SenseNet.Portal' />
      <typeAlias alias='ListItem' type='SenseNet.ApplicationModel.ListItemScenario, SenseNet.Portal' />
      <typeAlias alias='Settings' type='SenseNet.ApplicationModel.SettingsScenario, SenseNet.Portal' />
      <typeAlias alias='Views' type='SenseNet.ApplicationModel.ViewsScenario, SenseNet.Portal' />
      <typeAlias alias='AddField' type='SenseNet.ApplicationModel.AddFieldScenario, SenseNet.Portal' />
      <typeAlias alias='ActionBase' type='SenseNet.ApplicationModel.ActionBase, SenseNet.ContentRepository' />
      <typeAlias alias='UrlAction' type='SenseNet.ApplicationModel.UrlAction, SenseNet.Portal' />
      <typeAlias alias='UploadAction' type='SenseNet.ApplicationModel.UploadAction, SenseNet.Portal' />
      <typeAlias alias='ServiceAction' type='SenseNet.ApplicationModel.ServiceAction, SenseNet.Portal' />
      <typeAlias alias='ClientAction' type='SenseNet.ApplicationModel.ClientAction, SenseNet.Portal' />
      <typeAlias alias='WebdavOpenAction' type='SenseNet.ApplicationModel.WebdavOpenAction, SenseNet.Portal' />
      <typeAlias alias='WebdavBrowseAction' type='SenseNet.ApplicationModel.WebdavBrowseAction, SenseNet.Portal' />
      <typeAlias alias='ContentTypeAction' type='SenseNet.ApplicationModel.ContentTypeAction, SenseNet.Portal' />
      <typeAlias alias='CopyToAction' type='SenseNet.ApplicationModel.CopyToAction, SenseNet.Portal' />
      <typeAlias alias='CopyBatchAction' type='SenseNet.ApplicationModel.CopyBatchAction, SenseNet.Portal' />
      <typeAlias alias='MoveToAction' type='SenseNet.ApplicationModel.MoveToAction, SenseNet.Portal' />
      <typeAlias alias='MoveBatchAction' type='SenseNet.ApplicationModel.MoveBatchAction, SenseNet.Portal' />
      <typeAlias alias='DeleteBatchAction' type='SenseNet.ApplicationModel.DeleteBatchAction, SenseNet.Portal' />
      <typeAlias alias='ContentLinkBatchAction' type='SenseNet.ApplicationModel.ContentLinkBatchAction, SenseNet.Portal, Version=1.0.0.0, Culture=neutral' />
      <typeAlias alias='CopyAppLocalAction' type='SenseNet.ApplicationModel.CopyAppLocalAction, SenseNet.Portal' />
      <typeAlias alias='DeleteLocalAppAction' type='SenseNet.ApplicationModel.DeleteLocalAppAction, SenseNet.Portal' />
      <typeAlias alias='ApproveAction' type='SenseNet.Customization.ApplicationModel.ApproveByCommunication.ApproveAction, SenseNet.Customization' />
      <typeAlias alias='DenyAction' type='SenseNet.Customization.ApplicationModel.ApproveByCommunication.DenyAction, SenseNet.Customization' />
      <typeAlias alias='SoftLinkBrowseAction' type='SenseNet.Customization.ApplicationModel.SoftLinkBrowseAction, SenseNet.Customization' />
      <typeAlias alias='CustomizationSimpleLinkBrowseAction' type='SenseNet.Customization.ApplicationModel.CustomizationSimpleLinkBrowseAction, SenseNet.Customization' />
      <typeAlias alias='LinkBrowseAction' type='SenseNet.Customization.ApplicationModel.LinkBrowseAction, SenseNet.Customization' />
      <typeAlias alias='IViewManager' type='SenseNet.ContentRepository.IViewManager, SenseNet.ContentRepository' />
      <typeAlias alias='ViewManager' type='SenseNet.Portal.UI.ContentListViews.ViewManager, SenseNet.Portal' />
      <typeAlias alias='BinarySpecialAction' type='SenseNet.ApplicationModel.BinarySpecialAction, SenseNet.Portal' />
      <typeAlias alias='WorkflowsAction' type='SenseNet.ApplicationModel.WorkflowsAction, SenseNet.Portal' />
      <typeAlias alias='AbortWorkflowAction' type='SenseNet.ApplicationModel.AbortWorkflowAction, SenseNet.Portal' />
      <!--<typeAlias alias='CustomAction' type='SenseNet.Custom.CustomAction, SenseNet.Custom' />-->
      <typeAlias alias='singleton' type='Microsoft.Practices.Unity.ContainerControlledLifetimeManager, Microsoft.Practices.Unity' />
    </typeAliases>
    <containers>
      <container name='Providers'>
        <types>
          <type type='ActionBase' mapTo='UserProfileAction' name='UserProfileAction' />
          <type type='ActionBase' mapTo='ShareAction' name='ShareAction' />
          <type type='IApplicationCache' mapTo='IApplicationCacheImpl' />
          <type type='ISearchEngine' mapTo='ISearchEngineImpl' />
          <type type='ActionBase' mapTo='ActionBase' name='ActionBase' />
          <type type='ActionBase' mapTo='UrlAction' name='UrlAction' />
          <type type='ActionBase' mapTo='UploadAction' name='UploadAction' />
          <type type='ActionBase' mapTo='ServiceAction' name='ServiceAction' />
          <type type='ActionBase' mapTo='ClientAction' name='ClientAction' />
          <type type='ActionBase' mapTo='WebdavOpenAction' name='WebdavOpenAction' />
          <type type='ActionBase' mapTo='WebdavBrowseAction' name='WebdavBrowseAction' />
          <type type='ActionBase' mapTo='ContentTypeAction' name='ContentTypeAction' />
          <type type='ActionBase' mapTo='CopyToAction' name='CopyToAction' />
          <type type='ActionBase' mapTo='CopyBatchAction' name='CopyBatchAction' />
          <type type='ActionBase' mapTo='MoveToAction' name='MoveToAction' />
          <type type='ActionBase' mapTo='MoveBatchAction' name='MoveBatchAction' />
          <type type='ActionBase' mapTo='DeleteBatchAction' name='DeleteBatchAction' />
          <type type='ActionBase' mapTo='ContentLinkBatchAction' name='ContentLinkBatchAction' />
          <type type='ActionBase' mapTo='CopyAppLocalAction' name='CopyAppLocalAction' />
          <type type='ActionBase' mapTo='DeleteLocalAppAction' name='DeleteLocalAppAction' />
          <type type='ActionBase' mapTo='BinarySpecialAction' name='BinarySpecialAction' />
          <type type='ActionBase' mapTo='WorkflowsAction' name='WorkflowsAction' />
          <type type='ActionBase' mapTo='AbortWorkflowAction' name='AbortWorkflowAction' />
          <type type='ActionBase' mapTo='ApproveAction' name='ApproveAction' />
          <type type='ActionBase' mapTo='DenyAction' name='DenyAction' />
          <type type='ActionBase' mapTo='SoftLinkBrowseAction' name='SoftLinkBrowseAction' />
          <type type='ActionBase' mapTo='CustomizationSimpleLinkBrowseAction' name='CustomizationSimpleLinkBrowseAction' />
          <type type='ActionBase' mapTo='LinkBrowseAction' name='LinkBrowseAction' />
          <type type='PurgeUrlCollector' mapTo='PurgeUrlCollector'>
            <lifetime type='singleton' />
          </type>
          <type type='ActionBase' mapTo='PurgeFromProxyAction' name='PurgeFromProxyAction' />
          <type name='ViewManager' type='IViewManager' mapTo='ViewManager'>
            <lifetime type='singleton' />
          </type>
          <!--<type type='ActionBase' mapTo='CustomAction' name='CustomAction' />-->
          <type name='GenericScenario' type='GenericScenario' mapTo='GenericScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='Actions' type='GenericScenario' mapTo='GenericScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='WorkspaceActions' type='GenericScenario' mapTo='WorkspaceActions'>
            <lifetime type='singleton' />
          </type>
          <type name='ListActions' type='GenericScenario' mapTo='ListActions'>
            <lifetime type='singleton' />
          </type>
          <type name='New' type='GenericScenario' mapTo='New'>
            <lifetime type='singleton' />
          </type>
          <type name='ListItem' type='GenericScenario' mapTo='ListItem'>
            <lifetime type='singleton' />
          </type>
          <type name='Settings' type='GenericScenario' mapTo='Settings'>
            <lifetime type='singleton' />
          </type>
          <type name='Views' type='GenericScenario' mapTo='Views'>
            <lifetime type='singleton' />
          </type>
          <type name='AddField' type='GenericScenario' mapTo='AddField'>
            <lifetime type='singleton' />
          </type>
          <type name='SurveyScenario' type='GenericScenario' mapTo='SurveyScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='QuizSurveyScenario' type='GenericScenario' mapTo='QuizSurveyScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='SurveySettingsScenario' type='GenericScenario' mapTo='SurveySettingsScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='VotingSettingsScenario' type='GenericScenario' mapTo='VotingSettingsScenario'>
            <lifetime type='singleton' />
          </type>
          <type name='VotingAddFieldScenario' type='GenericScenario' mapTo='VotingAddFieldScenario'>
            <lifetime type='singleton' />
          </type>
        </types>
      </container>
    </containers>
  </unity>
  <sensenet>
    <providers>
      <add key='AccessProvider' value='SenseNet.ContentRepository.Security.UserAccessProvider' />
      <add key='DataProvider' value='SenseNet.ContentRepository.Storage.Data.SqlClient.SqlProvider' />
      <add key='MembershipExtender' value='SenseNet.ContentRepository.Storage.Security.DefaultMembershipExtender' />
    </providers>
  </sensenet>
</configuration>
";
            #endregion
            #region expected = ...
            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <sectionGroup name='sensenet'>
      <section name='providers' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <sensenet>
    <providers>
      <add key='AccessProvider' value='SenseNet.ContentRepository.Security.UserAccessProvider' />
      <add key='DataProvider' value='SenseNet.ContentRepository.Storage.Data.SqlClient.SqlProvider' />
      <add key='MembershipExtender' value='SenseNet.ContentRepository.Storage.Security.DefaultMembershipExtender' />
      <add key='ViewManager' value='SenseNet.Portal.UI.ContentListViews.ViewManager' />
      <add key='PurgeUrlCollector' value='SenseNet.Portal.Virtualization.PurgeUrlCollector' />
      <add key='ApplicationCache' value='SenseNet.ContentRepository.ApplicationCache' />
    </providers>
  </sensenet>
</configuration>";
            #endregion

            StepTest(config, expected, (step, configXml) =>
            {
                new PrivateObject(step).Invoke("Execute", configXml);
            });
        }

        private UpgradeProviderConfiguration CreateStep(string stepElementString)
        {
            var manifestXml = new XmlDocument();
            manifestXml.LoadXml($@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            {stepElementString}
                        </Steps>
                    </Package>");
            var manifest = Manifest.Parse(manifestXml, 0, true, new PackageParameter[0]);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, 0, manifest.CountOfPhases, null, null);
            var stepElement = (XmlElement)manifestXml.SelectSingleNode("/Package/Steps/UpgradeProviderConfiguration");
            var result = (UpgradeProviderConfiguration)Step.Parse(stepElement, 0, executionContext);
            return result;
        }

        private void StepTest(string config, string expectedConfig, Action<Step, XmlDocument> callback)
        {
            var xml = new XmlDocument();
            xml.LoadXml(config);

            var step = CreateStep("<UpgradeProviderConfiguration file='./web.config' />");
            callback(step, xml);

            var expected = expectedConfig.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").Replace("\"", "'");
            var actual = xml.OuterXml.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").Replace("\"", "'");

            Assert.IsTrue(expected == actual, $"Actual: {xml.OuterXml}");
        }

    }
}
