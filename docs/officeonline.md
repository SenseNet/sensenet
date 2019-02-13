---
title: "Office Online"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/officeonline.md'
category: Development
version: v7.0.0
tags: [enterprise, office, document, editing, sn7]
description: This article describes the concepts and configurations for viewing and editing documents in the browser.
---

# Office Online
Viewing and editing office documents in the browsers is a very powerful feature. You do not have to install Office applications on your machine and you do not have to worry about downloading and uploading documents: you simply open the document right there in the browser, modify it, and relax - the document is already saved in the cloud, accessible by others with appropriate permissions.

> **Please note**: currently only **viewing** is possible, editing is not yet implemented in sensenet.

## Office Online Server
What makes this feature possible is **Office Online Server** (formerly known as Office Web Apps Server). You either have to install the server in your local environment (recommended) or connect to the official server provided by Microsoft (requires registration).

> To learn more about how to install and maintain the server, please follow this link:
> 
> [Office Online Server overview](https://docs.microsoft.com/en-us/officeonlineserver/office-online-server-overview)

#### How it works
Your application will have to provide a *hosting page* for OOS where the document viewer or editor is displayed to the user. The viewer/editor UI is provided by OOS, you only have to host a nearly empty page frame.

When a user wants to view or edit a document, the client will connect to OOS, which in turn downloads the document in the background from sensenet and renders it to the user.

## Editing files
> Coming soon.

## Configuration
The only setting you have to make in sensenet is the url of the *Office Online Server*. Please open `OfficeOnline.settings` in the Content Repository (`/Root/System/Settings` folder) and fill the following value:

```json
{
   OfficeOnlineUrl: "https://oos.example.com"
}
```
In the background sensenet will connect to this server and check for its capabilities (supported file types and actions for example).