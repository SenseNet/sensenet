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
            var assembly = typeof(WebHookComponent).Assembly;

            builder
                .Install("0.0.1.2", "2021-03-11", "sensenet WebHooks")
                .DependsOn("SenseNet.Services", "7.7.18")
                .Action(context =>
                {
                    var resourcePrefix = assembly.GetName().Name;

                    #region String resource

                    // install a string resource stored as an embedded resource
                    const string resourceStringResource = "import.Localization.CtdResourcesWebHookSubscription.xml";

                    using (var stringResourceStream = assembly.GetManifestResourceStream(
                        resourcePrefix + "." + resourceStringResource))
                    {
                        const string parentPath = "/Root/Localization";
                        const string whResourceName = "CtdResourcesWebHookSubscription.xml";

                        var parent = RepositoryTools.CreateStructure(parentPath, "Resources") ??
                                     Content.Load(parentPath);
                        var whResourcePath = RepositoryPath.Combine(parentPath, whResourceName);
                        var whResource = Node.Load<Resource>(whResourcePath) ??
                                         new Resource(parent.ContentHandler)
                                         {
                                             Name = "CtdResourcesWebHookSubscription.xml"
                                         };
                        whResource.Binary = UploadHelper.CreateBinaryData(whResourceName, stringResourceStream);
                        whResource.Save(SavingMode.KeepVersion);
                    }
                    #endregion

                    #region Install CTD

                    // install a CTD stored as an embedded resource
                    const string resourceCtd = "import.System.Schema.ContentTypes.WebHookSubscriptionCtd.xml";

                    using (var ctdStream = assembly.GetManifestResourceStream(
                        resourcePrefix + "." + resourceCtd))
                    {
                        ContentTypeInstaller.InstallContentType(ctdStream);
                    }
                    #endregion

                    #region Content items

                    var webHooks = RepositoryTools.CreateStructure("/Root/System/WebHooks", 
                        "SystemFolder");

                    #endregion
                });

            builder.Patch("0.0.1", "0.0.1.2", "2021-03-11", "Upgrades the WebHook component")
                .DependsOn("SenseNet.Services", "7.7.18.1")
                .Action(context =>
                {
                    #region String resource

                    var rb = new ResourceBuilder();

                    rb.Content("CtdResourcesWebHookSubscription.xml")
                        .Class("Ctd-WebHookSubscription")
                        .Culture("en")
                        .AddResource("DisplayName", "Webhook")
                        .Culture("hu")
                        .AddResource("DisplayName", "Webhook");

                    rb.Apply();

                    #endregion

                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("WebHookSubscription")
                        .Field("WebHookPayload", "LongText")
                        .DisplayName("$Ctd-WebHookSubscription,WebHookPayload-DisplayName")
                        .Description("$Ctd-WebHookSubscription,WebHookPayload-Description")
                        .VisibleBrowse(FieldVisibility.Show)
                        .VisibleEdit(FieldVisibility.Show)
                        .VisibleNew(FieldVisibility.Show)
                        .FieldIndex(10)
                        .ControlHint("sn:WebhookPayload")
                        .Field("WebHookFilter")
                        .FieldIndex(50)
                        .Field("WebHookHeaders")
                        .FieldIndex(40)
                        .Field("Enabled")
                        .FieldIndex(90)
                        .Field("IsValid")
                        .FieldIndex(30)
                        .Field("InvalidFields")
                        .RemoveConfiguration("FieldIndex")
                        .VisibleBrowse(FieldVisibility.Hide)
                        .VisibleEdit(FieldVisibility.Hide)
                        .VisibleNew(FieldVisibility.Hide)
                        .Field("SuccessfulCalls")
                        .FieldIndex(20);

                    cb.Apply();

                    #endregion
                });
        }
    }
}
