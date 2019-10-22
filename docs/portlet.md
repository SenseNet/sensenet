# Portlet

>**Prerequisites**: this article is about a feature that requires the [SenseNet.WebPages](https://github.com/SenseNet/sn-webpages) component or the full sensenet ECM 6.5 application to be installed.

A Portlet is an *ASP.NET web control* that appears as a box on pages. Portlets can display custom layouts and implement custom application logic. The most simple portlets are used to present a single piece of [Content](content.md) or Content Collections. Pages in sensenet ECM are mostly built up of Portlets, the basic building blocks of Pages.

Building custom pages is done by placing various types of portlets in the page layout. A portlet can display a custom user interface and execute custom code (for example the *Login Portlet* that accepts credentials of users and logs them into the portal) or rely upon the ECMS features of sensenet ECM to present Content or Content Collections.

##### List of built-in portlets

There are many pre-defined portlets in the portal. These include the most basic applications and portlets that are necessary when building pages upon the [Content Repository](content-repository.md).

##### Adding portlets to a page

Portlets can easily be added to pages after editing the page and clicking on the _Add portlet_ link placed at the top side of the different portlet zones. When clicked, a Content Picker will pop up and the portlet can be selected from pre-defined categories.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/reference-wiki-portlet3.png" style="margin: 20px auto" />

Added portlets are only visible to the public when the page has been *checked-in* (and in case versioning or approval of the page is active, it also must be *published/approved*). Pages can be edited with the Portal Remote Control (PRC). After they are added, the properties of portlets can be set to customize their look and behavior.

##### Editing portlet properties

Portlets can be customized via their properties. Properties can be accessed and set in the portlet properties dialog displayed after clicking _Edit_ in the dropdown box that appears at the top right corner of the portlets when the page containing the portlet is in _Edit_ mode. In the portlet properties dialog, properties are organized into tabs by function categories.

Some common properties are displayed in the following visual:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/reference-wiki-portlet1.png" style="margin: 20px auto" />


- **Portlet title**: the text displayed in the header of the portlet. Localizable resources can also be used when the title is used in the form of _<%$ Resources:YourResourceClassName, YourResourceName %>_. See Localization for details.
- **Appearance**: controls the visibility of the portlet border and title (by default both are visible)
- **View**: an ascx (or in some cases an XSLT renderer) can be selected to define the UI of the portlet.
- **Custom CSS class(es)**: extra css classes to be rendered into the container of the portlet for easy customization of its look & feel.

The available list of properties depends on the type of portlet.

> When portlet properties are saved, some changes take effect only after a postback on the page - it is strongly advised to use the Preview mode functionality of the PRC after changes have been made to portlet properties.

##### Cacheable portlets

Some portlets support caching of their generated html output. When a portlet is cacheable, a _Cache_ tab will appear on the portlet properties dialog. When caching is enabled the portlet will cache its output for the given interval and thus when rendered no background logic will be executed within the cache interval. With this technique, page response time can be lowered significantly.

> It is strongly advised to use the cache functionality of portlets when available on live sites. Portlets that are not being cached may cause pages to be less responsive even at small visitor rates.

```diff
- Cache is only enabled for visitors and logged in users that are not member of special kind of content administrator groups. 
- These groups are enumerated in the web.config under the "AdminGroupPathsForLoggedInUserCache" key in the sensenet/cache section. 
```

##### Context bound portlets

Some portlets support context binding functionality. This means that the output of the portlet depends on the Content being bound to it. Specifying the Content (also referred to as the *context*) on which the portlet will execute its custom logic can be adjusted via properties of the _Context binding_ tab that appears in the portlet properties dialog for context bound portlets. These portlets are the basic building blocks for creating *Smart Pages*, pages that are able to present a whole set of Content with the same layout. 

>Always use Context-bound portlets to take full advantage of the [Smart Application Model](smart-application-model.md). 

The properties to fine adjust context binding are the following:

- **Bind Target**: this parameter specifies the base target of context binding. Most common choices include the following:
  - CurrentContent - the context content itself (usually the path you see in the address bar of the browser). Makes it possible to use the same page and portlet for different Content.
  - CurrentWorkspace - the closest ancestor Workspace of the current content.
  - CurrentSite - the Site used to access the system.
  - CurrentPage - The Smart Page itself. If you are using a Portlet Page as primary content, this is the same as CurrentContent.
  - CurrentUser - The user who is logged in.
  - CustomRoot - automatic Context Binding is disabled, and the Portlet is bound to a specific content item with an absolute path.
- **Custom root path**: when Bind Target is set to CustomRoot, the path of the specific Content can be specified here.
- **Ancestor selector index**: selects the ancestor of the set Bind Target. 0 leaves the bound content as specified above, 1 selects parent, higher value selects higher order ancestor.
- **Relative content selector path**: sets the bound content relative to the above settings with a relative path. Ie.: CustomChildFolder/CustomNode selects CustomNode from CustomChildFolder child folder of the Bind Target. Given path is relative to ancestor when Ancestor selector index is different from 0.

Context-bound portlets are cachable portlets by design.

##### Portlet inventory

Every Portlet has a content that represents it in the _/Root/Portlets_ folder, organized by category. This makes it possible for the users to adjust the behavior of the _add portlet_ dialog. Portlets and categories can easily be managed in Content Explorer as they build up a simple 1-level tree folder structure. Administrators can move portlets from one category to another, fine adjust Portlet visibility permissions for page editors or even delete Portlet Content so that it does not appear in the _add portlet_ dialog. After deleted, you can always reinstall any portlet using the Synchronize Application (see next section below).

> Please note that the inventory only controls the visible items in the _add portlet_ dialog and moving / deleting Portlet content has no effect to portlets already added to pages.

##### Installing portlets

The only Portlets that are visible in the _add portlet_ dialog are the ones available in the Portlet inventory. Therefore when a new Portlet is created and added in the webfolder in the form of a dll file, it has to be installed to the [Content Repository](content-repository.md). This can be done either by manually creating a new Portlet Content under the _/Root/Portlets_ folder in the appropriate Category folder, or automatically by using the *Synchronize* action. The latter is defined as a link in the _/Root/Portlets_ folder in Content Explorer.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/reference-wiki-portlet2.png" style="margin: 20px auto" />
