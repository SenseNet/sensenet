# SnAdmin tools
[SnAdmin](https://github.com/SenseNet/sn-admin) is the command line tool in **sensenet ECM** for executing upgrade or custom packages. It also helps you with **common operations** related to the Content Repository or web folder files of the sensenet ECM web application. This article is about the latter: the **SnAdmin tools**.

>In previous sensenet versions there were separate command line tools for these operations. From **version 7.0** these are replaced by simple SnAdmin tools, so that you do not have to maintain multiple configuration files, only the one for the **SnAdminRuntime** executable.

In most cases SnAdmin packages contain many steps that may add new dlls, new content to the repository or even change the database schema. But we also offer simple **built-in packages for common operations** like importing or exporting content items so that you *do not have to create packages manually*, just execute them. This article is for operators and developers about these built-in packages (*SnAdmin tools*) that you can use out-of-the box, in a way that is similar to *executing simple commands*.

>**Warning**: most of the following tools require the local sensenet ECM **web site to be stopped** as they access the same index.

# Tool packages
An SnAdmin tool is technically an **SnAdmin package** that resides in the *web\\Admin\\tools* folder. Usually these tool packages are very simple, containing only a few [built-in steps](snadmin-builtin-steps.md). When you execute a tool, you actually execute one of these packages with providing a few parameters - e.g. what do you want to import or which part of the repository do you want to re-index. The command line parameters are the same parameters that these built-in steps have.

For example this is how you execute the import tool:
``` text
SnAdmin import source:"c:\localrepo\new-articles" target:"/Root/Sites/MySite/articles"
```
The tool above will import the usual Content struture from the provided *source* file system directory to the Content Repository folder provided as the *target* parameter.

### Tool parameters
Most of these tools have parameters. You can get a list of available parameters by using the -HELP command line argument:

```txt
SnAdmin import -help
```

# List of SnAdmin tools
To execute these tools you only have to open a **command line** from the *web\\Admin\\bin* folder and execute SnAdmin the same way as you execute any package.
``` text
SnAdmin [toolname] [parameters]
```

## import
Imports Content items from the file system to the repository.
``` text
SnAdmin import source:"c:\localrepo\new-articles" target:"/Root/Sites/MySite/articles"
```

## export
Exporting a folder:
``` text
SnAdmin export source:"/Root/Sites/MySite/articles" target:"c:\localrepo"
```
Exporting only selected (filtered) content items using the [Content Query syntax](http://wiki.sensenet.com/Content_Query_syntax):
``` text
SnAdmin export source:"/Root/Sites/MySite/articles" target:"c:\localrepo" filter:"+TypeIs:Article +CreationDate:<@@CurrentDate+3days@@"
```

## delete
Deletes a content from the repository.

``` text
SnAdmin delete path:/Root/MyFolder/MyContent
```

## seturl
Setting a url on the default site:

``` text
SnAdmin seturl url:demo.example.com
```

A more complex scenario:

``` text
SnAdmin seturl url:demo.example.com site:MySite authenticationType:Windows
```

## index
Re-create the index for the whole Content Repository (in case of a large repository this may take time).
``` text
SnAdmin index
```
Repopulate the index of a subtree.
``` text
SnAdmin index path:"/Root/Sites/MySite/MyFolder"
```

## createeventlog
Creates the default **event log** for sensenet ECM so that you can see entries in the Windows **Event Viewer** tool.
``` text
SnAdmin createeventlog
```
Creates a named event log for your project.
``` text
SnAdmin createeventlog logname:"MyProject"
```

## deleteeventlog
Deletes an event log along with its registered sources.
``` text
SnAdmin deleteeventlog logname:"MyProject"
```
