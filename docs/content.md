# Content

A Content is the basic block for storing information in sensenet. A Content can be any kind of data: for example users, documents, memos, tasks, text files, etc. are all referred to as content in the portal. Any content can be created, edited, copied, moved or deleted easily.

> Some of the features described in this article (about displaying content on Pages) are available only if you have the sensenet [WebPages](https://github.com/SenseNet/sn-webpages) component installed, but the underlying philosophy of the content structure applies even if you only have the core [Services layer](https://github.com/SenseNet/sensenet).

### Where can I find these content?

Content are stored in the [Content Repository](content-repository.md) - the storage layer for the portal. The Content Repository is basically a tree structure of the various stored content of the portal. A specific Content is identified by a **unique id** and also by its **path** in the Content Repository - the relative path to the root content. The root of the Content Repository is a content at the _/Root_ path, and all other content is placed somewhere under this root content - for example, the login page for the default site is placed at _/Root/Sites/Default_Site/login_.

### How should I imagine a Content?

Every Content is built up of [Fields](field.md) (a user Content for example has a name field and password field). Different types of content can be created by defining a different set of fields. The type of the Content is called the [Content Type](content-type.md) and defines the set of fields a Content possesses and also the behavior of the Content. The set of fields the type defines is reusable, meaning different types can be created by deriving from already defined types and the newly created type will obtain the fields of its parent type. For example: a File Content has - among a couple others - a name Field for storing the name of the file and a binary Field for storing the binary data of the file, and an Image Content (whose type is a child type of the File Content Type) also have a name (for the name of the image) and a binary Field (for the image pixel data) and a couple of other fields, too - that a simple File does not contain: Keywords (for making the image searchable), DateTaken (for storing the info of when the picture was taken in case it is a photo), etc.

### How does a content look like?

A single Content can be presented in many forms:

You can visually see the content when they are enlisted in a list or in a tree, for example in [Content Explorer](content-explorer.md), the administrative surface of the portal.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/content1.png" style="margin: 20px auto" />

Also, the individual content can be edited, in this case mostly you will be presented a surface where you can edit the content's fields.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/content2.png" style="margin: 20px auto" />

Besides, the content can be presented in any visual form with the aid of [Content Views](content-view.md) or [XSLT Renderers](xslt-renderer.md).

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/content3.png" style="margin: 20px auto" />

The way a content looks like mainly depends on the scenario it is used in (a folder, for example, is used to contain other content, so it shows the list of its children, whereas an article represents some kind of textual information and pictures so it shows the visually formatted text and contained images), but the look of any kind of content can be fully customized.

### How can I store / browse my data in the portal?

The [Content Explorer](content-explorer.md) administrative surface of the portal provides a handful of tools to manage content. You can create new content of the defined [Content Types](content-type.md) and fill their fields with data, you can define new Content Types of any kind, you can copy, move, delete, rename content and upload / download files into / from the Content Repository. It is also possible to Import large amount of data from the file system into the Content Repository (and also to [Export](export.md) them from the Content Repository into the file system).

>Starting with sensenet 7.0, export and import functionality is done using the [SnAdmin tools](snadmin-tools.md) that are available in the core package. You can also create a custom export/import tool using the [.Net client library](https://github.com/SenseNet/sn-client-dotnet).

### Accessing content through the REST API
In case you have only the core [Services](https://github.com/SenseNet/sensenet) layer installed, you do not have a UI at all, but you can still access your content in the repository through our [REST api](odata-rest-api.md).

### How can I reference a content?

Content can be referenced most easily via a url built from the site url and the path of the Content. For example, you can request the content under _/Root/YourContents/FooterContent_ with the *http://example.com/Root/YourContents/FooterContent* url. In this case, the content will be displayed using its *Browse* view. To edit the content the *action=Edit* url parameter can be used: typing *http://example.com/Root/YourContents/FooterContent?action=Edit* will show the Content in Edit mode and thus the content's fields can be edited right away. The mechanism that allows Content to be requested via their Content Repository paths and an action parameter is defined by the Smart Application Model. The [Smart Application Model](smart-application-model.md) makes it possible to create custom actions for the Content. You can check the most common available actions on a content in Content Explorer - they are listed at the top part of the page. To view a content in Content Explorer you can

- open Content Explorer via the [Portal Remote Control](prc.md) (PRC) and navigate to the content, or
- request the Content in the browser with its url http://localhost/Root/YourContents/FooterContent, open PRC (top right corner of any sensenet page) and enter Content Explorer from there

## Examples/Tutorials

The following are some example content in the Content Repository:

- **/Root/YourDocuments/PressRoom/SenseNet6-Logos.pdf**: this is a PDF format File. You can upload / download files like this in Content Explorer.
- **/Root/YourDocuments/PressRoom**: this is the Folder containing the above PDF. You can create folders anywhere in Content Explorer.
- **/Root/Global/images/logo_portalengine.gif**: this is an Image. When you browse it, you will see the image itself. When you edit it, you can set the values of the fields of this content.
- **/Root/Sites/Default_Site/infos/features/calendarinfo**: this is a WebContentDemo content specially created to hold information of the yellow info boxes visible on the demo site. This particular content stores the text that is displayed at the Event Calendar feature of the demo site.
- **/Root/IMS/Demo/Managers/alexschmidt**: this content is a User. The login name of the account it represents is alexschmidt, and the password can be set by editing this content and setting the value for the password field.
- **/Root/IMS/BuiltIn/Portal/Administrators**: this is a Group for the administrators of the portal. Users can be added to this group by editing the group and adding them to the members field. Anyone included in the members list will have administrator privileges on the portal.
- **/Root/Sites/Default_Site/login**: this is a simple Page that is displayed when you are not logged in but you need to be logged in to browse the requested content.
- **/Root/System/Schema/ContentTypes/GenericContent/File/Image**: this is a ContentType describing the structure and behavior if Image Content Types. As you can see, the [Content Type](content-type.md) is a content itself.
