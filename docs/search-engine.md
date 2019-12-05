---

title: "Search engine"
source_url: 'https://github.com/SenseNet/sensenet/docs/search-engine.md'
category: Concepts
version: v7.0
tags: [search, index, indexing, query, sn7]

---

# Search engine

In an enterprise content management platform such as **sensenet** one of the most important technical aspects is how we index content items and how we search for them. Especially because sensenet is a *query based* system which means every content, every list you see on the UI is there because a query found it. In this article we describe the search module and how developers can customize it.

You may configure which search engine you want to use for indexing and querying by setting the `SearchEngine` value in the `providers` web.config section. 

> Please note that this decision is usually made in development time. Although it is possible to switch to another search engine on a portal later, it will involve reindexing the whole content repository.

The whole point of having a search module is that it provides a **generic search interface** for the content repository, a technology-independent search layer. Queries are expressed in a text or code (LINQ) format and are compiled to the language of the configured engine. Developers may create and execute queries without knowing which engine will execute them.

## Query and Indexing engines

[Content Query](/docs/content-query.md) has always been one of the most important features in sensenet. It is a *text-based* query that is compiled to an *expression object* understandable by the system and executed by the query engine.

The search engine is responsible for providing these two important components: 

The **query engine** is responsible for executing a query that is already compiled to a generic *SnQuery* object. It compiles the query to the language understandable by the underlying index technology. In sensenet the current default implementation uses the [Lucene engine](https://github.com/SenseNet/sn-search-lucene29) for indexing and querying content items.

The **indexing engine** is an implementation of an interface that defines methods for writing *IndexDocuments* to the index. These index documents are generic, they hold all the meta information by which we can search for content items in the repository. To learn more about how indexing works in sensenet, please visit the [Field indexing](/docs/field-indexing.md) article.

## Customization
From version 7.1 sensenet provides the `ISearchEngine`, `IIndexingEngine` and `IQueryEngine` interfaces that let developers create their own query and index implementations.

Currently the built-in search engine works the same way as before: using a local [Lucene index](https://github.com/SenseNet/sn-search-lucene29). In the future we will provide new official implementations that will be able to work as a service in the modern cloud environment.