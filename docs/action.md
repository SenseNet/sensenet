# Action

Most of the operations done on [Content](content.md) in sensenet ECM is governed via Actions. An Action is basically a command, instructing the system to use a specific component, a so-called [Application](application.md), to display or modify the [Content](content.md) item addressed. To read more on the mechanisms of Actions and [Applications](application.md), see the page on the [Smart Application Model](smart-application-model.md).

##### Actions as simple links

The [Smart Application Model](smart-application-model.md) makes it possible to address [Content](content.md) with links pointing to them using their paths in the [Content Repository](content-repository.md). An `?action` parameter can be used to select the [Application](application.md) to handle the [Content](content.md) - in other words this parameter defines what to do with the [Content](content.md). For example:

- http://www.example.com/MyBlog/2010/08/Great_day?action=Edit

If a content item is requested without an action, it is equivalent to specifying the default action, which is **Browse**. Actions are more often referred to as the links that guide the user to the requested [Application](application.md) page. An Action link is presented with an [ActionLinkButton](actionlinkbutton.md) control that is a simple HTML link also displaying the requested [Application's](application.md) link.

> From version 6.3 sensenet ECM provides a [Client-side action framework](client-side-action-framework,md) for displaying actions in Javascript.

##### Actions and Applications

Actions are basically unlimited in number, builders can create [Applications](application.md) for specific, custom actions that the business scenario calls for. Available Actions on a [Content](content.md) are projections of defined [Applications](application.md) for its [Content Type](content-type.md). It's not trivial to tell what Actions are valid for a specific [Content](content.md) item, as one needs to take into account all Applications defined for the [Content Type](content-type.md) of the item, as well as current user privileges on each of those and the item itself. The provided tools for displaying Actions natively handle the problem of available Actions:

- [ActionLinkButton](actionlinkbutton.md): a simple control that displays a single action link
- [ActionList](actionlist.md): a simple control that displays a collection of action links in a list
- [ActionMenu](actionmenu.md): a simple control that displays a collection of action links in a dropdown menu

##### Javascript and other Actions

Some action links do not navigate the current page to an [Application](application.md) defined for the specified [Content](content.md), but rather process data in the background and return or navigate to a custom page. An action link can run custom javascript code for custom operations. A good example for this is the _Copy selected..._ action link that when initialized from a list in [Content Explorer](content-explorer.md) it pops up a Content Picker where the destination folder can be selected, and the actual copy operation only takes place after destination has been selected.

The type of rendered Action is controlled by the [Application](application.md) it referes to. The [Application's](application.md) `ActionTypeName` property defines the type of action to be rendered. To see the list of built-in action types please refer to [Application#Application](application.md#Application) configuration section.

##### Scenarios

A **Scenario** is a group of Actions one usually displays together. You can think of it as a context menu definition. The way one defines a scenario, however, may be quite different from what you are used to. Basically, a scenario is usually defined bottom-up, by setting a `scenario` keyword on each of the [Applications](application.md) you wish to access. sensenet has a powerful caching system in place, enabling it to collect all needed Actions in a Scenario in a flash.

##### Back URL

The Action URL can contain a parameter called back. The portal uses this value when there is a need to return to the previous page after an operation - e.g. editing content properties. It is possible for portal builders to control the behavior of actions: whether to include the backurl or not. The default behavior of the portal is the following: all actions contain the `backurl` parameter except the Browse action.

You can control the visibility of the back url parameter in the following places:

- **Application property**: when you create an application in the repository, you can set the value of the `IncludeBackUrl` field to Default, True or False.
- **ActionLinkButton control**: when you put a control to a content view there is an `IncludeBackUrl` property that you can set. This overrides the value that is given in the application.
- **ActionPresenterPortlet**: this portlet has a property called `IncludeBackUrl` that you can set. This overrides the value that is given in the application.

> It is recommended that you set this value to **False** in your application content if you are sure that the user will not return after visiting that application but will continue to browse the portal 'forward' and the back parameter is not necessary. Otherwise URLs can grow long and can cause unexpected browser behavior.

##### Back target

The Action URL can contain a parameter called `backtarget`. The portal uses this value in a similar way as the back URL above: where to redirect the user after completing the task on the page. The difference is that the `backtarget` parameter values are not exact URLs but tokens that can refer to a certain junction on the portal. The available tokens are the following:

- **CurrentList**: the redirect target is the current ContentList
- **CurrentWorkspace**: the redirect target is the current Workspace
- **CurrentSite**: the redirect target is the current Site
- **CurrentPage**: the redirect target is the current page
- **Parent**: the redirect target is the parent of the current content
- **NewContent**: the redirect target is the newly created content

If a back target value is given in the URL the portal will use it instead of the back URL. The only exception is when the action was not completed (e.g. when a user hits the _Cancel_ button on a content creation page); in that case the back URL will be used (if exists).

## Example/Tutorials

##### Action links

Some example action links may be:

- http://www.example.com/MyBlog/2010/08?action=NewEntry
- http://www.example.com/MyForum/MyTopic?action=Moderate
- http://www.example.com/StrategyGame/Cloud_Kingdom?action=Attack
- http://www.example.com/Software/Sales?action=Executive-BI-Dashboard

##### Working with Scenarios

There is nothing more to creating a Scenario than making up a name (keyword) for it, and adding it to all the [Applications](application.md) you wish to access through it. Action presenter controls and Action query API calls usually accept a Scenario name, and automatically list all valid Actions found under that name.

Say, you wish to create a forum control panel, which will enable moderators to edit or delete Posts, and to lock or move Topics. First of all, you need a name. _ForumAdmin_ seems fine.

In our example, the [Applications](application.md) for Posts and Topics are distributed as such:

- Posts:
  - **Edit** - /Root/Sites/MySite/Forum/(apps)/ForumPost/Edit
  - **Delete** - /Root/(apps)/GenericContent/Delete
- Topics:
  - **Lock** - /Root/Sites/MySite/Forum/(apps)/ForumTopic/Lock
  - **Move** - /Root/(apps)/GenericContent/Move

  You simply enter the `ForumAdmin` keyword in the Scenario field of all the above Applications. Now the appropriate actions will be displayed for moderators when they open the admin console. Note however, that you placed the **Delete** action for GenericContent in the Scenario, which means it will also display for Topics. To hide it, you simply need to deny the Delete permission on Topics for the moderator group. This way, the Delete action on Topics will become invalid, and will not show.

  This also helps make your system more secure. Simply not showing a command in a menu does not offer real protection. To deny a certain action for a group of users, the preferred way is to use [User rights management](user-rights-management.md).