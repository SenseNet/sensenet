---
title: "How to create a new class for source code"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/how-to-create-a-new-class-for-source-code.md'
category: Development
version: v6.0
tags: [howto, developers, enterprise, community]
description: This article describes how to create a new class for containing custom source code.
---
# Steps
In order to have your code running in Sense/Net you will have to place the compiled code in the bin folder. There are two ways to achieve this.

## Option 1: Add new class in the web application
If you have not yet already set up your development environment please read [How to set up development environment](how-to-set-up-development-environment.md). Open the web application that is set up as the IIS site for your Sense/Net installation (_MyWebApplication_) and add a new class to the `Code` folder (see section _Create Code and Root folders_ in [How to set up development environment](how-to-set-up-development-environment.md)). Since your code will be part of the running web application, rebuilding your web application is adequate to have your compiled code running in the bin folder.

> Please note, that for creating console application and other external tools you will need to create separate projects and not use the web application referred in [How to set up development environment](how-to-set-up-development-environment.md).

## Option 2: Add new class in a separate project
1. Create a new class library in Visual Studio 2010 to an arbitrary location and add a new class (if Visual Studio has not yet added one automatically). 
2. Add the following references: 
    * SenseNet.ContentRepository.dll
    * SenseNet.Portal.dll
    * SenseNet.Storage.dll
    * System.Web.dll
3. After producing your code, compile the project and copy dll-s into the web folder's bin folder.
> Although option 2 will work fine and might be easier to start off with, the recommended way is option 1.

# Related links
* [How to set up development environment](how-to-set-up-development-environment.md)

# References
There are not external references for this article.
