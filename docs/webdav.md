# WebDAV

Sensenet ECM provides a way to access your content via WebDAV, allowing Microsoft Office, Windows Explorer, Total Commander, Visual Studio, etc to open and edit content residing in a sensenet ECM [Content Repository](content-repository.md). The Content Repository can be even mapped as a drive. Office documents can be opened directly from the portal surface with WebDAV. When a document opened via WebDAV is saved, it is automatically versioned, permissions are automatically checked, etc. Drag and drop move and copy also works.

### Installation

WebDAV automatically works in sensenet ECM, but there are a few things you may have to configure before you can access your [Content Repository](content-repository.md) as a file system.

#### Authentication

Sensenet ECM supports the following authentication schemes with WebDAV:

- **Windows authentication**
  Set up both your sensenet ECM site content to use *Windows* authentication, and your IIS web site to allow Windows authentication. Make sure your AD user is present in the Content Repository under the appropriate domain. You will be able to access your fodlers and files in the Content Repository using your AD user and password.

- **Basic authentication**
  Set up your sensenet ECM Site to use *Forms* authentication, and your IIS web site to allow *Anonymous* authentication only (do not allow Windows in IIS and allowing Basic is not necessary). You will be able to access the Content Repository using your portal user and password. Please note that operations that use Basic authentication over a non-SSL HTTP connection are disabled by default by your operating system. To enable WebDAV for non-SSL sites with Basic authentication refer to the following article:  [http://support.microsoft.com/kb/2123563](http://support.microsoft.com/kb/2123563).

> Please note that support for Basic authentication has been introduced in sensenet ECM 6.1. Earlier versions must use Windows authentication for using WebDAV.

 ##### WebDAV publishing

Make sure you don't have WebDAV publishing installed, as it may interfere with sensenet WebDAV features.

### Opening Office documents

Managing documents in a workspace using Microsoft Office is a common feature of Enterprise Content Management Systems. Using sensenet ECM you are able to create, open or edit documents within Office. Opening and saving documents is also done using the WebDAV protocol. To open a document click the **Edit in Microsoft Office** action on the document:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/webdav2.png" style="margin: 20px auto" />

> The UI above is available only if you installed the [WebPages](https://github.com/SenseNet/sn-webpages) component. If you do not have that, you can still put an action link onto your custom UI that lets your users open Office documents - please check the details on the link below.

For more info on managing Office documents in the sensenet ECM Content Repository within Microsoft Office please read the following article:

- [Managing Documents from Microsoft Office](managing-documents-from-microsoft-office.md)

### Mapping the Content Repository to a network drive

It is also possible to map the full sensenet [Content Repository](content-repository.md) to a network drive. To do this first click the **Map network drive...** link in the context menu of *This PC* in Windows Explorer:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/webdav3.png" style="margin: 20px auto" />

Select a suitable drive letter and enter the web address of your sensenet site:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/webdav4.png" style="margin: 20px auto" />

After clicking Finish the [Content Repository](content-repository.md) should appear in Windows Explorer:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/webdav5.png" style="margin: 20px auto" />

From here onwards you can use this drive to manage your [Content Repository](content-repository.md): uploading, editing, deleting, renaming content can be done just like using a normal file system.

### Uploading different file types

The sensenet ECM [Content Repository](content-repository.md) has much more features than a simple file system. It can distinguish between different file types for example. An image for example in a regular file system is a simple file with .png or .jpg extension, an image in the sensenet ECM Content Repository however is a content of *Image* type, which is derived from the *File* type. Therefore when using the Content Repository via WebDAV it is desired to connect files with different extensions to the appropriate [Content Type](content-type.md). This is done using the same setting that is used by the upload function on the ui:

```json
UploadFileExtensions: {
   "jpg": "Image",
   "jpeg": "Image",
   "gif": "Image",
   "png": "Image",
   "bmp": "Image",
   "svg": "Image",
   "svgz": "Image",
   "tif": "Image",
   "tiff": "Image",
   "xaml": "WorkflowDefinition",
   "DefaultContentType": "File"
}
```
> You'll find the settings above in the */Root/System/Settings/Portal.settings* content.

For example when dropping a file with .png extension the content created in the sensenet Content Repository will be of the Image type.

### Configuration

The configuration for WebDAV can be found in the web.config under the `webdav` section.

```xml
<sensenet>
  <webdav>    
    <add key="MockExistingFiles" value="desktop.ini,Thumbs.db,wdmaud.drv,foo,MSGRHU32.ini" />
    <add key="WebdavEditExtensions" value=".docx,.pptx,.xlsx" />
  </webdav>
```

The following options can be set (all values are optional and have a reasonably correct default value):

- **MockExistingFiles**: some versions of WebDAV clients shipped with Windows will automatically probe on certain files and will fail to continue if those files are not present. Since these files are never present in a sensenet [Content Repository](content-repository.md), the WebDAV handler mocks these files as if they existed there to ensure flawless operation. These files may vary from system-to-system, so if you experience any problems with opening sensenet folders via WebDAV it is possible that your WebDAV client is looking for files not present in the Content Repository. Use fiddler or a debug version of sensenet ECM with dbgview to detect this situation and extend this list with the file names to be mocked as existing.
- **AutoCheckoutFiles**: if set to true, files opened in Office will be automatically checked out (locked) to the user. Default is false.
- **WebDAVEditExtensions**: a comma separated list of file extensions (if not set, the usual Office types are listed by default). When a file is downloaded from the portal with the 'download' url parameter, only the files with these extensions will get the correct *filename* value in the *Content-Disposion* response header that browsers use to save the file (*currently this feature has nothing to do with the WebDAV functionality*).

## Custom WebDAV provider

It is possible to customize the behavior of the WebDAV feature by developing a custom provider. Developers can restrict what users see in Windows Explorer and what they can do with files and folders.

- [How to create a custom WebDAV provider](http://community.sensenet.com/docs/tutorials/how-to-create-a-custom-WebDAV-provider/)
