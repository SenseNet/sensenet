using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging;
using SenseNet.Packaging.Tools;
using SenseNet.Portal.Handlers;

namespace SenseNet.WebHooks
{
    /// <summary>
    /// Defines an installer and future patches for the WebHooks component.
    /// The installer uses resources stored in this library as embedded resource.
    /// </summary>
    public class WebHookComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.WebHooks";

        public override void AddPatches(PatchBuilder builder)
        {
            builder
                .Install("0.0.1.3", "2021-03-12", "sensenet WebHooks")
                .DependsOn("SenseNet.Services", "7.7.18")
                .Action(context =>
                {
                    #region String resource

                    InstallStringResource("CtdResourcesWebHookSubscription.xml");
                    InstallStringResource("CtdResourcesWebHookSubscriptionList.xml");
                    
                    #endregion

                    #region install CTD

                    InstallCtd("WebHookSubscriptionCtd.xml");
                    InstallCtd("WebHookSubscriptionListCtd.xml");

                    #endregion

                    #region Content items

                    RepositoryTools.CreateStructure("/Root/System/WebHooks", "SystemFolder");

                    #endregion
                });

            builder.Patch("0.0.1", "0.0.1.3", "2021-03-12", "Upgrades the WebHook component")
                .DependsOn("SenseNet.Services", "7.7.18")
                .Action(context =>
                {
                    #region String resources

                    InstallStringResource("CtdResourcesWebHookSubscriptionList.xml");

                    #endregion

                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("WebHookSubscription")
                        .Icon("Settings")
                        .Field("WebHookPayload", "LongText")
                        .DisplayName("$Ctd-WebHookSubscription,WebHookPayload-DisplayName")
                        .Description("$Ctd-WebHookSubscription,WebHookPayload-Description")
                        .VisibleBrowse(FieldVisibility.Show)
                        .VisibleEdit(FieldVisibility.Show)
                        .VisibleNew(FieldVisibility.Show)
                        .FieldIndex(0)
                        .ControlHint("sn:WebhookPayload");

                    cb.Apply();

                    InstallCtd("WebHookSubscriptionListCtd.xml");

                    #endregion
                });
        }

        private static void InstallStringResource(string resourceContentName)
        {
            var assembly = typeof(WebHookComponent).Assembly;
            var resourcePrefix = assembly.GetName().Name;

            // install a string resource stored as an embedded resource
            var resourcePath = resourcePrefix + ".import.Localization." + resourceContentName;
            const string parentPath = "/Root/Localization";

            using (var resourceStream = assembly.GetManifestResourceStream(resourcePath))
            {
                var parent = RepositoryTools.CreateStructure(parentPath, "Resources") ??
                             Content.Load(parentPath);
                var whResourcePath = RepositoryPath.Combine(parentPath, resourceContentName);
                var whResource = Node.Load<Resource>(whResourcePath) ??
                                 new Resource(parent.ContentHandler)
                                 {
                                     Name = resourceContentName
                                 };
                whResource.Binary = UploadHelper.CreateBinaryData(resourceContentName, resourceStream);
                whResource.Save(SavingMode.KeepVersion);
            }
        }
        private static void InstallCtd(string ctdFileName)
        {
            var assembly = typeof(WebHookComponent).Assembly;
            var resourcePrefix = assembly.GetName().Name;

            // install a CTD stored as an embedded resource
            var resourceCtdPath = resourcePrefix + ".import.System.Schema.ContentTypes." + ctdFileName;

            using (var ctdStream = assembly.GetManifestResourceStream(resourceCtdPath))
            {
                ContentTypeInstaller.InstallContentType(ctdStream);
            }
        }
    }
}
