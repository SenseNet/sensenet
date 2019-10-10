---
title:  "Content Query API"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/content-query-api.md'
category: Concepts
version: v6.0
tags: [content, query, api]
---

# Content Query API 

## Overview
---------------------------------------------------------------------------------------------------------

This page contains information for developers about the Application Programming Interface (API) of the Content Query. About creating the query text see the query syntax page.

The API is accessible in the following locations:

* from server side code (like a portlet)
* from any user control or MVC view that can contain source code (ascx, cshtml/vbhtml files)
* any tool that is built on the Content Repository (for example an import/export tool)

With a basic .Net knowledge you can create fast and effective content queries.


## Creating and executing a query
----------------------------------------------------------------------------------------------------------
The heart of the Query API is the ContentQuery class. This class handles the creation and execution of a content query. You can create a query with a simple query text and execute it later:

```
var myQuery = ContentQuery.Create("query text");
 
//...do other stuff...
 
var results = myQuery.Execute();
```
Or you can execute the query in one statement to get the results immediately.
```
var results = ContentQuery.Query("query text");
```
## Prepared queries - from version 6.3
---------------------------------------------------------------------------------------------------------
The methods above work fine when you work with well-known static content query text. For handling scenarios when the query needs to work with dynamic parameters (received from outside of the scope of the current source code), we created a query API that works with predefined queries and parameters.
```
public static QueryResult Query(string text, QuerySettings settings, params object[] parameters)
public static ContentQuery CreateQuery(string text, QuerySettings settings, params object[] parameters)
```
You can create a query object for later use or execute the query immediately same as before. The query text should contain the field and parameter names, and you must provide same number of parameter values at the end:

```
var cq = ContentQuery.Query("+TypeIs:@0 +InTree:@1 +Name:@2", null, type.Name, path, name); 
// if you provide null as a setting, the default QuerySetting will be used
```
## Parameter substitution
------------------------------------------------------------------------------------------------------------
When you create a query with parameters, the system replaces the parameter placeholders with the approppriate escaped parameter values. This is to make sure that the query cannot be changed in any way - e.g. no comments or additional field filters can be added this way: one parameter value will always be treated as an expression for one and only one field.

The parameter placeholder is the '@' character followed by a non-negative integer value. The values are the parameters at a zero based index in the passed parameter array. The value can be any object: the string representation of it will be inserted into the query - except in case of enumerables, as you can see below.

In case of inconsistence beetween parameter count and placeholder indexes, an InvalidOperationException will be thrown.

To ensure the safety of the query, the input value is surrounded by quotation marks if the value contains any forbidden character:

* whitespace
* ' " \ + - & | ! ( ) { } [ ] ^ ~ * ? : / .
In quoted strings every backslash and quotation mark is escaped in this order:

* every '\' is replaced by '\\'
* every '"' is replaced by '\"'.

## Multiple parameter values
If the provided parameter value is an IEnumerable and contains more than one item, it will be enumerated and the escaped values will be joined with one space (' ') and wrapped by brackets. This way you can create implicit OR clauses.
```
var result = ContentQuery.Query("+Index>:@0 +Name:@1 -Index:@2", null, 0, new[] { "Name1", "Name 2", "Name3" }, 42);
```
The executed query:

```
+Index>:0 +Name:(Name1 "Name 2" Name3) -Index:42
```
## Safe queries - from version 6.3
---------------------------------------------------------------------------------------------------------------
There is a whitelist in the system for queries that are considered to be safe (meaning we know them and the context they are executed in). Only these queries can be executed in elevated mode. If the system is not in elevated mode, no whitelist check is performed. All the predefined queries that are considered to be safe should be stored in a special class as static readonly string properties. This class should be marked with the following interface:
```
ISafeQueryHolder
```
There are no methods to implement, just put the query texts into the class. You can define any number of classes for this purpose and put them anywhere in your dlls. The name of the properties can be anything, the system cares only about the text values. The system will preload these queries using reflection, regardless of the visibility of the members, no further configuration is needed.
```
public class MySafeQueries : ISafeQueryHolder
{
    public static string AllDevices { get { return "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"; } }
    public static string InFolderAndSomeType { get { return "+InFolder:@0 +TypeIs:(@1)"; } }
}
```
To use these safe queries you can simply provide them as the query text. You can even build the query text dynamically, if the final result equals with one of the whitelisted query texts, it will pass as a safe query.
```
var results = ContentQuery.Query(MySafeQueries.AllDevices);
```
## Built-in safe queries
-------------------------------------------------------------------------------------------------------------
The following list contains all the currently built-in predefined safe queries. You can use them in your code to execute quries. Your custom safe queries will be added to this whitelist at the start of the application.

* SenseNet.ContentRepository.SafeQueries class
    * AllDevices: "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"
    * AspectExists: "+TypeIs:Aspect +Name:@0 .AUTOFILTERS:OFF .COUNTONLY"
    * InTreeOrderByPath: "InTree:@0 .SORT:Path"
    * InFolder: "+InFolder:@0"
    * InFolderAndTypeIs: "+InFolder:@0 +TypeIs:@1"
    * InFolderCountOnly: "+InFolder:@0 .COUNTONLY"
    * InFolderAndTypeIsCountOnly: "+InFolder:@0 +TypeIs:@1 .COUNTONLY"
    * InTree: "+InTree:@0"
    * InTreeAndTypeIs: "+InTree:@0 +TypeIs:@1"
    * InTreeAndTypeIsAndName: "+InTree:@0 +TypeIs:@1 +Name:@2"
    * InTreeCountOnly: "+InTree:@0 .COUNTONLY"
    * InTreeAndTypeIsCountOnly: "+InTree:@0 +TypeIs:@1 .COUNTONLY"
    * TypeIsAndName: "+TypeIs:@0 +Name:@1"
* SenseNet.Portal.SafeQueries class
    * PreloadXslt: "+Name:'*.xslt' +TypeIs:File .SORT:Path .AUTOFILTERS:OFF"
    * PreloadContentTemplates: "+InTree:@0 +Depth:@1 .AUTOFILTERS:OFF"
    * PreloadControls: "+Name:\"*.ascx\" -InTree:\"/Root/Global/celltemplates\" -Path:'/Root/Global/renderers/MyDataboundView.ascx' .SORT:Path .AUTOFILTERS:OFF"
    * Resources: "+TypeIs:Resource"
    * ResourcesAfterADate: "+TypeIs:Resource +ModificationDate:>@0"
* SenseNet.Workflow.SafeQueries class
    * WorkflowsByRelatedContent: "+TypeIs:Workflow +RelatedContent:@0 .AUTOFILTERS:OFF"
    * UserCountByName: "+TypeIs:User +Name:@0 .COUNTONLY .TOP:1"
    * WorkflowsAutostartWhenCreated: "+TypeIs:Workflow +InFolder:@0 +AutostartOnCreated:yes .AUTOFILTERS:OFF"
    * WorkflowsAutostartWhenChanged: "+TypeIs:Workflow +InFolder:@0 +AutostartOnChanged:yes .AUTOFILTERS:OFF"
    * WorkflowsAutostartWhenPublished: "+TypeIs:Workflow +InFolder:@0 +(AutostartOnPublished:yes AutostartOnChanged:yes) .AUTOFILTERS:OFF"

## Adding clauses
---------------------------------------------------------------------------------------------------------------
If you do not execute the query at once, you can add one or more clauses (filter expressions) to it later. This can be useful when collecting conditions from different sources (for example from the UI).
```
var myQuery = ContentQuery.Create("query text");
myQuery.AddClause("filter text");
...
```
By default these clauses will be added with the **And** chain operator. This means that the result content must fulfill both the original query conditions and the added clause (for more info see the query syntax page). You can change this behavior with the following override:
```
var myQuery = ContentQuery.Create("query text");
myQuery.AddClause("filter text", ChainOperator.Or);
```
...
In this case it is enough for the result content to fulfill only the original conditions or the added clause.
## Query settings
--------------------------------------------------------------------------------------------------------------
In most cases you want to customize how the query must run or how you want to handle the results. You can tell the query engine how much content you want to get or in what order.

## Top
--------------------------------------------------------------------------------------------------------------
The example below shows how can you load only the first 10 items.
```
var settings = new QuerySettings { Top = 10 };
var results = ContentQuery.Query("query text", settings);
```
## Skip
---------------------------------------------------------------------------------------------------------------
You can define how much content should be skipped when running the query. This way (combined with the Top setting) you can define paging for the results. The example below how to get the second page of the results (more info on paging later on this page).
```
var settings = new QuerySettings { Skip = 10, Top = 10 };
var results = ContentQuery.Query("query text", settings);
```
## Sort
--------------------------------------------------------------------------------------------------------------
You can control the order of the results by defining SortInfo objects. You have to give a field name and whether the order is reverse or not.
```
var settings = new QuerySettings { Sort = new[] { new SortInfo() { FieldName = "Title", Reverse = false } } };
var results = ContentQuery.Query("query text", settings);
```
## Enable Auto Filters
--------------------------------------------------------------------------------------------------------------
In Sense/Net there are lots of system content that are must not be presented in search results - for example content views, applications. The content query can filter these content from the result list. By default this filter is ON but you can switch it off in the settings object:
```
var settings = new QuerySettings { EnableAutofilters = false };
var results = ContentQuery.Query("query text", settings);
```
## EnableLifeSpanFilter
---------------------------------------------------------------------------------------------------------------

Lifespan is a per-content setting that defines the lifespan of the content. There is a lifespan switch (called enable lifespan handling) and validity date fields on some content types. If a content is out of these dates it should not be presented in the result list. By default this filter is OFF but you can switch it on in the settings object:
```
var settings = new QuerySettings { EnableLifespanFilter = true };
var results = ContentQuery.Query("query text", settings);
```
## Query Results
---------------------------------------------------------------------------------------------------------------
If you execute a content query, you will get a QueryResult object that contains all the information and operations you will need to handle and present the results.

You can get all the nodes that fulfill the query conditions, or just the first or last page of the results. If you need only the identifiers or the count of the result set, these are there too.

The most important thing about using the results is to use only the aspect of the results that you really need. In most cases you will only need to execute a query and use the nodes on the current page.
```
var result = ContentQuery.Query("query text");
var nodes = result.CurrentPage;
```
Count of the results is always there and there are several methods to find the nodes or identifiers you need. The key of handling the results is paging. To control this, you have to set the SKIP or TOP properties when running a query.

If you want to control the number of nodes on one page in the result set or filter the results, use the QuerySettings parameter of the query. The example below shows how can you get 10 nodes from the repository, while still getting the count info for all the results:
```
var result = ContentQuery.Query("query text", new QuerySettings { Top = 10, EnableLifespanFilter = false });
var nodes = result.CurrentPage;
var count = result.Count;
```
If you want to use more than one page, you have to enumerate the result pages:
```
var result = ContentQuery.Query("query text");
while (result.MoveToNextPage())
{
    var currentNodes = result.CurrentPage;
    //do something with the nodes...
}
```

## Properties and methods
---------------------------------------------------------------------------------------------------------------

* Count property contains the total count of the results. Even if you used the TOP or SKIP setting, the value of Count will represent the whole result set.
* CurrentPage is the list of nodes on the current page. The size of this list is controlled by the TOP setting.
* CurrentPageIndex is the index of the current page. Index of the first page is 0.
* MoveToNextPage method drives the paging mechanism on the result set. After you call this method, CurrentPage will contain the nodes on the next result page.
* Nodes property contains all the nodes on all pages that fulfill the query conditions, regardless of the the value of TOP and SKIP setting. Use only if you need all the result nodes.
* Identifiers property contains all the ids of the result nodes, not only for nodes on the current page.
* Query property contains the query object that was the source of the QueryResult.
* GetPage method returns the nodes on a specific page of the result set and moves the paging cursor to that page.
* GetFirstPage moves the paging cursor to the first page and gets the nodes on that page.
* GetPreviousPage moves the paging cursor to the previous page and gets the nodes on that page.
* GetNextPage moves the paging cursor to the next page and gets the nodes on that page.
* GetLastPage moves the paging cursor to the last page and gets the nodes on that page.

## Query Speedup
----------------------------------------------------------------------------------------------------------------
(from version 6.5.4)

Frequent index writing (e.g. on Content saving) can cause slow-downs in querying because operations like IndexWriter’s Commit and GetReader could be very slow depending on the size of the Index.

We’ve made a speedup strategy to solve this issues with the following improvements:

* Executing Content Queries on an outdated (but not too old) Index.
* Decreasing the amount of Content Queries executed against the Lucene index using database queries instead. This way query load can be balanced between the webservers and the database server.
This solution does not make individual queries faster but can make significant improvements on overloaded webservers.
## Quick Queries
---------------------------------------------------------------------------------------------------------------
A Contenty Query can be marked whether updating the Index Reader is necessary or not before executing the query, even if the actual reader is outdated. There’s a new property named QueryExecutionMode which can have three values (Default, Strict or Quick) and affects updating of the IndexReader in case of using Lucene queries.

* **Strict**: this value causes immediate reopening (if the index is outdated), so the query will be executed on the latest IndexReader
* **Default** : the default value is Strict
* **Quick** : this value causes reopen only occasionally. Frequency of reopening can be configured in the Indexing.setting (/Root/System/Settings/Indexing.settings) with the ForceReopenFrequencyInSeconds property, so the query will be executed only on those data that was updated at least that much time ago. For example if you set 30 seconds as reopen frequency (this is default amount) and switch Quick on it can happen that you will get all the content only those which were updated more than 30 seconds ago.
**Example: CQL**
```
Id:<42 .QUICK
```
**Example: QuerySettings**
```
new QuerySettings { QueryExecutionMode = QueryExecutionMode.Quick };
```
**Example: SN Linq**
```
Content.All.SetExecutionMode(QueryExecutionMode.Quick).Where(c => c.Id < 42)
```
**Example: OData**
```
"/OData.svc/Root/System?queryexecutionmode=quick"
```
## CQL to SQL
----------------------------------------------------------------------------------------------------------------
There are some cases when a Content Query can be compiled to a SQL query and we can execute it on the database instead of the Lucene index. This moves some load from web servers to the SQL server where there are fast indexes that can make these queries faster. A Content Query can be compiled to a SQL query with the following restrictions:

* it does not want to hit older versions (only latest draft or latest public),
* it is not a "count only" query,
the full count feature is not used (inlinecount=allpages in an OData query)
* it does not have nested queries,
* its result is not paged (skip = 0),
* it does not have a wildcard query with question mark,
* it is not a range query with both limit set.
* it does not contain any of the following subqueries:
    * PhraseQueries
    * FuzzyQueries
* it does not contain any dynamic fields (that are stored outside of the Nodes table)
* it contains at least one of these indexed fields:
    * Id, Type, TypeIs, ParentId, InFolder
* it contains only these fields as predicates:
    * CreationDate, ModificationDate, Name, IsSystemContent, LastMinorVersionId, LastMajorVersionId,
* it contains only these fields as sorting conditions:
    * Index, ContentListTypeId, ContentListId, Locked, ModifiedById, CreatedById
This solution’s main advantage is the balancing between the webserver and the database server, because executing strict queries could be faster in SQL. But because of using strict queries, QueryExecutionMode (see the Quick queries feature above) is ineffective in this case (if a query is executed against the SQL db, it has nothing to do with reopening the Lucene index).

The compilation and execution is transparent to the client: you do not have to change anything in your queries to make this work. We compile queries to SQL automatically (if possible).

**Configuration**
The query algorithm can be configured in the appSettings section of web.config.
```
<appSettings>
  <add key="ContentQueryExecutionAlgorithm" value="Validation"/>
</appSettings>
```
It can have the following values:

* **Provider**: Based on the capability of the query and the providers, a main controller will decide which will be the executor. If possible, we compile the query to SQL by default.
* **LuceneOnly**: Uses Lucene only.
* **Validation**: If the provider is other than Lucene than providers data will be validated by Lucene’s result set. This is designed only for validation scenarios because it executes queries twice.
* **Default**: The Provider behavior is effective, see above.
## Related links
---------------------------------------------------------------------------------------------------------------
* [Content Query](http://wiki.sensenet.com/Content_Query)
* [Query syntax](http://wiki.sensenet.com/Content_Query_syntax)
* [LINQ to Sense/Net](http://wiki.sensenet.com/LINQ_to_Sense/Net)
* [Content Query security](http://wiki.sensenet.com/Content_Query_security)
## References
---------------------------------------------------------------------------------------------------------------
There are no external references for this article.