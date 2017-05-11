# Portlet

A Portlet is an ASP.NET web control that appear as a box on pages. Portlets can display custom layouts and implement custom application logic. The most simple portlets are used to present [Content](content.md) and Content Collections. Sense/Net Pages are mostly built up of Portlets - they are the basic building blocks of Pages.

Building custom pages is done by placing various types of portlets in the page layout. A portlet can be a custom web application (for example the Login Portlet that accepts credentials of users and logs them into the portal) or portlets that rely much on the CMS features of Sense/Net to present Content or Content Collections.

##### List of built-in portlets

There are a couple of pre-defined portlets in the portal. These include the most basic applications and portlets that are necessary when building pages upon the [Content Repository](content-repository.md).

##### Adding portlets to a page

Portlets can easily be added to pages after editing the page and clicking on the _Add portlet_ link placed at the top side of the different portlet zones. When clicked, a Content Picker will pop up and the portlet can be selected from pre-defined categories.

<img src="https://github.com/SenseNet/sensenet/docs/images/reference-wiki-portlet3.png" style="margin: 20px auto" />

Added portlets are only visible to the public when the page has been checked-in (and when versioning or approval of the page is switched on it is published/approved). Pages can be edited using the Portal Remote Control (PRC). After added the properties of the portlets can be set to customize the look and behavior of the portlet.

##### Editing portlet properties

Portlets can be customized via their properties. Properties can be accessed and set in the portlet properties dialog displayed after clicking _Edit_ in the dropdown box that appear at the top right corner of the portlets when the page containing the portlet is in _Edit_ mode. In the portlet properties dialog properties are organized into tabs along function categories.

<img src="https://github.com/SenseNet/sensenet/docs/images/reference-wiki-portlet1.png" style="margin: 20px auto" />

Common properties include the ones displayed on the above screenshot:

- **Portlet title**: the text displayed in the header of the portlet. Localizable resources can also be used, when the title is used in the form of _<%$ Resources:YourResourceClassName, YourResourceName %>_. See Localization for details.
- **Appearance**: controls the visibility of the border and the title (Default/TitleAndBorder/None/TitleOnly/BorderOnly, Default is both are visible)
- **Renderer*: when the portlet supports XSLT rendering an XSLT Renderer can be provided
- **Custom CSS class(es)**: extra css classes to be rendered into the container of the portlet for easy customization of portlet look & feel

Displayed properties depend on the type of the portlet.

> Some property changes when saved only take effect after a postback on the page - it is strongly advised to use the Preview mode functionality of the PRC after changes have been made to portlet properties.

##### Cacheable portlets

Some portlets support caching functionality. When a portlet is _Cacheable_ a _Cache_ tab will appear on the portlet properties dialog. When caching enabled the portlet will cache its output for the given interval and thus when rendered no background logic will be executed within the cache interval. With this technique page response time can be lowered significantly.

> It is strongly advised to use the cache functionality of portlets when available on live sites. Portlets that are not being cached may cause pages to be less responsive even at small visitor rates.

```diff
- Cache is only enabled for visitors and logged in users that are not member of special kind of content administrator groups. 
- These groups are enumerated in the web.config under the "AdminGroupPathsForLoggedInUserCache" appsetting key! 
- This is an important notice when creating sites with largenumber of content administrator users.
```

##### Context bound portlets

Some portlets support context binding functionality. This means that the functionality of the portlet depends on the Content being bound to the portlet. Specifying the Content (also referred to as the context) on which the portlet will execute its custom logic can be adjusted via properties of the _Context binding_ tab that appears in the portlet properties dialog for context bound portlets. These portlets are the basic building blocks for creating Smart Pages - pages that are able to present a whole set of Content with the same layout and function but changing content. Always use Context bound portlets to take fulladvantage of the [Smart Application Model](smart-application-model,md). Context bound portlets can also be used for one specific Content instead of a class of Content. The properties to fine adjust context binding are the following:

- **Bind Target**: this parameter specifies the base target of context binding. Most common choices include:
  - CurrentContent - the context content itself. Makes it possible to use the same page and portlet for different Content.
  - CurrentWorkspace - the closest ancestor Workspace of the current content.
  - CurrentSite - the Site used to access the system.
  - CurrentPage - The Smart Page itself. If you are using a Portlet Page as primary content, this is the same as CurrentContent.
  - CurrentUser - The user who is logged in.
  - CustomRoot - automatic Context Binding is disabled, and the Portlet is bound to a specific content item with an absolute path.
- **Custom root path**: when Bind Target is set to CustomRoot the path of the specific Content can be given here.
- **Ancestor selector index**: selects the ancestor of the set Bind Target. 0 leaves the bound content as specified above, 1 selects parent, higher value selects higher order ancestor.
- **Relative content selector path**: sets the bound content relative to the above settings with a relative path. Ie.: CustomChildFolder/CustomNode selects CustomNode from CustomChildFolder child folder of the Bind Target. Given path is relative to ancestor when Ancestor selector index is different from 0.

Context bound portlets are cachable portlets by design.

##### Portlet inventory

Every Portlet is placed in the _/Root/Portlets_ folder as a Content of Portlet Content Type. This makes it possible for the users to adjust the behavior of the _add portlet_ dialog. Portlet Content are organized into categories. Portlets and categories can easily be managed in Explore as they build up a simple 1-level tree folder structure. Administrators can move portlets from one category to another, fine adjust Portlet visibility permissions for page editors or even delete Portlet Content so that it does not appear in the _add portlet_ dialog. After deleted, you can always reinstall any portlets using the Synchronize Application (read on!).

> Please note that the inventory only controls the visible items in the _add portlet_ dialog and moving / deleting Portlet content has no effect to portlets already added to pages.

##### Installing portlets

Only those portlets are visible in the _add portlet_ dialog that are available in the Portlet inventory. Therefore when a new Portlet is created and added in the webfolder in the form of a dll file it has to be installed to the [Content Repository](content-repository.md). This can be done either by manually creating a new Portlet Content under the _/Root/Portlets_ folder in the appropriate Category folder, or automatically using the Synchronize Application. This latter is defined as an [Action](action.md) link on the _/Root/Portlets_ folder.

<img src="https://github.com/SenseNet/sensenet/docs/images/reference-wiki-portlet2.png" style="margin: 20px auto" />