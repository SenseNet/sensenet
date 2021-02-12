using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging;
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
                .Install("1.0.0", "2021-02-11", "sensenet WebHooks")
                .DependsOn("SenseNet.Services", "7.7.17")
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

                    #region install CTD

                    // install a CTD stored as an embedded resource
                    const string resourceCtd = "import.System.Schema.ContentTypes.WebHookSubscriptionCtd.xml";

                    using (var ctdStream = assembly.GetManifestResourceStream(
                        resourcePrefix + "." + resourceCtd))
                    {
                        ContentTypeInstaller.InstallContentType(ctdStream);
                    }
                    #endregion
                });
        }
    }
}
