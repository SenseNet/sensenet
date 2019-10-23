---
title:  "Content Query"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/content-query.md'
category: Concepts
version: v6.0
tags: [query, cql]
---

# Content Query

One of the main features of sensenet is Content Query. sensenet Content Repository is a query based system. This means that every content or image you see on the portal is collected by a very sophisticated and fast query engine.

As a user, portal builder or administrator you get a simple but powerful content query language (CQL). You can use this language to find, filter and sort content even in a huge and constantly expanding repository.

As a developer, you get an easy-to-use Application Programming Interface (API) that is capable of serving any need in your custom code.

## End users

If you want details on how to search for content on the portal, create simple or more sophisticated queries to collect content, see the [Query syntax](query-syntax.md) page.

## Developers

If you are a developer and want to extend your code with collecting and presenting content, see the [Query API](query-api.md) docs.

> Content Query also works from the client side - see our [OData REST API article](odata-rest-api.md) for details.

## Examples

In this section, you will get some overview about where and how can you utilize the power of the Content Query.

> Please check out [Query syntax](query-syntax.md) before proceeding!

### Getting articles

If you want to get the five newest articles you can do so by defining the following query.

```bash
Type:Article .REVERSESORT:ModificationDate .TOP:5
```

### Getting last modified documents

If you want to list the 10 last modified documents of the current user you can do so with this query:

```bash
+Type:Document +ModifiedBy:@@CurrentUser@@ .SORT:Name .TOP:10
```

### All web content in a subtree

```bash
+TypeIs:WebContent +InTree:/Root/MyDocuments
```

### Portlet usage examples

> The examples below are related to the old WebForms user interface of sensenet. We encourage you to use a more modern UI solution using our [client-side packages](https://www.npmjs.com/org/sensenet).

You can use query text in the following places on the UI:

#### Search boxes

When you search for a content on the portal, you can make a simple search for one word, but using the full [Query syntax](query-syntax.md) you can create more sophisticated searches.

### Smart folders

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/Demo-smartfolder.png" style="margin: 20px auto" />

**SmartFolder** is a very powerful portal builder feature of sensenet. You can define a query to collect content from anywhere in the portal and present them as **children** in a SmartFolder.

### Portlet property windows

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/QueryPortletProperties.png" style="margin: 20px auto" />

In sensenet every portlet has a property window that you can use to customize the behavior of the portlet. In some cases, you can provide a content query to specify the list of content you want to present or just a filter query that narrows a children list. These portlets are for example the Content collection Portlet and the Content query presenter Portlet.

### Content Picker (Search mode)

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/QueryContentPicker.png" style="margin: 20px auto" />

If you are editing a content that has a [Reference Field](reference-field.md) or you are editing the properties of a [portlet](portlet.md) you will come across the Content Picker. You can use it to search for content if you switch to Search mode and type a query text to the text box.
