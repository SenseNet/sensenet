---
title: "Versioning and approval"
source_url: 'https://github.com/SenseNet/sensenet/docs/versioning-and-approval.md'
category: Concepts
version: v6.0
tags: [versioning, approval, document management, sn6, sn7]
---

# Versioning and approval

Versioning, also known as revision control, is the management of changes to documents and other information stored in a file system or repository.

The main goal of versioning is to prevent information from being overwritten or deleted during everyday work with documents. Changes are kept track of, and a mechanism is offered to restore a particular document to a previous version.

The versioning system of **sensenet ECM** also provides mechanisms for keeping the published version of a document under heavy editing visible to outside users, while you continue to work on the latest, draft version.

### Versioning in sensenet ECM

In sensenet ECM, versioning is disabled by default. It can be enabled for folders or content lists, by setting the value of the Versioning Mode field. Subfolders inherit versioning settings by default.

**Versioning Mode** settings for folders:

| Versioning mode |                                                                                                            |
| --------------- | ---------------------------------------------------------------------------------------------------------- |
| None            | The default setting of the Root folder, no versioning.                                                     |
| Inherited       | The folder inherits its versioning mode from its parent. This is the default setting for all other content.|
| Major only      | Only major versions (*1.0*, *2.0*,...) are preserved.                                                      |
| Major and minor | Every version is preserved (*1.0*, *1.1*, *1.2*,...).                                                      |

When a new Content is created in the Content Repository with versioning enabled, it is assigned the initial version number, depending on the versioning mode. Changes made to the document will result in a bump of the version number, with old versions tracked for possible rollbacks.

### Approval

sensenet ECM also introduces a basic approval functionality. Regardless of the versioning mode in use, approval can be enabled to control changes.

If approval is required for a certain Content, after changes has been made to the document, the system creates a version labeled ‘Pending for approval’. This version is visible only for administrators and users who have permission to *Approve* or *Reject* it. If the Content is approved, its version number is bumped according to the versioning mode, and gets the A ("*approved*") flag.

This method provides an extra line of defense for keeping mission critical content error free.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/approval/approving-approve.png" style="margin: 20px auto" />

### Public visibility

Vistors in general are only allowed to view last public versions of a content. This is controlled by the *Open minor* permissions: a user that does not possess the open minor permission for a content will only see the last public version of a content, and will never see any changes that correspond to a draft version or that are not yet approved.
The other important thing here to bear in mind is that if a document gets rejected it does not mean that the document is not visible for the public. It only means that the last version that was rejected will not be visible to the public. So for example:
1. Set approval on a document library to true.
2. Upload a document - it's state will be pending for approval (you can check it out on versions tab): only users with open minor permissions will be able to see it.
3. Send it to approval using the approval workflow. If the approver rejects it, it still not be visible for the public - or users that have no open minor permissions. If the approver approves it, it will be visible for the public.
4. Edit the document and make some modifications. It's state will be pending for approval once again. Users with no open minor permissions are able to see the document but not the latest modifications: that is they see the last public version, and not the one that is pending for approval.
5. Send the document for approval. If the approver rejects it: users without open minor permission will still not be able to view the modifications that have been rejected: only the last public version. If the approver approves the document: users without open minor permissions will finally be able to see the modifications as well, because at the moment of approval the last (yet pending) version became the last public version.

### Content states

Content in the Repository can have several non-numeric version states:

| State |          |                                                                                                                                    |
|:-----:| -------- | ---------------------------------------------------------------------------------------------------------------------------------- |
|A      | APPROVED | Only a major version can be in approved state: 1.0A or 2.0A. The last approved version of the content is that users with low level permission can see. |
|L      | LOCKED   | When a content is locked, only the user who locked it can modify it.                                                               |
|D      | DRAFT    | A draft version is only visible to users who have permission to see minor versions of a content. Any other user will see the last public version. |
|P      | PENDING  | When approval is enabled in a folder or list, then contents cannot be published without approval. After sending a content for approval it remains in pending for approval state, until somebody with sufficient rights approves it. |
|R      | REJECTED | If a content is not correct, the user with approving rights can reject it. This means it is not published and should be refined.   |

### Content behavior if versioning is enabled

#### Major only mode

After a user finished editing a Content, the major version number is bumped by default: eg. *1.0* becomes *2.0*. This means the new version is automatically published, and will be served to all users requesting the content in question.

#### Major and minor mode

In this mode, saving a Content bumps its minor version number. Content with minor version (for example 1.2, 3.5, anything but *x.0*) are considered working versions, and aren't served to guest users - they get the latest major version instead. (Eg. if a document's latest version is *5.4D*, guest users will be served *5.0A*, the latest public version.)
In this versioning mode, bumping the major version and thus publishing the changes can be done by pressing the **Publish** button.

#### Check out and Check in

You may have already had trouble with scenarios where both you and a colleague were making changes to the same document on a corporate file server. If you saved first, and your colleague second, all your changes were overwritten and lost. The *Check out / Check in* locking mechanism is intended to prevent such problems.

Before making changes to a Content, you can - and indeed, should, if you are working in a multi-user, production environment - *Check* the Content *out*. This acts as a write lock, enabling other users to access it for reading, but not for modifications.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/approval/versioning-checkout.png" style="margin: 20px auto" />

You may save several times during work, and *Check in* when you are done, lifting the lock.

In *None* and *Major only* versioning modes, *Checking out* also provides a way to separate the working version of a Content from the public version. Guest users will see the Content's previous state (before it has been *Checked out*), until the changes have been *Checked in*.

#### Undo changes

If you checked out a content, edited it, but decide to drop your changes, you can do so by pressing the **Undo changes** button. This reverts the content to the state before you checked it out.

Administrators can have a *Force undo changes* permission. This means they can drop changes to a locked content made by any other user. This is useful when somebody checked out a content and she cannot check it in for some reason, but the content needs to be edited by somebody else.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/approval/versioning-checkin.png" style="margin: 20px auto" />

#### Taking over the lock - from version 6.3.1 patch1

There are cases when somebody checked out a document, but the user is no longer available (or does not have enough permissions anymore). If force undo changes is not sufficient (e.g. because you want to preserve the content modifications) then administrators are allowed to *take over the lock* on the content. The admin still needs to have *Force undo changes* permission on the content. It is also possible to *pass on the lock to another user*. That user does not have to have force undo changes permission though, only *Save*.

As this action is very rare and used only in eceptional cases, it has a special [audit log](/docs/Logging.md#Audit log) entry:

- *LockTakenOver*

**for Developers**

Developers can access the take over lock feature through the following APIs:

- [TakeLockOver OData action](/docs/built-in-odata-actions-and-functions.md)
- C# api:

```csharp
node.Lock.TakeLockOver(targetUser);
```

### <a name="enable"></a>Enabled versioning actions

The following tables summarizes the actions you can perform in a particular state of a Content.

|          | Save | Check out | Check in | Undo | Publish | Approve | Reject | Save and check in | Take lock over |
| -------- |:----:|:---------:|:--------:|:----:|:-------:|:-------:|:------:|:-----------------:|:--------------:|
| Approved | ✔   | ✔         |          |      |         |         |        | ✔                 |                |
| Locked   | ✔   |           | ✔        | ✔   |         |         |        |                    | ✔             |
| Draft    | ✔   | ✔         |          |      |✔       |         |        | ✔                 |                |
| Rejected | ✔   | ✔         |          |      |✔       |         |        | ✔                 |                |
| Pending  | ✔   | ✔         |          |      |         |         |        | ✔                 |                |

### Changing Versioning or Approving mode

You can change versioning or approving mode on any folder or other container type. If you visit the edit page of the folder you'll find the versioning and approving settings among the advanced fields.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/approval/versioning-change.png" style="margin: 20px auto" />

### Example/Tutorials

In this section you can see examples of how content version numbers changing if you work with the content. If you want to see the actions you can perform in particular state of a content, check [Enabled versioning actions](#enable).

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/approval/versioning-tutorial.png" style="margin: 20px auto" />