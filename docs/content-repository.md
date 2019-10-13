# Content Repository

A content repository is a store of digital content with an associated set of data management, search and access methods allowing application-independent access to the content rather like a digital library, but with the ability to store and modify content in addition to searching and retrieving. The sensenet Content Repository (SNCR) forms the technical underpinning of sensenet ECM. It gives structure to unstructured content as the logical storage facility. It is the container of content (individual blocks of information) that also provides the service layer to manipulate (add, copy, move, delete, etc.) it.

### Managing Content

The Content Repository provides services to end users for managing content. It has the following features:

- Content storage in one hierarchical tree structure with content types
- Built-in and custom meta data for content
- Create (or upload), modify, delete, copy and move content
- Trash for temporary deletion
- Ultrafast query and full text search
- Permission management with inheritance in the tree
- Versioning (version control, version history and locking) for collaboration
- Import/export
- WebDAV, so users can map the repository as a remote drive
- Standard OData REST API for third party applications

### Structured storage of Content

The Content Repository is basically a tree structure of the various stored content. A specific content is identified by a **unique id** and also by its **path** in SNCR. The root of SNCR is at the /Root path, all other content is placed somewhere under this root content - for example the login page for the default site is placed at /Root/Sites/Default_Site/login. The default structure is organized as follows (only the main folder structure is listed here):

<div style="display: inline-block;vertical-align: top; padding-right: 20px; width: 260px;">
<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/content-repository1.png" /> 
</div>

<div style="display: inline-block;width: 500px;">

- **Root** - this is the root of the tree structure. If you click on it in [Content Explorer](content-explorer.md) you will get a link summary page where the most common administrative functions are listed ([Root Console](root-console.md))
  - **(apps)** - top level _(apps)_ folder containing the global applications for various [Content Types](content-types.md)
  - **ContentTemplates** - folder containing [Content Templates](content-templates.md): pre-defined default values and structures for [Content Types](content-types.md)
  - **Global** - the global resources of the portal. Requested skin resources will fall back to these Content if not defined in the current skin
    - **celltemplates**
    - **contentviews**
    - **fieldcontroltemplates**
    - **images**
    - **pagetemplates**
    - **plugins**
    - **renderers**
    - **scripts** - JavaScript files
    - **styles** - Css files
  - **IMS** - folder containing Domains, Organizational units, Groups and [Users](user-content-type.md) in a hierarchical tree structure
  - **Localization** - resource files with multi-language text content for [Localization](localization.md)
  - **Portlets** - folder of installed Portlets
  - **Sites** - container of defined sites
    - **Default_Site** - demo site
  - **Skins** - container folder for [Skins](skin-system.md)
    - **empty** - an empty skin for creating new skins
    - **sensenet** - default skin and resources of demo site
  - **System** - system related Content
    - **Devices** - contains Device content which can help you to create specific application pages to display the same content on different devices (e.g. tablet, mobile)
    - **errormessages** – contains customized site specific html files to display exception messages (the files are grouped in folders by site)
    - **Renderers** - some renderers used by the base system
    - **Schema**
      - **Aspects** - container for Aspects
      - **ContentTypes** - container for [Content Types](content-types.md)
    - **Settings** – contains global [Settings](settings.md)
    - **SystemPlugins** - resources of base system applications
    - **WebRoot** - container for system handlers/pages, with automatic redirection from '/' path (see [WebRoot Folder](webroot-folder.md) for details).
    - **Workflows** – container for workflows
  - **Trash** - container of deleted Content ([Trash](trash.md))

</div>

### Content Repository and the type system

The sensenet Content Repository is built upon a metadata system with pre-defined base types and type inheritance support. Content stored in SNCR can have different types, but content can be one type at any one time. A content type defines the properties (fields) and behavior of content. [Content type definitions](ctd.md) are also stored as content in the repository. They are located in the _/Root/System/Schema/ContentTypes_ folder. To sum it up, the SNCR relies on the type system that - from the storage perspective - defines the reusable set of fields for each content type. You can also add extra fields to a certain content (apart from the fields in its type) through [aspects](aspect.md).

 ### Metadata indexing for fast search and filtering

 Content in the SNCR are indexed using the [Lucene](http://lucene.apache.org/lucene.net/) indexing and search library. The text of binary documents (Microsoft Word, Excel, Adobe PDF, etc) is extracted and can also be searched in SNCR. Lucene provides extremely fast query results even on big (over 10 million content) repositories.

 ### Content access and url resolution

 Every content in SNCR is identified by its unique Id and its Path. You cannot change a content id, but you can move a content to another folder and thus change its path. The tree structure of the SNCR makes it possible to use the path as a link to the content, and thus the individual content can be addressed by their root-relative or site-relative paths as URL links. For example a root-relative /Root/Sites/DefaultSite/My-folder/My-file.docx content can be addressed as My-folder/My-file.docx if your browser points to a url that is registered on the Default_Site.

 ### Managing sensenet Content Repository

In an ECM system the most frequently used operations are searching, reading and writing content in the content repository. Document libraries in workspaces, custom forms, pictures in image libraries are all stored and managed in the one and only big tree of sensenet Content Repository. If you want to have an overview of the whole tree structure (similarly to Windows Explorer or OSX Finder) you can use the administrative GUI of sensenet ECM, the [Content Explorer](content-explorer.md) (this is available only if you have the [WebPages](https://github.com/SenseNet/sn-webpages) component or the full sensenet ECM 6.5 product installed).