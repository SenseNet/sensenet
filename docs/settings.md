---
title: "Settings"
source_url: “https://github.com/SenseNet/sensenet/blob/master/docs/settings.md”
category: Concepts
version: v6.0
tags: [settings, config]
---

**Sense/Net ECMS** has a number of built-in modules and features that provide a way to customize their behavior by offering **settings**. It is an essential part of an application how it stores and handles settings and we wanted to provide a framework that is easy to use and customize at the same time. In this article developers and portal builders can learn how to acess settings and manage them in Sense/Net ECMS to make custom modules more flexible and easy to configure.

## Settings or configuration
There is an important distinction between settings and configuration. In the following sections you can learn about the differences.

## What is a configuration?
The main attribute of a configuration value is that it is needed during system start. The system cannot start properly without loading the value so it is needed beforehand. In Sense/Net ECMS these values are stored in the **web.config** file (or application configuration files in case of tools). Configuration is read only from the web application's point of view, so every time you need to change a config value it involves the restart of the IIS application. Do not forget to keep configuration synchronized in [installations](/install-sn-from-nuget.md) where there are multiple web servers with config files.

Examples of a configuration value:

- provider types (e.g. MSMQ or resource provider type)
- config values for 3rd party modules (e.g. Entity Framework)
- anything that needs to be loaded before the repository starts

## What is a setting?

In general settings are created for administrators or editors to let them customize the behavior of a certain feature. In Sense/Net ECMS settings are stored as content in the [Content Repository](/content-repository.md). The advantage of this is that changing a setting **does not involve site restart** and you can **manage values in one central place** instead of synchronizing them across web servers.

> In most cases, you should use settings instead of config values as described in the following sections.

## Settings content

Settings on the other hand are readable and writable by the application without restarting it. Settings files are stored in Sense/Net Content Repository. They can be global or local:

- *Global settings*: they are stored in the */Root/System/Settings* folder.
- *Local settings*: any folder can contain a system folder named *Settings* for storing settings files related only to that part of the repository that override or extend global settings (see more about this in the [setting inheritance](#Setting-inheritance) section).

> From version 6.5.4 it is possible to create global-only setting that cannot be inherited so that the setting values will always come from the global file. See settings inheritance for details later in this article.

Another advantage of storing settings as content is that you are able to provide a rich user interface for administrators to manage settings. It is possible to create or edit settings files on the portal user interface or using [OData](odata-rest-api.md), as you can see in the following sections.

> Developers and portal builders are able to access setting values through a simple API that handles caching in the background so you do not have to worry about that.

## Restrictions
There are a couple of restrictions on these files:

- their content type must be *Settings* (or a custom derived type)
- the extension of the file content must be *.settings*
- the name of the settings file must be locally unique: there must not exist two settings files with the same name in one settings container even in subfolders of settings (e.g. if you have mysetting.settings in the Settings folder, you cannot have mysetting.settings in Settings/MySubfolder)

## Settings formats
In Sense/Net you can store settings in one of the following ways.

- **JSON**: a compact, human readable and modern format to store key/value pairs or even large object structures. This is the recommended format for settings.
- **XML**: you can re-use your old configuration values. Just copy-and-paste them into a settings file and there you go.
- **Content fields**: it is possible to store setting values in Content fields defined in your custom settings content type.

In most cases you will just provide key/value pairs in a text file and the settings framework will take care of the rest: you will be able to access the values using a unified API.

## JSON
The easiest way to store settings is to provide values in JSON format that is well-known for web developers.

```
{
  MyDefaultValue: "custom text",
  IsFeatureEnabled: true
}
```

## XML
Another way to store settings is to provide values in an XML format that is the same as the well-known configuration format of ASP.NET config files. You can also provide values in named XML nodes (useful for longer texts), as you can see in the following example:

```
<MySettings>
  <add key="MyDefaultValue" value="custom text" />
  <IsFeatureEnabled>True</IsFeatureEnabled>
</MySettings>
```

## Editing settings
As settings are simple text files, they can be edited in [Content Explorer](http://wiki.sensenet.com/Content_Explorer) or any custom user interface that provides a text editor for settings files. When you modify a settings file, the framework takes care of refreshing the values in the cache, so your features will automatically get the new values.

<img src="/images/settings-editor.png" style="margin: 20px auto" />

## Editing settings through OData

Settings content can be edited through OData as any other content in the Content Repository. You have two options:

- Edit the whole text using the OData [Upload action](http://wiki.sensenet.com/Upload_action#Upload_text_to_a_file_binary).
- Send a POST or PATCH OData request to create or modify settings.

> If the settings file is in **JSON** format, the properties are exposed as **regular fields of the settings content**. This lets you access and even modify setting values directly through OData without having to edit the text. Visit the [Dynamic content items](http://wiki.sensenet.com/Dynamic_content_items) article for more details about this technology.

## Accessing values

There is a server side (C#) API for accessing setting values. The framework takes care of finding the appropriate settings file, caching values and refreshing the value in case somebody modifies the settings content. You can use the syntax below directly in your code.

```
**var** stringValue = Settings.**GetValue<string>**("MyCustomSettings", "MyDefaultValue", PortalContext.**Current.ContextNodePath**);
**var** boolValue = Settings.**GetValue<bool>**("MyCustomSettings", "IsFeatureEnabled", PortalContext.**Current.ContextNodePath**);
```
The parameters are the following:

- **settings name**: the name of the settings file without the extension.
- **settings key**: the name of the setting key inside the file.
- **context path**: path of the requested content (or the content you want to apply the setting to - see [setting inheritance](#Setting-inheritance) later in this article).

If you want to provide a default value in case of the settings content is deleted or the value is missing, you can do that too:

```
**var** boolValue = Settings.**GetValue<bool>**("MyCustomSettings", "IsFeatureEnabled", contextPath, **true**);
```
>It is not advisable to cache setting values in your custom code unless you take care of invalidating values in case of changing settings content.

## Value types

You can use the common value types (numeric types, boolean, even enumerations), strings and dates. The string representation of the value should be in a correct format so that the system must be able to convert it to the desired type. The type is **defined by the caller**. As you can see in the examples below, you must provide the type of the value when accessing settings:

```
**var** boolValue = Settings.**GetValue<bool>**("MyCustomSettings", "IsFeatureEnabled", contextPath, **true**);
**var** intValue = Settings.**GetValue<int>**("MyCustomSettings", "MaxCount", contextPath, **500**);
**var** dateValue = Settings.**GetValue<DateTime>**("MyCustomSettings", contextPath, "**DateValue**");
**var** longValue = Settings.**GetValue<long>**("MyCustomSettings", "LongValue", contextPath, **42000**);
```

## Enumerations

It is possible to store enum values in the settings file. You can provide the string representation of the enum value in the JSON or XML text.



```
{  
   ModeEnumValue: "Advanced"
}
```
Accessing the enum value:

```
var enumValue = Settings.GetValue<ModeEnumType>("MyCustomSettings", "ModeEnumValue", contextPath, ModeEnumType.Simple);
```

## Custom objects (JSON only)

The JSON format and serialization mechanism allows you to store custom objects in JSON format without writing custom code. The following example settings file contains a custom setting object that can be used from your code without writing a converter algorithm:

```
{
   MyCustomValue: {
	SettingPropety1: "text",
	SettingProperty2: 12345
   }
}
```

You can access the setting above by providing the appropriate type (in this case CustomType) that has the properties specified in the setting:

```
var customValue = Settings.GetValue<CustomType>("MyCustomSettings", "MyCustomValue", contextPath);
```

You can also access the individual properties of the custom object above directly by providing the key in a <setting.propertyname> format.

```
var customPropertyValue = Settings.GetValue<int>("MyCustomSettings", "MyCustomValue.SettingProperty2", contextPath);
```
>This is possible because Settings content type supports adding fields dynamically. Please visit the Dynamic content items article for more details.


## Arrays (JSON only)

The JSON format and serialization mechanism allows you to store arrays in settings files, even arrays of custom objects. The following example demonstrates the automatic deserialization of cache header settings in Sense/Net:


```
{
	ClientCacheHeaders: [
		{ ContentType: "PreviewImage", MaxAge: 1 },
		{ Extension: "gif", MaxAge: 604800 },
		{ Extension: "jpg", MaxAge: 604800 },
		{ Extension: "png", MaxAge: 604800 },
		{ Extension: "css", MaxAge: 600 },
		{ Extension: "js", MaxAge: 600 }
	]
}
```
This is how you can access the values above:

```
var customArray = Settings.GetValue<IEnumerable<CacheHeaderSetting>>("Portal", "ClientCacheHeaders", contextPath);
```

## Accessing values through the settings content

It is possible to retrieve the settings content itself and access the values directly through the content interface (if the settings are stored in JSON format). In this case JSON properties are exposed as regular content fields and are accessible the same way you access regular fields:

```
//this method finds the relevant settings content for the path provided (see settings inheritance)
var settingsFile = Settings.GetSettingsByName<Settings>("MySettings", contextPath);

//access one of the JSON properties
var settingValue = Content.Create(settingsFile)["BoolValue"] as bool;
```

##Accessing values on the client side

Currently there is no Javascript API for accessing individual setting values on the client side. You either have to get the settings content directly through OData (remember that JSON properties are accessible as regular fields) or create a [custom OData](/odata-rest-api.md) action for accessing individual settings needed on the client side.

>If you access settings content directly, you have to be sure that you are reading or writing the appropriate (relevant) settings content. Please see the next section about settings inheritance for more details.

## (#Setting-inheritance)
Settings files can be global or local, as we mentioned above. Local settings files override global ones and they are applied only in that part of the Content Repository.

Every key in a settings file is overridable in another file with the same name under an appropriate position in the subtree. See the following example:

<img src="/images/settings-inheritance.png" style="margin: 20px auto" />

For example there is a custom application that uses MySettings. The request starts this application with Aenean semper.doc as a context node (red arrow). In this case your application's real settings are **combined by the ancestor settings chain** that contains three items in the following order:

- the workspace's settings (green arrow)
- the site's settings (blue arrow)
- and global settings (black arrow)

The inheritance is realized on the file and key level: if the settings file on a lower level (which has the same name) contains only one key, only this value will be overridden; values in other files in higher levels won't be affected and will remain accessible.

The examples above about accessing setting values contain a path parameter. That is the content path that the settings framework will use as the starting point when discovering the appropriate settings. If no local setting is found with that name or the given property, the fallback is always the global settings file in the /Root/System/Settings folder.

## Global-only settings
*(from version 6.5.4)*

There are some cases when it does not make sense to let editors or local admins override settings locally. Another important aspect of local settings is that they inherit the values (setting fields) of their parents so if you (as a local admin) can create local settings, you will have access to the values (even sensitive ones) inherited from the global setting. This is where global-only settings come in: if you create a global setting and set it to be Global-only, nobody will be able to create a local setting with the sThere are some cases when it does not make sense to let editors or local admins override settings locally. Another important aspect of local settings is that they inherit the values (setting fields) of their parents so if you (as a local admin) can create local settings, you will have access to the values (even sensitive ones) inherited from the global setting. This is where **global-only settings** come in: if you create a global setting and set it to be Global-only, nobody will be able to create a local setting with the same name, preventing them to see the values in the settings - unless of course they have permissions for the global one or can execute code in the system (because all settings are accessible from code).

## Custom Settings content type
It is possible to create a custom settings content handler and store setting values in predefined fields instead of raw text. You have to create a custom [Content Type Definition](/content-type.md) that inherits the built-in **Settings** type and define the desired fields for setting values on that CTD. After that you have to [allow this new content type](/docs/allowed-child-types.md) on the Settings folder you want to put your settings file into. The advantage of this solution is that you get a GUI for editing your values, you can provide custom logic for your values in your content handler and still be able to access the values using the same simple API.

```
var intValue = Settings.GetValue<int>("MyCustomSettings", "IntegerField", contextPath);
```

## Custom setting values

There is a way to customize the value returned from the text (a JToken from JSON or an XmlNode coming from the XML). You have to override a method in your custom Settings content handler and convert the stored string value to your custom object. The returned value should be of the desired setting value type - or the return value of the base call.

**JSON example**

```
public class City
{
    public string Name;
}

public class MyCustomSettings : Settings
{
    //...

    protected override object GetValueFromJson(JToken token, string key)
    {
        switch (key)
        {
            case "CityList":
                var customValue = ; //TODO: convert the values coming from the token to a custom object
                return customValue;
            default:
                return base.GetValueFromJson(token, key);
        }
    }

    //...
}
```

**XML Example**

```
{
    public string Name;
}

public class MyCustomSettings : Settings
{
    //...

    protected override object GetValueFromXml(XmlNode xmlNode, string key)
    {
        switch (key)
        {
            case "CityList":
                var stringValue = GetInnerTextOrAttribute(xmlNode);
                return stringValue.Split(new[] { ',' }).Select(c => new City { Name = c });
            default:
                return base.GetValueFromXml(xmlNode, key);
        }
    }

    //...
}
```
Now you have a strongly typed custom settings value:

```
if (Settings.GetValue<IEnumerable<City>>("MyCustomSettings", "CityList").Contains("Budapest"))
{
    //...
}
```
## Custom settings binary format

If you want to store your settings in a different format than JSON or XML, you can do so by overriding a method in your custom Settings content handler. If the value cannot be found in the binary, you must return the provided default value parameter. The Settings infrastructure will still take care of caching the value.

```
protected override T GetValueFromBinary<T>(string key, T defaultValue)
{
    var binary = this.Binary;

    //TODO: parse the binary stream and return the value for the given key
}
```

## Related links

- [Content Repository](docs/content-repository.md)
- [Content Handler](docs/content-handler.md)
- [Field](docs/field.md)
- [How to configure Sense/Net in NLB](http://wiki.sensenet.com/How_to_configure_Sense/Net_in_NLB)
- [OData](http://wiki.sensenet.com/How_to_configure_Sense/Net_in_NLB)
- [How to create a custom OData action](http://wiki.sensenet.com/How_to_create_a_custom_OData_action)

## References
There are no related external articles for this article.
