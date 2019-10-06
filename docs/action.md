# Action

Most of the operations done on [Content](content.md) in sensenet ECM is governed via Actions. An Action is basically a command, instructing the system to use a specific component, a so-called [Application](application.md), to display or modify the [Content](content.md) item addressed. To read more on the mechanisms and structure of [Applications](application.md), see the page on the [Smart Application Model](smart-application-model.md).

>**Prerequisites**: some of the features described in this article (about displaying content using Pages) are available only if you have the sensenet ECM [WebPages](https://github.com/SenseNet/sn-webpages) component installed, but the underlying philosophy of arranging applications, security and URL generation applies even if you only have the core [Services layer](https://github.com/SenseNet/sensenet).

There are several kinds of actions in sensenet ECM: there are **HTML actions** that lead the user to an actual page (e.g. the Edit page of a content, where you can modify its properties); there are **client-side actions** that do something in *JavaScript* (e.g. display a popup dialog for picking a content); there are **service actions** that do something with the content and redirect you to a different page; and there are the **OData actions** that make the foundation of the [REST API](odata-rest-api.md) in sensenet ECM.

In this article, we go through these action types and look at their common use cases.

### Actions as simple links

The [Smart Application Model](smart-application-model.md) makes it possible to address [Content](content.md) with links pointing to them using their paths in the [Content Repository](content-repository.md). An `?action` parameter can be used to select the application to handle the [Content](content.md) - in other words this parameter defines what to do with the [Content](content.md). For example:

- http://www.example.com/MyBlog/2010/08/Great_day?action=Edit

If a content item is requested without an action, it is equivalent to specifying the default action, which is **Browse**. Actions are more often referred to as the links that guide the user to the requested application page. An Action link is presented with an [ActionLinkButton](actionlinkbutton.md) control that is a simple HTML link also displaying the requested [Application's](application.md) link.

> sensenet ECM provides a [Client-side action framework](client-side-action-framework.md) for displaying actions in Javascript.

### Actions and Applications

Actions are basically unlimited in number, builders can create [Applications](application.md) for specific, custom actions that the business scenario calls for. Available Actions on a [Content](content.md) are projections of defined applications for its [Content Type](content-type.md). It's not trivial to tell what Actions are valid for a specific [Content](content.md) item, as one needs to take into account all Applications defined for the Content Type of the item, as well as current user privileges on each of those and the item itself. The provided tools (ASP.NET controls available in the [WebPages](https://github.com/SenseNet/sn-webpages) component) for displaying Actions natively handle the problem of available Actions:

- [ActionLinkButton](actionlinkbutton.md): a simple control that displays a single action link
- [ActionList](actionlist.md): a simple control that displays a collection of action links in a list
- [ActionMenu](actionmenu.md): a simple control that displays a collection of action links in a dropdown menu

>If you do not have the **WebPages** component installed, our **action framework** still helps you constructing action links, so you do not have to assemble links manually.

### JavaScript and service actions

Some action links do not navigate the current page to an application defined for the specified [Content](content.md), but rather process data in the background and return or navigate to a custom page. An action link can run custom JavaScript code on the client-side. A good example for this is the _Copy selected..._ action link that when initialized from a list in [Content Explorer](content-explorer.md) it pops up a Content Picker where the destination folder can be selected, and the actual copy operation only takes place after the destination has been selected.

The type of rendered Action is controlled by the application it referes to. The [Application's](application.md) `ActionTypeName` property defines the type (.Net class) of action to be rendered. 

### OData actions

The [REST API](odata-rest-api.md) of sensenet ECM is built on OData actions, and you can create your own custom ones too to extend this API.

#### Action classes and methods
In most cases, it is sufficient to implement a custom operation as a simple method, the same way as you would write an ASP.NET **Web API** method. After putting a placeholder **GenericODataApplication** application in the appropriate folder in the Content Repository, you can start creating your custom method in your project. For the details, please check this article:
- [How to create a custom OData action](how-to-create-a-custom-odata-action.md)

### Scenarios

A **Scenario** is a group of actions one usually displays together. You can think of it as a context menu definition. A scenario is defined bottom-up, by setting the appropriate scenario keyword on each of the applications you wish to access. sensenet has a powerful caching system in place, enabling it to collect all needed actions in a scenario in a flash.

>The action controls above (for example the ActionMenu) can filter and display actions by scenario. The action framework API also lets you query actions this way. See examples below.

### Back URL

The Action URL can contain a parameter called `back`. The portal uses this value when there is a need to **return (redirect) to the previous page** after an operation - e.g. editing content properties. Portal builders can control the behavior of actions: whether to include the backurl or not. The default behavior of the portal is the following: all actions contain the `backurl` parameter except the *Browse* action.

You can control the visibility of the back URL parameter in the following places:

- **Application property**: when you create an application in the repository, you can set the value of the `IncludeBackUrl` field to Default, True or False.
- **ActionLinkButton control**: when you put a control to a content view there is an `IncludeBackUrl` property that you can set. This overrides the value that is given in the application.
- **ActionPresenterPortlet**: this portlet has a property called `IncludeBackUrl` that you can set. This overrides the value that is given in the application.

> It is recommended that you set this value to **False** in your application content if you are sure that the user will not return after visiting that application but will continue to browse the portal 'forward' and the back parameter is not necessary. Otherwise, URLs can grow long and can cause unexpected browser behavior.

### Back target

The Action URL can contain a parameter called `backtarget`. The portal uses this value in a similar way as the back URL above: where to redirect the user after completing the task on the page. The difference is that the `backtarget` parameter values are not exact URLs but tokens that can refer to a certain junction on the portal. The available tokens are the following:

- **CurrentList**: the redirect target is the current ContentList
- **CurrentWorkspace**: the redirect target is the current Workspace
- **CurrentSite**: the redirect target is the current Site
- **CurrentPage**: the redirect target is the current page
- **Parent**: the redirect target is the parent of the current content
- **NewContent**: the redirect target is the newly created content

If a back target value is given in the URL the portal will use it instead of the back URL. The only exception is when the action was not completed (e.g. when a user hits the _Cancel_ button on a content creation page); in that case, the back URL will be used (if exists).

## Example/Tutorials

### Action links

Some example action links may be:

- http://www.example.com/MyBlog/2010/08?action=NewEntry
- http://www.example.com/MyForum/MyTopic?action=Moderate
- http://www.example.com/StrategyGame/Cloud_Kingdom?action=Attack
- http://www.example.com/Software/Sales?action=Executive-BI-Dashboard

### Working with Scenarios

There is nothing more to creating a Scenario than making up a name (keyword) for it, and adding it to all the applications you wish to access through it. Action presenter controls and Action query API calls usually accept a Scenario name, and automatically list all valid Actions found under that name.

Say, you wish to create a forum control panel, which will enable moderators to edit or delete Posts and to lock or move Topics. First of all, you need a name for the scenario. _ForumAdmin_ seems fine.

In our example, the applications for Posts and Topics are distributed as such:

- Posts:
  - **Edit** - /Root/Sites/MySite/Forum/(apps)/ForumPost/Edit
  - **Delete** - /Root/(apps)/GenericContent/Delete
- Topics:
  - **Lock** - /Root/Sites/MySite/Forum/(apps)/ForumTopic/Lock
  - **Move** - /Root/(apps)/GenericContent/Move

  You simply enter the `ForumAdmin` keyword in the *Scenario* field of all the applications above. Now the appropriate actions will be displayed for moderators when they open the admin console. Note, however, that you placed the **Delete** action for GenericContent in the Scenario, which means it will also display for Topics. To hide it, you simply need to deny the Delete permission on Topics for the moderator group. This way, the Delete action on Topics will become inaccessible, and will not show up in the menu.

  This also helps make your system more secure. Simply not showing a command in a menu does not offer real protection. To deny a certain action for a group of users, the preferred way is to use [User rights management](user-rights-management.md).
