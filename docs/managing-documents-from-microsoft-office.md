# Managing Documents from Microsoft Office

Managing workspace documents using Microsoft Office is a common feature of Enterprise Content Management Systems. Using sensenet ECM you are able to create, modify or delete documents within Office. Editing and publishing documents into a workspace is as easy as managing tasks or links as well. There's no need to learn how to upload documents or other files into sensenet workspaces manually, it's done automatically while you're using Office.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/doc-management-msword2013.png" style="margin: 20px auto" />

This feature is fully compatible with Microsoft Office 2013, 2010 and 2007.

> The feature has been tested with Microsoft Office Word 2013, Microsoft Office Word 2010, Microsoft Office Word 2007, Microsoft Office Excel 2013, Microsoft Office Excel 2010 and Microsoft Office Excel 2007. The following information is only valid for these versions of these two programs. Please note that Office 2013 support has been introduced in sensenet ECM 6.3, Office 2010 support in sensenet ECM 6.1, versions prior to this only support Office 2007.

```diff
- From the 1st of September 2015 Chrome does not support NPAPI plugins anymore. 
- This issue has been fixed in sensenet ECM 6.5 by using another technology, but the 
- Edit in Microsoft Office feature will not work in Chrome in sensenet ECM versions prior to 6.5.
```

### Configuration

##### Authentication

sensenet ECM supports the following authentication schemes with Office integration:

- **Windows authentication**
  Set up both your sensenet ECM site content to use *Windows* authentication, and your IIS web site to allow Windows authentication. Make sure your AD user is present in the Content Repository under the appropriate domain. You will be able to access your folders and files in the Content Repository using your AD user and password.

- **Basic authentication**
  Set up your sensenet ECM Site to use *Forms* authentication, and your IIS web site to allow *Anonymous* authentication only (do not allow Windows in IIS and allowing Basic is not necessary). You will be able to access the Content Repository using your portal user and password. Please note that operations that use Basic authentication over a non-SSL HTTP connection are disabled by default by your operating system. To enable WebDAV for non-SSL sites with Basic authentication refer to the following article:  [http://support.microsoft.com/kb/2123563](http://support.microsoft.com/kb/2123563).

> Please note that support for Basic authentication has been introduced in sensenet ECM 6.1. Earlier versions must use Windows authentication for using WebDAV.

```diff
- sensenet ECM requires Windows authentication or SSL HTTP connection in order to manage Office documents. 
- Otherwise, you will see a greyed out menu item in document action menus. 
- To learn how to configure and create your own self-signed certificates, click the following link:
```

- [Configuring Server Certificates](https://technet.microsoft.com/en-us/library/cc732230(v=ws.10).aspx)

##### Trusted sites

Office may not allow you to open documents from sites that are not in the list of _Trusted Sites_. To set up your site as a trusted site open **Internet Explorer** then go to **Internet Options**. On the **Security** tab select **Trusted sites** then click **Sites**. In the following window add your site's url to the list.

##### Permission settings

Only users with sufficient permissions are allowed to use Office protocol functionality. The system files that handle Office protocols are located under _/Root/System/WebRoot/DWS folder_. Add _See_ and _Open_ permissions to users in order that they can access document management functionality from Microsoft Office (see [WebRoot Folder](webroot-folder.md) for details and examples)! Also, sufficient permissions have to be granted to users on Content Repository Workspaces and Documents for them to be able to access and open documents, and to [Applications](application.md) for them to be able to invoke certain Office document management functionality via actions.

### How to open a document from sensenet ECM UI

>**Prerequisites**: this section is about a feature that requires the [SenseNet.WebPages](https://github.com/SenseNet/sn-webpages) component or the full sensenet ECM 6.5 application to be installed.

You can open a document from portal by clicking on the *Edit in Microsoft Office* action from the action list.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/edit-in-microsoft-office-sn65.png" style="margin: 20px auto" />

The _Edit in Microsoft Office_ action appears only for File types of specific extensions. If you'd like to, you can expand the list of supported document types by editing the key **WebdavEditExtensions** under _sensenet/webdav_ section in Web.Config:

```xml
<add key="WebdavEditExtensions" value=".doc;.docx;.rtf;.xls;.xlsx;.ppt;.pptx;.xlsm;.xltx;.ods;.odt;.odp;.ppd;.ppsx;.rtf" />
```

> If you have Microsoft Office 2010 or Microsoft Office 2013 on your computer, you can open documents with 'Edit in Microsoft Office' action. It works in Internet Explorer, Firefox and Chrome. If you have Microsoft Office 2007, you can use this action only in Internet Explorer.

### How to open a document in Windows Explorer (WebDAV)
Please check out the [WebDAV](webdav.md) article about configuring and using WebDAV with sensenet ECM. Opening a file by double-clicking it in a mapped network drive in Windows Explorer opens it automatically in Office the same way as you would open it from the UI.

### How to open a document from Word

- First, open your Word instance, then click on the Office button and select Open
- Type the url of the document server you want to work in into the _File name_ text box on the bottom then press enter. Now you should see a custom open sensenet ECM workspace dialog like below:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-open-file-dialog1.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/edit-in-office-win-expolorer.png" style="margin: 20px auto" />

> You can always use the _Favorites_ folder on the left side of the navigation bar to store the location of the server simply by right clicking _Favorites_ and selecting _Add current location to Favorites_ - to avoid typing the url of the sensenet server all the time when opening a document from Word.

- Navigate to the desired document, then click _Open_ (you can navigate to any content using the well-known Windows Explorer navigation)

**Document Workspaces** tab contains shortcuts to all document workspaces in the [Content Repository](content-repository.md). By clicking on them the window will be navigated to the selected workspace where documents can be found under the _DocumentLibrary_ folder.

### How to publish a new document to an existing workspace

If you have opened a document from the [Content Repository](content-repository.md), you can simply save and checkin the document from Word, just like you used to with documents (see information on versioning and checking in/out documents later). In case you have created a new document and want to upload it to an existing sensenet ECM Workspace, or you want to publish an existing document to a different sensenet Workspace Document Library, you can publish the document right from Word:

- click on the _Office_ button then select _Save As_, or
- click on the _Office_ button then select _Publish / Document Management Server_ like below:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/publish-to-document-management-server.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/publish-to-document-management-server-2010.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/edit-in-office-publishing.png" style="margin: 20px auto" />

In the _Save as_ dialog type the url of the document server into the File name text box then press enter. A custom sensenet ECM workspace window will appear where you can select the desired Document_Library folder you want to save your document in.

> You can use the _Save as_ dialog similarly as the _Open_ dialog: if you don't want to type the url of the server, you can store the address in the _Favorites_ folder. This option, however, is only available when selecting _Save As_, and is not present with the _Publish_ feature.

```diff
- After publishing or saving a new document to an existing document workspace, it should instantly be checked out manually, 
- as Office does not check it out automatically in the current implementation! 
- The Check Out button in this case is only available in the Office button / Server menu. 
- See the 'Document source control' section later in this page.
```

##### Creating a new Document Workspace in Microsoft Office 2007

You can also create a new workspace for your document with Microsoft Office 2007. To do so click on the Office button then select Publish \ Create Document Workspace instead. The _Document Management_ pane will appear. Choose a name for the workspace then type the url of the document server into the _Location for new workspace_ text box. Click _Create_ to finish creating a new workspace.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/create-new-workspace.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/new-workspace.png" style="margin: 20px auto" />

The Location given is treated as site relative, so entering _myworkspace_ and http://localhost will create the new workspace with the path _/Root/Sites/DefaultSite/myworkspace_. You may as well give absolute url's: http://localhost/Root will create the workspace directly under the _/Root_ content.

### Managing workspace content in Microsoft Office 2007

After you open a document from a workspace the _Document Management_ pane will appear. If not, you can make it visible by clicking on Server \ Document Management Information under the Office menu.

> You can make the _Document Management_ pane always visible by clicking _Options..._ at the bottom of the Document Management pane and checking all checkboxes at the label that says _Show the Document Management pane at startup when:..._

> The _Document Management_ pane is only capable of displaying a single document library, a single task list and a single link list only. The name of these lists and their workspace relative paths in the [Content Repository](content-repository.md) must be _/DocumentLibrary_, _/Tasks_ and _/Lists_, respectively. It is possible however to open a document from another Document Library, other than _/DocumentLibrary_ - in this case only the current library's documents can be accessed from the Document Management pane.

> The _Document Management_ pane only works for documents opened from the Document Libraries of Workspaces - but not for documents under a Site, Trash Bin or a Document Library defined outside of a Workspace, even though the document source control features (see later) will work for all documents in the [Content Repository](content-repository.md) regardless of their locations.

List of available members associated to the Workspace.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/document-management-pane-members.png" style="margin: 20px auto" />

List of available tasks in the Workspace.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/document-management-pane-tasks.png" style="margin: 20px auto" />

List of available files and folders in the Workspace.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/document-management-pane-documents.png" style="margin: 20px auto" />

List of available links in the Workspace.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/document-management-pane-links.png" style="margin: 20px auto" />

##### Members

You can not manage members directly from Word but you can still see who is available (and who is online in MSN). Members are defined by explicit permission settings on the workspace content. This means that you can add new members by adding permissions to users on the workspace (add them explicitly if you want them to be displayed on the members tab even if they are already contained in any of the groups having permissions on the workspace).

##### Tasks

Tasks are also important content of a workspace. You have full control on managing tasks in the current workspace within Office.

You can set a task completed by clicking the checkbox next to its title. An empty check box means that it's state is **Pending**. A checked task refers to a **Completed** task. In any other cases, you'll see a filled checkbox. Depending on the status of the task an icon is placed next to the checkbox to visualize its state.

In the following table you can find the proper mappings between the fields of Task Content Type and Office's properties:

| **SN Task Field** | **Office property** |
| ----------------- |:-------------------:|              
| DisplayName       | Title               |
| Status            | Status              |
| Priority          | Priority            |
| AssignedTo        | Assigned to         |
| Description       | Description         |
| DueDate           | Due date            |

> After creating a new task it won't be visible until _Get Updates_ button is pressed.

> If you create your own MyTask Content Type make sure you inherit from the base Task Content Type or any of its descendants in order to make it visible in the list.

##### Documents

Any documents, associated to Office, in the Document Library folder of the current Workspace is visible and can be opened, edited or deleted, if the user has enough permission for these operations. You can also create or delete folders.

##### Links

Links are also supported by sensenet ECM. Create, edit or delete links in the current workspace right within Office.
In the following table you can find the proper mappings between the fields of Link Content Type and Office's properties:

| **SN Link Field** | **Office property** |
| ----------------- |:-------------------:|              
| Url               | URL                 |
| DisplayName       | Description         |
| Description       | Notes               |

> After creating a new link it won't be visible until _Get Updates_ button is pressed.

> If you create your own MyLink Content Type make sure you inherit from the base Link Content Type in order to make it visible in the list.

### Document source control

##### Check Out

When working with documents in sensenet [Content Repository](content-repository.md), the documents have to be [checked out](versioning-and-approval.md) to the users currently editing them. This is not done automatically: when opening a document the user has to click Office button / Server / Check Out button, or click the the Check Out button placed at the top of the document - before any changes can be made to the document:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkout.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkout-2010.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkin-sn65.png" style="margin: 20px auto" />

> You can configure sensenet ECM to automatically check out documents upon opening. This is handled by the `AutoCheckoutFiles` web.config key in the *sensenet/webdav* section. If you set it to true, documents will automatically be checked out to the current user when opened in Office and automatically checked in when closing Office.

##### Check In

After checking out a document and editing it, the document can be checked in by clicking the _Office button / Server / Check In_ button, or the _Check in..._ link placed in the status tab of the Document Management pane:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkin.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/edit-in-office-checkin.png" style="margin: 20px auto" />

After clicking the _Check in..._ link a dialog window will appear where the user can enter a [Checkin comment](checkin-comments.md) and select the desired version of the document (this latter choice is only available when Versioning is switched on for the document):

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkin1.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkin2.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checkin-comment.png" style="margin: 20px auto" />

> A [Checkin comment](checkin-comments.md) in Office can always be given and will be stored in the [Content Repository](content-repository.md) for the given version even if checkin comments are switched off or not available for the given document on the portal.

> A [Checkin comment](checkin-comments.md) in Office is never required and therefore empty chekin comments can always be given in Office even if it is required to fill out on the portal.

##### Discard Check Out

Office also supports a third action called **Discard Check Out**. Undoing a checkout means losing all the changes made to the current document and unlocking it to others (equivalent to Undo changes on the portal). This action can be found under the menu **Server**.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/check-in-check-out-in-office.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/check-in-check-out-in-office-2010.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/edit-in-office-file-info.png" style="margin: 20px auto" />

### Source control messages

If the document has been checked out by someone else, it can still be opened, but when trying to check out the following dialog will appear:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-checked-out.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/source-control-message-msword-2013.png" style="margin: 20px auto" />

Selecting _Notify_ will leave the document uneditable, but as soon as the other user checks in the document, a notifying dialog window will pop up indicating that the document can now be checked out. The above dialog does not indicate the name of the user who has checked out the document for editing, but you can always check it in the status tab of the Document Management pane:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-status.png" style="margin: 20px auto" />

There is a known issue with Office: if a document is already opened (but not yet checked out), and you open the document once more from the UI (or in another instance of Word or Excel), the document will be opened in the previous instance, and the bar at the top of the document - indicating the necessity of Checkin Out the document - will disappear, the document will be editable right-away. However, when trying to save it, the following message will appear:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-not-checked-out.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/checked-out-dialogue.png" style="margin: 20px auto" />

When this happens, simply click the _Office button / Server / Check Out_ button, and the document will be checked out, and you can instantly check it in by clicking the _Check in..._ link already discussed above.

### Document version history

If [Versioning](versioning-and-approval.md) is switched on for a document, Checking in the document will create another version of it. Always the latest version is shown to the user. If you'd like to see all of the previous versions or even want to restore a previous version then choose _Office button / Server / View Version History_ button. The version history dialog window displaying all versions with checkin comments will appear:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/word-versions.png" style="margin: 20px auto" />

You can select any version from the list and
- select _Compare_ to compare the contents of the latest version with the selected version,
- select _Restore_ to restore the previous version. In this case, a new version will be created with the contents of the selected version.

The _Delete_ version operation is not supported in sensenet ECM.

### Examples/Tutorials

##### Permission settings

You can find examples for permission settings under [WebRoot Folder](webroot-folder.md#Examples/Tutorials) section.
