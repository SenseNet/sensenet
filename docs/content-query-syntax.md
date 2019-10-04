---
title:  "Content Query Syntax"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/content-query-syntax.md'
category: Concepts
version: v6.0
tags: [content, query, syntax]
---

# Content Query Syntax 

## Overview
sensenet uses the *Lucene search engine* for indexing and searching for content. We fully support the original [Lucene syntax](http://lucene.apache.org/java/2_9_1/queryparsersyntax.html), but extended it with our own keywords for sorting and filtering, thus forming the content query language (CQL). For the overview of the concept of querying in sensenet, visit the [Content Query](https://github.com/SenseNet/sensenet/blob/master/docs/field-setting.md) page.

Almost everywhere in the portal if you come across a search box - e.g. on the main search page, portlet property window or launching the Content Picker - you can use the Content Query Language.

**Developers** can use this syntax with the [Query API](https://github.com/SenseNet/sensenet/blob/master/docs/query-api.md) when they create a portlet or application.

## Basic search
In most cases you want to look for a single term or a phrase, which is a group of words surrounded by double quotes ("apple pie"). In these cases Sense/Net will look for contents that have these words in their default index data that contains all the relevant text of the content.

In more complex situations you may need some filtering based on the type or place of the content you are looking for, or you want to search in a specific field. In this page you can learn how to construct more complex queries that you can use to effectively find content in a huge repository.

## Wildcard search
We support single and multiple character wildcard searches within single terms.

| Symbol													| Example																		|
| --------------------------------------------------------- | -----------------------------------------------------------------------------	|
| **?** &nbsp;&nbsp; single character wildcard search		| **tru?k** &nbsp;&nbsp; returns content containing *truck* or *trunk*			|
| **&ast;** &nbsp;&nbsp; multiple character wildcard search	| **app*** &nbsp;&nbsp; returns content containing *apple* or *application*		|

## Fuzzy search
We can search similar words based on Lucene's Fuzzy search. Fuzzy search is a simple term followed by "~" and a small number (0 < n < 1). Fuzzy search uses Levenshtein or Edit Distance algorithm in depth. Example:
```bash
Keyword:abbreviate~0.8
```
With this query you can find documents that contain a word with one different character from "abbreviate" e.g. abreviate, sbbreviate etc.

## Proximity search
It is possible to limit the distance of the relevant words in the result documents. The proximity search is two word in quots and followed by "~" and a positive whole number. This number defines the maximal count of words between the given two word. Example:
```bash
Description:"Lorem amet"~3
```
This query will find the following text: "Lorem ipsum dolor sit amet" because there is 3 words between "Lorem" and "amet".

## Fields
When performing a search you can specify the field you want to search in. In the previous cases Lucene used the **default field** called *_Text*, which contains all relevant data of a content. If you search for apple, it is the same as *_Text:apple*.

You can name any existing field, the syntax is *FieldName:VALUE*.
```bash
DisplayName:about
```
There are a few special field types that have their own syntax for defining the query term. For example **DateTime**:
```bash
ModificationDate:>'2010-09-01 12:00:00'
```
If you want to know more about how fields are indexed, visit the [Field Indexing](https://github.com/SenseNet/sensenet/blob/master/docs/field-indexing.md)  article.

### Common search-related fields
sensenet has lots of built-in fields that you can use to make more precise queries. These fields are related to type or place of the content:

| **Name**		| **Comment**													| **Example**						|
| -------------	| ------------------------------------------------------------- | ---------------------------------	|
| **InTree**	| Returns the given folder and all contents under it			| InTree:"/Root/MyContent"			|
| **InFolder**	| Returns all content in that particular folder					| InFolder:"/Root/MyContent/Media"	|
| **Type**		| Returns1 all content that is of the given type				| Type:Article						|
| **TypeIs**	| Returns all content that is of the given type or any subtype	| TypeIs:WebContent					|

> It is usually advised to use quotation marks when querying paths, since paths containing spaces and other special characters like '-' or '\_' could cause trouble and "split up" the expression into two separate expressions instead of handling it as one query for path.}}

### Content List fields
It is possible to execute a query that contains a condition for Content List fields. The syntax of this is the following: you have to use the *#* sign when searching for a content list field. For example if there is a Document Library with the added field **Contract type**, the query will look like:
```bash
+InFolder:/Root/MyContracts +#ContractType:FixedPrice
```
Another example for a date query:
```bash
+TypeIs:CustomContract +#ContractDeadline:<'2011-09-01 12:00:00'
```
## Keywords
In sensenet query language there are several keywords that you can use to make the query more specific, define ordering or paging.

All keywords must be specified using the following syntax: *.KEYWORD:VALUE*

-   starts with a **dot** (.)
-   uppercase
-   followed by a colon and the value

## Sorting
Two keywords exist for defining sorting in a query text:

-   SORT
-   REVERSESORT

You can use both to name one or more fields. Results will be returned in the given order (ascending or descending, depending on the given keyword).
```bash
InFolder:/Root/MyContents .SORT:DisplayName
```
```bash
InFolder:/Root/MyContents .REVERSESORT:CreationDate .REVERSESORT:Index
```

> Please note that sorting works on the indexed fields of the content that the query is executed on. This means you cannot sort the results based on a referenced content's fields. For example it is not possible to sort books based on the referenced authors' country. Developers have the following options:
> -  sort the results in memory on the server or on the client side after the query got executed
> -  create helper fields on the Book content type that index the metadata of the Author only for the purpose of sorting. E.g. add an AuthorCountry field that stores the referenced author's country and can be used for sorting books. But this solution kind of beats the purpose of having [reference fields](https://github.com/SenseNet/sensenet/blob/master/docs/reference-field.md) because you have to maintain consistency across metadata changes: for example reindex book content when the author's country is changed.}}

## Operators
Operators allow terms to be combined through logic operators. Lucene supports AND, "+", OR, NOT and "-" as Boolean operators (boolean operators must be ALL CAPS like keywords).

The OR operator is the default conjunction operator. This means that if there is no operator between two terms, the OR operator is used. The OR operator links two terms and finds a matching document if either of the terms exist in a document. This is equivalent to a union using sets.
```bash
apple OR melon
```
**AND**

The **AND** operator matches documents where both terms exist anywhere in the text or fields of a single document. To search for content that contains the words apple and melon in the field Ingredients, use this query:
```bash
Ingredients:apple AND Ingredients:melon
```
**+**

The "**+**" or **required** operator requires that the term after the "+" symbol exist somewhere in a the field of a single document.

To search for content that must contain "apple" and may contain "melon" use the query:
```bash
+apple melon
```
**NOT**

The **NOT** operator excludes content that contain the term after NOT. It cannot be used with only one term.
```bash
apple NOT melon
```
**-**

The "**-**" or **prohibit** operator excludes documents that contain the term after the "-" symbol.

To search for content that contain "apple" but not "melon" use the query:
```bash
apple -melon
```
## Grouping
Lucene supports using parentheses to group clauses to form sub queries. This can be very useful if you want to control the boolean logic for a query.
```bash
(lime AND apple) OR melon
```
## Paging
One of the most important things when writing queries is paging. In most cases you will not need all of the content but only a few of them that you can present on the UI.

If you want to display only the first 5 content, you can use the keyword **TOP**:
```bash
InFolder:/Root/MyContents .TOP:5
```

> Always use the TOP keyword whenever limiting result count is possible. TOP limits the size of temporary result arrays used in background query execution logic. Not providing .TOP keyword may result in unnecessary memory consumption and unreasonably high intensity GC usage! See Query Optimization for details.

In case you want to display a few content but not the first ones (for example in a box showing related articles or you create a user interface that lets the user choose a page), use the keyword **SKIP**. The query below will skip the first 3 results and will return the second 3:
```bash
InFolder:/Root/MyContents .SKIP:3 .TOP:3
```
## Range search
Range queries allow one to match documents whose field(s) values are between the lower and upper bound specified by the range query. Range queries can be inclusive or exclusive of the upper and lower bounds. As you can see in the examples below, the two types can be mixed.

-   **[...TO...]**: inclusive (the lower and upper bounds are included)
-   **{...TO...}**: exclusive (the lower and upper bounds are NOT included)
```bash
CreationDate:['2010-08-30' TO '2010-10-30']
CreationDate:{'2010-08-30' TO '2010-10-30'}
CreationDate:['2010-08-30' TO '2010-10-30'}
CreationDate:{'2010-08-30' TO '2010-10-30']
CreationDate:>'2010-08-30'
CreationDate:<='2010-10-30'
```
## Filter keywords
sensenet has several types of filters that automatically filter the results. These filters are **switched ON** by default, but you can decide to switch them off.

### AUTOFILTERS
If Autofilters is on, result list will not contain system or deleted content. For example content in (apps) or any other system folder will not be returned. If you want to switch this filter off, do it this way:
```bash
Type:ContentType .AUTOFILTERS:OFF
```
The following content will be filtered out from the results if Autofilters is on:

-   system files (all content of the type SystemFile)
-   System Folders and all content below them (even if the children are not system files)
-   content in the Trash bin
### LIFESPAN
In some content management scenarios contents have lifespan information. This means the content is created but will be valid or available only on a defined date in the future and it may become invalid on another date.

Lifespan filtering is turned off by default. If you want to get results depending on the lifespan status of content, you can switch this filter on:
```bash
Type:Article .LIFESPAN:ON
```
## Quick queries
*(from version 6.5.4)*

When content items are being created or changed, these changes need to be updated in the index. In cases when it is not necessary for the query results to reflect these changes immediately, you can perform quick queries that will be executed without making the index reader up-to-date. For details please visit this article:

-   Query speedup

CQL syntax for executing a quick query:
```bash
Id:<42 .QUICK
```
## Template parameters
You can use parameters in your query text that will we be replaced by the current portal environment. This lets you save a dynamic query. For example it is possible to present the top 5 news of the day or listing the content modified by the currently logged in user.

The usage of the parameters is easy: just put them into the query between double **@@** signs:
```bash
ModifiedBy:@@CurrentUser@@
```
These are the built-in parameters that you can use in your queries:

| **Name**	            |     **Comment**																|
| --------------------- | ----------------------------------------------------------------------------- |
| **CurrentUser**		| Represents the currently logged in user										|
| **CurrentDate**		| Current date, meaning the very start of the day, e.g.:9/17/2010 00:00:00		|
| **CurrentTime**		| Current time, e.g.:10/23/2010 14:30:00										|
| **CurrentMonth**		| Current month																	|
| **CurrentContent**	| Current content																| 
| **CurrentWorkspace**	| Current workspace 															| 
| **CurrentSite**		| Current site																	| 

From **version 6.5.2** there are many new template names that you can use:

| **Name**				|     **Comment**																																	|
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Today**				| Equivalent to CurrentDate and CurrentDay: represents the start of the current day (00:00 AM).														|
| **Yesterday**			| Start of the previous day.																														|
| **Tomorrow**			| Start of the next day.																															|
| **CurrentWeek**		| Start of the current week. This builds on the first weekday defined by the current culture (which is Sunday in some cultures, Monday in others).	|
| **CurrentMonth**		| Start of the current month.																														|
| **CurrentYear**		| Start of the current year.																														|
| **NextWorkday**		| Start of the next workday. It skips weekends but it does not know anything about holidays.														|
| **NextWeek**			| Start of the next week. This builds on the first weekday defined by the current culture.															|							
| **NextMonth**			| Start of the next month.																															|
| **NextYear**			| Start of the next year.																															|
| **PreviousWorkday**	| Start of the previous workday. It skips weekends but it does not know anything about holidays.													|
| **PreviousWeek**		| Start of the previous week. This builds on the first weekday defined by the current culture.														|
| **PreviousMonth**		| Start of the previous month.																														|
| **PreviousYear**		| Start of the previous year.																														|

### Templates with properties
If the parameter represents another content (like *CurrentUser* or *CurrentWorkspace*), you can use any field of that content by naming it after the parameter and a *dot* (. sign):
```bash
InFolder:"@@CurrentWorkspace.Path@@"
```

> Please note the quotation marks around the template. Since a path may contain characters like '-' that are special characters in the query language, it is best to put templates like this inside quotation marks.}}

From **version 6.5.2** multiple properties can be chained:
```bash
CreationDate:<@@CurrentWorkspace.Manager.CreationDate@@
```
### Template expressions
From **version 6.5.2** you can use simple value modifiers (a '+' or '-') on template values (e.g. dates or numbers). This is extremely useful in case of dates, when you want your query to contain dates in the past or future. You may use any one of the date templates above, or any content property that is a date or number.
```bash
CreationDate:<@@CurrentDate+3days@@
CreationDate:<@@CurrentWorkspace.CreationDate-1month@@
Index:@@CurrentList.Index+3@@
```
The following, method-like syntax also works (the units - like days or months - are the same as above, they are just part of the method name):
```bash
CreationDate:<@@CurrentDate.AddWorkdays(3)@@
CreationDate:<@@CurrentDate.AddDays(-5)@@
CreationDate:<@@CurrentWorkspace.CreationDate.SubtractMonth(1)@@
Index:@@CurrentList.Index.Add(3)@@
```
The following list contains the units you may use in an expression (with a shortcut in parenthesis if exists):

```bash
seconds (s), minutes (m), hours (h), days (d), workdays, months, weeks, years (y)
```

> **For developers**: it is possible to define your own query template parameters by writing a custom query template replacer.}}

## Inner query
There are cases when you want to filter content by their [reference fields](https://github.com/SenseNet/sensenet/blob/master/docs/reference-field.md). For example you want to look for books whose authors live in the UK. This query cannot be designed as a simple query for books, because the origin of an author is stored (therefore indexed) on the author content. First you'll have to collect all the authors that live in the UK than execute a second query for books of these authors. This is where **inner query** comes in: it is possible to construct and execute a query like this in a single, compact statement.
```bash
+TypeIs:Book +Author:{{+TypeIs:Author +Country:'UK'}}
```
Note the parenthesis' in the example above. Inside there is a regular query for authors - that is what we call inner query. When you execute a query like this, the system executes the inner query first and it results in a list of *authors* (that live in the UK). Their *id* will be inserted automatically into the outer query as an OR expression:
```bash
+TypeIs:Book +Author:(11 22 33)
```
So every inner query will actually mean executing two queries: first the inner query (or queries) than the outer one. This is the way you can construct queries described above.

> Please note that this type of query works only when executed from source code (using the Content Query API) or through [OData](https://github.com/SenseNet/sensenet/blob/master/docs/odata-rest-api.md). It does not work in an environment when the query will go through a LINQ to Sense/Net execution pipeline - for example smart folders or portlet properties cannot contain inner queries.}}

## Comments
Content queries may contain comments. A comment is useful for explaining the query or adding metadata to it. Line and block comments also can be used. The line comment starts with a double slash ("//") and ends with line feed or EOF.
```bash
InTree:"/Root/(apps)" // Search under the specified location
```
A block comment starts with the "/*" characters and finishes with the "*/" characters and can contain a line feed too.
```bash
/*****************************
** this is a big block comment
**    written by John Smith
******************************/
/* Only in the global apps */ +InTree:"/Root/(apps)"
/* Only specific types     */ +TypeIs:SystemFolder
```
A block comment can be placed anywhere where a whitespace is allowed but the following example is a bad practice:
```bash
InTree:/*bad practice*/"/Root/(apps) //avoid block comment in the middle of a term"
```
Unterminated block comment does not throw an exception but try to avoid this case.
```bash
InTree:"/Root/(apps)" /* Unterminated block comment
```
A comment wrapped in quotation marks or apostrophes behaves as a string. The following query will be parsed as a wildcard query that contains four terms:
```bash
Body:"Lorem ipsum /*dummy text*/"
```
## Escaping Special Characters
Lucene query supports escaping special characters that are part of the query syntax. The current list special characters are

+ - && || ! ( ) { } [ ] ^ " ~ * ? : \

To escape these character use the \ before the character. For example to search in a folder called (5+5-3), you can use the query:
```bash
InFolder:\(1\+1\)\:2
```
You can also create phrases by putting the " character around words. So if you want to search for (1+1):2, you can use the query:
```bash
InFolder:"(1+1):2"
```
Using keywords and operators in term values are also possible if the term value is wrapped by quotes (or apostrophes):
```bash
Name:"AND"
or
Name:'AND'
```
Missing quotes causes InvalidContentQueryException in this case.
