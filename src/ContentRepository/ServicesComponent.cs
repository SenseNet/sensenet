using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging;
using SenseNet.Packaging.Tools;

namespace SenseNet.ContentRepository
{
    internal class ServicesComponent : SnComponent
    {
        public override string ComponentId => "SenseNet.Services";

        //TODO: Set SupportedVersion before release.
        // This value has to change if there were database, content
        // or configuration changes since the last release that
        // should be enforced using an upgrade patch.
        public override Version SupportedVersion => new Version(7, 7, 0);

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Patch("7.7.11", "7.7.12", "2020-09-7", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("Settings")
                        .Field("Description", "LongText")
                        .VisibleBrowse(FieldVisibility.Hide)
                        .VisibleEdit(FieldVisibility.Show)
                        .VisibleNew(FieldVisibility.Show);

                    cb.Apply();

                    #endregion

                    #region Settings Description field changes

                    SetSettingsDescription("Indexing", "In this Settings file you can customize the indexing behavior (for example the text extractor used in case of different file types) of the system.");
                    SetSettingsDescription("Logging", "Contains logging-related settings, for example which events are sent to the trace. You can control tracing by category: switch on or off writing messages in certain categories to the trace channel.");
                    SetSettingsDescription("MailProcessor", "The content list Inbox feature requires an Exchange or POP3 server configuration and other settings related to connecting libraries to a mailbox.");
                    SetSettingsDescription("OAuth", "When users log in using one of the configured OAuth providers (like Google or Facebook), these settings control the type and place of the newly created users.");
                    SetSettingsDescription("OfficeOnline", "To open or edit Office documents in the browser, the system needs to know the address of the Office Online Server that provides the user interface for the feature. In this section you can configure that and other OOS-related settings.");
                    SetSettingsDescription("Sharing", "Content sharing related options.");
                    SetSettingsDescription("TaskManagement", "When the Task Management module is installed, this is the place where you can configure the connection to the central task management service.");
                    SetSettingsDescription("UserProfile", "When a user is created, and the profile feature is enabled (in the app configuration), they automatically get a profile – a workspace dedicated to the user’s personal documents and tasks. In this setting section you can customize the content type and the place of this profile.");

                    void SetSettingsDescription(string name, string description)
                    {
                        var settings = Content.Load("/Root/System/Settings/" + name + ".settings");
                        if (settings == null)
                            return;
                        settings["Description"] = description;
                        settings.SaveSameVersion();
                    }

                    #endregion

                    #region App changes

                    var app1 = Content.Load("/Root/(apps)/Folder/Add");
                    if (app1 != null)
                    {
                        app1["Scenario"] = string.Empty;
                        app1.SaveSameVersion();
                    }

                    var app2 = Content.Load("/Root/(apps)/ContentType/Edit");
                    if (app2 == null)
                    {
                        var parent = RepositoryTools.CreateStructure("/Root/(apps)/ContentType") ??
                                     Content.Load("/Root/(apps)/ContentType");

                        app2 = Content.CreateNew("ClientApplication", parent.ContentHandler, "Edit");
                        app2["DisplayName"] = "$Action,Edit";
                        app2["Scenario"] = "ContextMenu";
                        app2["Icon"] = "edit";
                        app2["RequiredPermissions"] = "See;Open;OpenMinor;Save";
                        app2.Save();

                        // set app permissions
                        var developersGroupId = NodeHead.Get("/Root/IMS/BuiltIn/Portal/Developers")?.Id ?? 0;
                        var aclEditor = SecurityHandler.SecurityContext.CreateAclEditor();
                        aclEditor
                            .Allow(app2.Id, Identifiers.AdministratorsGroupId,
                                false, PermissionType.RunApplication);
                        if (developersGroupId > 0)
                            aclEditor.Allow(app2.Id, developersGroupId,
                                false, PermissionType.RunApplication);

                        aclEditor.Apply();
                    }

                    #endregion
                });

            builder.Patch("7.7.12", "7.7.13", "2020-09-23", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region String resources

                    var rb = new ResourceBuilder();

                    rb.Content("CtdResourcesAB.xml")
                        .Class("Ctd-BinaryFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Binary field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Bináris mező");

                    rb.Content("CtdResourcesCD.xml")
                        .Class("Ctd-ChoiceFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Choice field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Választó mező")
                        .Class("Ctd-CurrencyFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Currency field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Pénzérték mező")
                        .Class("Ctd-DateTimeFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "DateTime field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Dátum mező");

                    rb.Content("CtdResourcesEF.xml")
                        .Class("Ctd-FieldControlTemplate")
                        .Culture("en")
                        .AddResource("DisplayName", "FieldControlTemplate")
                        .AddResource("Description", "A type for FieldControl templates.")
                        .Culture("hu")
                        .AddResource("DisplayName", "Mező vezérlő sablon")
                        .AddResource("Description", "Mező vezérlő sablont tároló fájl")
                        .Class("Ctd-FieldControlTemplates")
                        .Culture("en")
                        .AddResource("DisplayName", "FieldControlTemplates")
                        .AddResource("Description", "This is the container type for ContentViews. Instances are allowed only at /Root/Global/fieldcontroltemplates.")
                        .Culture("hu")
                        .AddResource("DisplayName", "Mező vezérlő sablonok")
                        .AddResource("Description", "Mező vezérlő sablonokat tároló mappa. Csak egy lehet, itt: /Root/Global/fieldcontroltemplates.");
                    
                    rb.Content("CtdResourcesGH.xml")
                        .Class("Ctd-HyperLinkFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Hyperlink field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Hivatkozás mező");


                    rb.Content("CtdResourcesIJK.xml")
                        .Class("Ctd-Image")
                        .Culture("en")
                        .AddResource("DisplayName", "Image")
                        .AddResource("Name-DisplayName", "Name")
                        .Culture("hu")
                        .AddResource("DisplayName", "Kép")
                        .AddResource("Name-DisplayName", "Név")
                        .Class("Ctd-IntegerFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Integer field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Egész szám mező");

                    rb.Content("CtdResourcesLM.xml")
                        .Class("Ctd-LongTextFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Longtext field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Hosszú szöveges mező");

                    rb.Content("CtdResourcesNOP.xml")
                        .Class("Ctd-NullFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Null field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Null mező")
                        .Class("Ctd-NumberFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Number field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Szám mező")
                        .Class("Ctd-PasswordFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Password field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Jelszó mező")
                        .Class("Ctd-PermissionChoiceFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Permission choice field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Jogosultság választó mező");

                    rb.Content("CtdResourcesRS.xml")
                        .Class("Ctd-ReferenceFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Reference field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Referencia mező")
                        .Class("Ctd-ShortTextFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "ShortText field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Rövid szöveges mező");

                    rb.Content("CtdResourcesTZ.xml")
                        .Class("Ctd-Workspace")
                        .Culture("en")
                        .AddResource("Name-DisplayName", "Name")
                        .Culture("hu")
                        .AddResource("Name-DisplayName", "Név")
                        .Class("Ctd-TextFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Text field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Szöveges mező")
                        .Class("Ctd-UserControl")
                        .Culture("en")
                        .AddResource("DisplayName", "User control")
                        .AddResource("Description", "A type for storing ASP.NET user controls.")
                        .Culture("hu")
                        .AddResource("DisplayName", "Egyéni vezérlőelem")
                        .AddResource("Description", "ASP.NET user control tárolására.")
                        .Class("Ctd-ViewBase")
                        .Culture("en")
                        .AddResource("DisplayName", "View base")
                        .AddResource("Description", "An abstract type for ContentList views.")
                        .AddResource("IsDefault-DisplayName", "Default")
                        .AddResource("IsDefault-Description",
                            "Whether this is the default view on the parent ContentList.")
                        .AddResource("Template-DisplayName", "Markup template")
                        .AddResource("Template-Description", "The Xslt template used to generate the view.")
                        .AddResource("FilterXml-DisplayName", "Filtering")
                        .AddResource("FilterXml-Description", "Define filtering rules for the view.")
                        .AddResource("EnableAutofilters-DisplayName", "Enable autofilters")
                        .AddResource("EnableAutofilters-Description",
                            "If autofilters are enabled system content will be filtered from the query.")
                        .AddResource("EnableLifespanFilter-DisplayName", "Enable lifespan filter")
                        .AddResource("EnableLifespanFilter-Description",
                            "If lifespan filter is enabled only valid content will be in the result.")
                        .AddResource("Hidden-Description",
                            "The view won't show in the selector menu if checked. (If unsure, leave unchecked).")
                        .AddResource("QueryTop-DisplayName", "Top")
                        .AddResource("QueryTop-Description",
                            "If you do not want to display all content please specify here a value greater than 0.")
                        .AddResource("QuerySkip-DisplayName", "Skip")
                        .AddResource("QuerySkip-Description",
                            "If you do not want to display the first several content please specify here a value greater than 0.")
                        .AddResource("Icon-DisplayName", "Icon identifier")
                        .AddResource("Icon-Description", "The string identifier of the View's icon.")
                        .Culture("hu")
                        .AddResource("DisplayName", "View base")
                        .AddResource("Description", "Minden view (listanézet) őse.")
                        .AddResource("IsDefault-DisplayName", "Alapértelmezett")
                        .AddResource("IsDefault-Description", "Legyen ez az alapértelmezett listanézet..")
                        .AddResource("Template-DisplayName", "Sablon")
                        .AddResource("Template-Description", "A listát generáló xslt sablon.")
                        .AddResource("FilterXml-DisplayName", "Szűrés")
                        .AddResource("FilterXml-Description", "A listanézet szűrési feltételei.")
                        .AddResource("EnableAutofilters-DisplayName", "Automata szűrések bekapcsolása")
                        .AddResource("EnableAutofilters-Description",
                            "Ha be van kapcsolva, a rendszer fájlok kiszűrésre kerülnek.")
                        .AddResource("EnableLifespanFilter-DisplayName", "Élettartam szűrés")
                        .AddResource("EnableLifespanFilter-Description",
                            "Ha be van kapcsolva, akkor csak az időben aktuális elemek jelennek meg.")
                        .AddResource("Hidden-Description",
                            "Ha be van pipálva, akkor a listanézet nem jelenik meg a választhatók között a felületen.")
                        .AddResource("QueryTop-DisplayName", "Elemszám")
                        .AddResource("QueryTop-Description",
                            "Ha nem akarja az összes elemet megjeleníteni, írjon be egy nullánál nagyobb számot.")
                        .AddResource("QuerySkip-DisplayName", "Kihagyott elemek")
                        .AddResource("QuerySkip-Description",
                            "Ha a lista első valahány elemét ki szeretné hagyni a megjelenítésből, írja be az elhagyni kívánt elemek számát.")
                        .AddResource("Icon-DisplayName", "Ikon azonosító")
                        .AddResource("Icon-Description", "Név amely a listanézet ikonját azonosítja.")
                        .Class("Ctd-WebContent")
                        .Culture("en")
                        .AddResource("DisplayName", "Web Content (structured web content)")
                        .AddResource("Description", "Web Content is the base type for structured web content.")
                        .AddResource("ReviewDate-DisplayName", "Review date")
                        .AddResource("ReviewDate-Description", "")
                        .AddResource("ArchiveDate-DisplayName", "Archive date")
                        .AddResource("ArchiveDate-Description", "")
                        .Culture("hu")
                        .AddResource("DisplayName", "Webes tartalom")
                        .AddResource("Description", "")
                        .AddResource("ReviewDate-DisplayName", "Ellenőrzés dátuma")
                        .AddResource("ReviewDate-Description", "")
                        .AddResource("ArchiveDate-DisplayName", "Archiválás dátuma")
                        .AddResource("ArchiveDate-Description", "")
                        .Class("Ctd-XmlFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Xml field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Xml mező")
                        .Class("Ctd-XsltApplication")
                        .Culture("en")
                        .AddResource("DisplayName", "Xslt application")
                        .AddResource("Description", "Xslt rendering application.")
                        .AddResource("Binary-DisplayName", "Xslt template")
                        .AddResource("Binary-Description", "Upload or enter the Xslt template to be used in rendering.")
                        .AddResource("MimeType-DisplayName", "MIME type")
                        .AddResource("MimeType-Description",
                            "Sets HTTP MIME type of the output stream. Default value: application/xml.")
                        .AddResource("OmitXmlDeclaration-DisplayName", "OmitXmlDeclaration")
                        .AddResource("OmitXmlDeclaration-Description",
                            "Sets a value indicating whether to write XML declaration.")
                        .AddResource("ResponseEncoding-DisplayName", "Response encoding")
                        .AddResource("ResponseEncoding-Description",
                            "Sets the text encoding to use. Default value: UTF-8.")
                        .AddResource("WithChildren-DisplayName", "With children")
                        .AddResource("WithChildren-Description",
                            "Sets a value indicating whether to render content with all children Default value: true.")
                        .AddResource("Cacheable-DisplayName", "Application is cached")
                        .AddResource("Cacheable-Description",
                            "If set the output of the application will be cached. <div class='ui-helper-clearfix sn-dialog-editportlet-warning'><img class='sn-icon sn-icon16 sn-floatleft' src='/Root/Global/images/icons/16/warning.png' /><i>Switching off application cache may cause performance issues!</i></div>")
                        .AddResource("CacheableForLoggedInUser-DisplayName",
                            "Application is cached for logged in users")
                        .AddResource("CacheableForLoggedInUser-Description",
                            "If set the output of the application will be cached for logged in users. <div class='ui-helper-clearfix sn-dialog-editportlet-warning'><img class='sn-icon sn-icon16 sn-floatleft' src='/Root/Global/images/icons/16/warning.png' /><i>Switching off application cache may cause performance issues!</i></div>")
                        .AddResource("CacheByPath-DisplayName", "Request path influences caching")
                        .AddResource("CacheByPath-Description",
                            "Defines whether the requested content path is included in the cache key. When unchecked application output is preserved regardless of the page's current context content or request path. Check it if you want to cache application output depending on the requested context content.")
                        .AddResource("CacheByParams-DisplayName", "Url query params influence caching")
                        .AddResource("CacheByParams-Description",
                            "Defines whether the url query params are also included in the cache key. When unchecked application output is preserved regardless of changing url params.")
                        .AddResource("CacheByLanguage-DisplayName", "Language influences caching")
                        .AddResource("CacheByLanguage-Description",
                            "Defines whether the language code is also included in the cache key. When unchecked application output is preserved regardless of the language that the users use to browse the site.")
                        .AddResource("CacheByHost-DisplayName", "Host influences caching")
                        .AddResource("CacheByHost-Description",
                            "Defines whether the URL-host (e.g. 'example.com') is also included in the cache key. When unchecked application output is preserved regardless of the host that the users use to browse the site.")
                        .AddResource("AbsoluteExpiration-DisplayName", "Absolute expiration")
                        .AddResource("AbsoluteExpiration-Description",
                            "Given in seconds. The application will be refreshed periodically with the given time period. -1 means that the value is defined by 'AbsoluteExpirationSeconds' setting in the web.config.")
                        .AddResource("SlidingExpirationMinutes-DisplayName", "Sliding expiration")
                        .AddResource("SlidingExpirationMinutes-Description",
                            "Given in seconds. The application is refreshed when it has not been accessed for the given seconds. -1 means that the value is defined by 'SlidingExpirationSeconds' setting in the web.config.")
                        .AddResource("CustomCacheKey-DisplayName", "Custom cache key")
                        .AddResource("CustomCacheKey-Description",
                            "Defines a custom cache key independent of requested path and query params. Useful when the same static output is rendered at various pages. <div class='ui-helper-clearfix sn-dialog-editportlet-warning'><img class='sn-icon sn-icon16 sn-floatleft' src='/Root/Global/images/icons/16/warning.png' /><i>For experts only! Leave empty if unsure.</i></div>")
                        .Culture("hu")
                        .AddResource("DisplayName", "Xslt alkalmazás")
                        .AddResource("Description",
                            "Alkalmazás, amely XSLT segítségével jeleníti meg az aktuális tartalmat.")
                        .AddResource("Binary-DisplayName", "Xslt sablon")
                        .AddResource("Binary-Description", "Adja meg az xslt sablont a megjelenítéshez.")
                        .AddResource("MimeType-DisplayName", "MIME type")
                        .AddResource("MimeType-Description",
                            "Beállítja a kimeneti stream HTTP MIME type-ját. Alapértelmezett érték: <i>alkalmazás/xml</i>.")
                        .AddResource("OmitXmlDeclaration-DisplayName", "Xml deklaráció kihagyása")
                        .AddResource("OmitXmlDeclaration-Description",
                            "Megadhatja, hogy kihagyjuk-e az xml deklarációt.")
                        .AddResource("ResponseEncoding-DisplayName", "Kimeneti stream kódolás")
                        .AddResource("ResponseEncoding-Description",
                            "Beállítja a szöveg kódolását. Alapértelmezett érték: <i>UTF-8</i>.")
                        .AddResource("WithChildren-DisplayName", "Gyermek tartalmak")
                        .AddResource("WithChildren-Description",
                            "Ha be van állítva, a gyermek elemek is szerepelni fognak az oldalon.")
                        .AddResource("Cacheable-DisplayName", "Az oldal kerüljön be a gyorsítótárba")
                        .AddResource("Cacheable-Description",
                            "Ha be van állítva, az oldal kimenete bekerül a gyorsítótárba<div class='ui-helper-clearfix sn-dialog-editportlet-warning'><img class='sn-icon sn-icon16 sn-floatleft' src='/Root/Global/images/icons/16/warning.png' /><i>Ennek kikapcsolása nagy terhelés alatt sebesség-problémákat okozhat!</i></div>")
                        .AddResource("CacheableForLoggedInUser-DisplayName",
                            "Az oldal kerüljön be a gyorsítótárba belépett felhasználók számára.")
                        .AddResource("CacheableForLoggedInUser-Description",
                            "Ha be van állítva, az oldal kimenete bekerül a gyorsítótárba belépett felhasználók számára. <div class='ui-helper-clearfix sn-dialog-editportlet-warning'><img class='sn-icon sn-icon16 sn-floatleft' src='/Root/Global/images/icons/16/warning.png' /><i>Ennek kikapcsolása nagy terhelés alatt sebesség-problémákat okozhat!</i></div>")
                        .AddResource("CacheByPath-DisplayName", "Az aktuális tartalom befolyásolja a cache-elést")
                        .AddResource("CacheByPath-Description",
                            "Ha be van állítva, a kért tartalom útvonala (Path) befolyásolja a gyorsítótárat. Ha nincs, az oldal kimenete ugyanaz lesz, függetlenül az aktuális tartalomtól.")
                        .AddResource("CacheByParams-DisplayName", "URL paraméterek befolyásolják a gyorsítótárat")
                        .AddResource("CacheByParams-Description",
                            "Ha nincs bekapcsolva, az oldal kimenete ugyanaz lesz, függetlenül az URL paraméterektől.")
                        .AddResource("CacheByLanguage-DisplayName", "A nyelv befolyásolja a gyorsítótárat")
                        .AddResource("CacheByLanguage-Description",
                            "Ha nincs bekapcsolva, az oldal kimenete ugyanaz lesz, függetlenül attól, hogy a felhasználó milyen nyelven nézi az oldalt.")
                        .AddResource("CacheByHost-DisplayName", "A host befolyásolja a gyorsítótárat")
                        .AddResource("CacheByHost-Description",
                            "Ha nincs bekapcsolva, az oldal kimenete ugyanaz lesz, függetlenül attól, hogy a felhasználó milyen url-host-ról nézi az oldalt.")
                        .AddResource("AbsoluteExpiration-DisplayName", "Abszolút gyorsítótár lejárat")
                        .AddResource("AbsoluteExpiration-Description",
                            "Másodpercben megadott lejárat. Az oldal rendszeresen frissülni fog a megadott idő után. <br />-1 azt jelenti, hogy az értéket az <i>AbsoluteExpirationSeconds</i> web.config beállítás határozza meg.")
                        .AddResource("SlidingExpirationMinutes-DisplayName", "Csúszó gyorsítótár lejárat")
                        .AddResource("SlidingExpirationMinutes-Description",
                            "Másodpercben megadott lejárat. Az oldal frissülni fog, ha nem érkezett rá kérés a megadott időn belül. <br />-1 azt jelenti, hogy az értéket az <i>SlidingExpirationSeconds</i> web.config beállítás határozza meg.")
                        .AddResource("CustomCacheKey-DisplayName", "Egyedi gyorsítótár (cache) kulcs")
                        .AddResource("CustomCacheKey-Description",
                            "Megadhat egyedi gyorsítótár kulcsot, függetlenül az aktuális tartalomtól és URL paraméterektől. Akkor érdemes használni, ha ugyanazt a statikus tartalmat szeretné megjeleníteni több különböző oldalon. <div class='ui-helper-clearfix sn-dialog-editportlet-warning'><img class='sn-icon sn-icon16 sn-floatleft' src='/Root/Global/images/icons/16/warning.png' /><i>Csak adminisztrátoroknak. Hagyja üresen, ha nem biztos a dologban.</i></div>")
                        .Class("Ctd-YesNoFieldSetting")
                        .Culture("en")
                        .AddResource("DisplayName", "Yes/No field")
                        .Culture("hu")
                        .AddResource("DisplayName", "Igen/nem mező");

                    rb.Apply();

                    #endregion

                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("CalendarEvent")
                        .Field("StartDate")
                        .DefaultValue("@@currenttime@@")
                        .Field("EndDate")
                        .DefaultValue("@@currenttime@@");
                    cb.Type("GenericContent")
                        .Field("ValidFrom")
                        .DefaultValue("@@currenttime@@")
                        .Field("ValidTill")
                        .DefaultValue("@@currenttime@@");

                    cb.Type("Link")
                        .Field("Url")
                        .DefaultValue("https://");

                    cb.Type("User")
                        .Field("LoginName")
                        .VisibleEdit(FieldVisibility.Hide)
                        .Field("Email")
                        .VisibleBrowse(FieldVisibility.Show)
                        .VisibleEdit(FieldVisibility.Hide)
                        .VisibleNew(FieldVisibility.Show)
                        .Field("BirthDate")
                        .DefaultValue("@@currenttime@@");

                    cb.Type("Group")
                        .Field("AllRoles")
                        .VisibleBrowse(FieldVisibility.Hide)
                        .VisibleEdit(FieldVisibility.Hide)
                        .VisibleNew(FieldVisibility.Hide)
                        .Field("DirectRoles")
                        .VisibleBrowse(FieldVisibility.Hide)
                        .VisibleEdit(FieldVisibility.Hide)
                        .VisibleNew(FieldVisibility.Hide);

                    cb.Apply();

                    #endregion

                    #region Permission changes

                    SecurityHandler.SecurityContext.CreateAclEditor()
                        .Allow(NodeHead.Get("/Root/Localization").Id, Identifiers.OwnersGroupId, false, 
                            PermissionType.Save, PermissionType.Delete)
                        .Apply();

                    #endregion
                });

            builder.Patch("7.7.13", "7.7.14", "2020-11-19", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("ListItem")
                        .Field("ModifiedBy")
                        .VisibleEdit(FieldVisibility.Hide);

                    cb.Type("User")
                        .Field("BirthDate")
                        .DefaultValue("");

                    cb.Apply();

                    #endregion
                });

            builder.Patch("7.7.14", "7.7.16", "2020-12-08", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    // We can set the new regex only if the current regex is the old default
                    // (otherwise we do not want to overwrite a custom regex).
                    // NOTE: in the old regex below the & character appears as is (in the middle
                    // of the first line), but in the new const we had to replace it with &amp;
                    // to let the patch algorithm set the correct value in the XML.

                    const string oldUrlRegex = "^(http|https)\\://([a-zA-Z0-9\\.\\-]+(\\:[a-zA-Z0-9\\.&%\\$\\-]+)*@)*((25[0-5]|" +
                                               "2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}" +
                                               "[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}" +
                                               "[0-9]{1}|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|" +
                                               "localhost|([a-zA-Z0-9\\-]+\\.)*[a-zA-Z0-9\\-]+(\\.(com|edu|gov|int|mil|net|org|biz|" +
                                               "arpa|info|name|pro|aero|coop|museum|hu|[a-zA-Z]{2})){0,1})(\\:[0-9]+)*((\\#|/)($|" +
                                               "[a-zA-Z0-9\\.\\,\\?\\'\\\\\\+&%\\$#\\=~_\\-]+))*$";
                    const string newUrlRegex = "^(https?|ftp)\\://([a-zA-Z0-9\\.\\-]+(\\:[a-zA-Z0-9\\.&amp;%\\$\\-]+)*@)*((25[0-5]|" +
                                               "2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}" +
                                               "[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}" +
                                               "[0-9]{1}|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|" +
                                               "localhost|([a-zA-Z0-9\\-]+\\.)*[a-zA-Z0-9\\-]+(\\.(com|edu|gov|int|mil|net|org|biz|" +
                                               "arpa|info|name|pro|aero|coop|museum|hu|[a-zA-Z]{2})){0,1})(\\:[0-9]+)*" +
                                               "(\\/[\\w\\-\\@\\/\\(\\)]*){0,1}((\\?|\\#)(($|[\\w\\.\\,\\'\\\\\\+&amp;" +
                                               "%\\$#\\=~_\\-\\(\\)]+)*)){0,1}$";

                    var currentRegex =
                        ((ShortTextFieldSetting) ContentType.GetByName("Link").GetFieldSettingByName("Url")).Regex;

                    // replace the regex only if it was the original default
                    if (string.Equals(oldUrlRegex, currentRegex, StringComparison.Ordinal))
                    {
                        cb.Type("Link")
                            .Field("Url")
                            .Configure("Regex", newUrlRegex);
                    }

                    cb.Apply();

                    #endregion

                    #region Content changes

                    // create the new public admin user
                    if (User.PublicAdministrator == null)
                    {
                        var publicAdmin = Content.CreateNew("User", OrganizationalUnit.Portal, "PublicAdmin");
                        publicAdmin["Enabled"] = true;
                        publicAdmin["FullName"] = "PublicAdmin";
                        publicAdmin["LoginName"] = "PublicAdmin";
                        publicAdmin.Save();
                    }

                    #endregion
                });

            builder.Patch("7.7.16", "7.7.17", "2021-01-25", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region CTD changes

                    const string ctDefaultValue = @"&lt;?xml version=""1.0"" encoding=""utf-8""?&gt;
&lt;ContentType name=""MyType"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition""&gt;
  &lt;DisplayName&gt;MyType&lt;/DisplayName&gt;
  &lt;Description&gt;&lt;/Description&gt;
  &lt;Icon&gt;Content&lt;/Icon&gt;
  &lt;AllowIncrementalNaming&gt;true&lt;/AllowIncrementalNaming&gt;
  &lt;AllowedChildTypes&gt;ContentTypeName1,ContentTypeName2&lt;/AllowedChildTypes&gt;
  &lt;Fields&gt;
    &lt;Field name=""ShortTextField"" type=""ShortText""&gt;
      &lt;DisplayName&gt;ShortTextField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;MaxLength&gt;100&lt;/MaxLength&gt;
        &lt;MinLength&gt;0&lt;/MinLength&gt;
        &lt;Regex&gt;[a-zA-Z0-9]*$&lt;/Regex&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""LongTextField"" type=""LongText""&gt;
      &lt;DisplayName&gt;LongTextField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;MaxLength&gt;100&lt;/MaxLength&gt;
        &lt;MinLength&gt;0&lt;/MinLength&gt;
        &lt;TextType&gt;LongText|RichText&lt;/TextType&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""NumberField"" type=""Number""&gt;
      &lt;DisplayName&gt;NumberField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;MinValue&gt;0&lt;/MinValue&gt;
        &lt;MaxValue&gt;100.5&lt;/MaxValue&gt;
        &lt;Digits&gt;2&lt;/Digits&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""IntegerField"" type=""Integer""&gt;
      &lt;DisplayName&gt;IntegerField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;MinValue&gt;0&lt;/MinValue&gt;
        &lt;MaxValue&gt;100&lt;/MaxValue&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""BooleanField"" type=""Boolean""&gt;
      &lt;DisplayName&gt;BooleanField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""ChoiceField"" type=""Choice""&gt;
      &lt;DisplayName&gt;ChoiceField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;AllowMultiple&gt;false&lt;/AllowMultiple&gt;
        &lt;AllowExtraValue&gt;false&lt;/AllowExtraValue&gt;
        &lt;Options&gt;
          &lt;Option selected=""true""&gt;1&lt;/Option&gt;
          &lt;Option&gt;2&lt;/Option&gt;
        &lt;/Options&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""DateTimeField"" type=""DateTime""&gt;
      &lt;DisplayName&gt;DateTimeField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;DateTimeMode&gt;DateAndTime&lt;/DateTimeMode&gt;
        &lt;Precision&gt;Second&lt;/Precision&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""ReferenceField"" type=""Reference""&gt;
      &lt;DisplayName&gt;ReferenceField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;AllowMultiple&gt;true&lt;/AllowMultiple&gt;
        &lt;AllowedTypes&gt;
          &lt;Type&gt;Type1&lt;/Type&gt;
          &lt;Type&gt;Type2&lt;/Type&gt;
        &lt;/AllowedTypes&gt;
        &lt;SelectionRoot&gt;
          &lt;Path&gt;/Root/Path1&lt;/Path&gt;
          &lt;Path&gt;/Root/Path2&lt;/Path&gt;
        &lt;/SelectionRoot&gt;
        &lt;DefaultValue&gt;/Root/Path1,/Root/Path2&lt;/DefaultValue&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
    &lt;Field name=""BinaryField"" type=""Binary""&gt;
      &lt;DisplayName&gt;BinaryField&lt;/DisplayName&gt;
      &lt;Description&gt;&lt;/Description&gt;
      &lt;Configuration&gt;
        &lt;IsText&gt;true&lt;/IsText&gt;
        &lt;ReadOnly&gt;false&lt;/ReadOnly&gt;
        &lt;Compulsory&gt;false&lt;/Compulsory&gt;
        &lt;DefaultValue&gt;&lt;/DefaultValue&gt;
        &lt;VisibleBrowse&gt;Show|Hide&lt;/VisibleBrowse&gt;
        &lt;VisibleEdit&gt;Show|Hide&lt;/VisibleEdit&gt;
        &lt;VisibleNew&gt;Show|Hide&lt;/VisibleNew&gt;
      &lt;/Configuration&gt;
    &lt;/Field&gt;
  &lt;/Fields&gt;
&lt;/ContentType&gt;";
                    
                    var cb = new ContentTypeBuilder();

                    cb.Type("ContentType")
                        .Field("Binary")
                        .DefaultValue(ctDefaultValue);

                    cb.Apply();

                    #endregion
                });

            builder.Patch("7.7.17", "7.7.18", "2021-02-17", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region String resources

                    var rb = new ResourceBuilder();

                    rb.Content("CtdResourcesQ.xml")
                        .Class("Ctd-Query")
                        .Culture("en")
                        .AddResource("UiFilters-DisplayName", "UI filters")
                        .AddResource("UiFilters-Description", "Technical field for filter data.")
                        .Culture("hu")
                        .AddResource("UiFilters-DisplayName", "UI szűrők")
                        .AddResource("UiFilters-Description", "Technikai mező szűrő adatoknak.");

                    rb.Content("ActionResources.xml")
                        .Class("Action")
                        .Culture("en")
                        .AddResource("Browse", "Details");

                    rb.Apply();

                    #endregion

                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("Query")
                        .Field("UiFilters", "LongText")
                        .DisplayName("$Ctd-Query,UiFilters-DisplayName")
                        .Description("$Ctd-Query,UiFilters-Description")
                        .VisibleBrowse(FieldVisibility.Hide)
                        .VisibleEdit(FieldVisibility.Hide)
                        .VisibleNew(FieldVisibility.Hide);

                    cb.Type("File")
                        .Field("Size")
                        .ControlHint("sn:FileSize");

                    cb.Apply();

                    #endregion
                });
            builder.Patch("7.7.18", "7.7.19", "2021-03-16", "Upgrades sensenet content repository.")
                .Action(context =>
                {
                    #region CTD changes

                    var cb = new ContentTypeBuilder();

                    cb.Type("ContentLink")
                        .Field("Name", "ShortText")
                        .FieldIndex(20)
                        .Field("Link", "Reference")
                        .FieldIndex(10);

                    cb.Type("DocumentLibrary")
                        .Field("Name", "ShortText")
                        .FieldIndex(10)
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(20)
                        .Field("Index")
                        .RemoveConfiguration("FieldIndex")
                        .Field("InheritableVersioningMode")
                        .RemoveConfiguration("FieldIndex")
                        .Field("InheritableApprovingMode")
                        .RemoveConfiguration("FieldIndex")
                        .Field("AllowedChildTypes")
                        .RemoveConfiguration("FieldIndex");

                    cb.Type("Folder")
                        .Field("Name", "ShortText")
                        .FieldIndex(20)
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(10);

                    cb.Type("Group")
                        .Field("Name", "ShortText")
                        .FieldIndex(30)
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(20)
                        .Field("Version")
                        .RemoveConfiguration("FieldIndex")
                        .Field("Members", "Reference")
                        .FieldIndex(10)
                        .Field("Index")
                        .RemoveConfiguration("FieldIndex")
                        .Field("Description")
                        .RemoveConfiguration("FieldIndex");

                    cb.Type("Image")
                        .Field("Name", "ShortText")
                        .FieldIndex(30)
                        .Field("DateTaken", "DateTime")
                        .FieldIndex(20)
                        .Field("Keywords")
                        .RemoveConfiguration("FieldIndex")
                        .Field("Index", "Integer")
                        .FieldIndex(10);

                    cb.Type("ImageLibrary")
                        .RemoveField("Index")
                        .RemoveField("InheritableVersioningMode")
                        .RemoveField("InheritableApprovingMode")
                        .RemoveField("AllowedChildTypes")
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(20)
                        .Field("CoverImage", "Reference")
                        .FieldIndex(10)
                        .Field("Description")
                        .RemoveConfiguration("FieldIndex");

                    cb.Type("ItemList")
                        .Field("Name", "ShortText")
                        .FieldIndex(20)
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(10);

                    cb.Type("ListItem")
                        .Field("Name", "ShortText")
                        .FieldIndex(20)
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(10);

                    cb.Type("Query")
                        .Field("Name", "ShortText")
                        .FieldIndex(20)
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(10);

                    cb.Type("Workspace")
                        .Field("Name", "ShortText")
                        .FieldIndex(20)
                        .Configure("MaxLength", "100")
                        .Field("DisplayName", "ShortText")
                        .FieldIndex(10)
                        .Field("Description")
                        .RemoveConfiguration("FieldIndex")
                        .Field("AllowedChildTypes")
                        .RemoveConfiguration("FieldIndex")
                        .Field("InheritableVersioningMode")
                        .RemoveConfiguration("FieldIndex")
                        .Field("InheritableApprovingMode")
                        .RemoveConfiguration("FieldIndex")
                        .Field("Path")
                        .RemoveConfiguration("FieldIndex");

                    cb.Apply();

                    #endregion
                });
        }
    }
}
