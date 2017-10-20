﻿using System;
using System.Collections.Generic;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public enum IndexingMode { Default, Analyzed, AnalyzedNoNorms, No, NotAnalyzed, NotAnalyzedNoNorms }
    public enum IndexStoringMode { Default, No, Yes }
    public enum IndexTermVector { Default, No, WithOffsets, WithPositions, WithPositionsOffsets, Yes }

    public enum IndexFieldAnalyzer { Default, Keyword, Standard, Whitespace /*, Simple, Stop*/ }

    public interface IIndexableField
    {
        string Name { get; }
        object GetData(bool localized = true);

        bool IsInIndex { get; }
        bool IsBinaryField { get; }
        IEnumerable<IndexField> GetIndexFields(out string textExtract);
    }

    public interface IFieldIndexHandler
    {
        /// <summary>For SnQuery parser</summary>
        IndexValue Parse(string text);

        /// <summary>For LINQ</summary>
        IndexValue ConvertToTermValue(object value);

        IndexFieldAnalyzer GetDefaultAnalyzer();

        IEnumerable<string> GetParsableValues(IIndexableField field);
        IndexValueType IndexFieldType { get; }
        IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        string GetSortFieldName(string fieldName);

        IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract);
    }
    public interface IPerFieldIndexingInfo
    {
        IndexFieldAnalyzer Analyzer { get; set; }
        IFieldIndexHandler IndexFieldHandler { get; set; }

        IndexingMode IndexingMode { get; set; }
        IndexStoringMode IndexStoringMode { get; set; }
        IndexTermVector TermVectorStoringMode { get; set; }

        bool IsInIndex { get; }

        Type FieldDataType { get; set; }
    }

    public enum QueryFieldLevel { NotDefined = 0, HeadOnly = 1, NoBinaryOrFullText = 2, BinaryOrFullText = 3 }


    /// <summary>
    /// Defines query operations for general purposes.
    /// </summary>
    public interface IQueryEngine
    {
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// If there is any problem, throws an exception.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports mermission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: contains content identifier collection in the desired order defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context);
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem, throws an exception.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports mermission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: string value collection of the content property values.
        /// Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context);
    }

    /// <summary>
    /// Defines query operations for increasing performance purposes.
    /// </summary>
    public interface IMetaQueryEngine
    {
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// If there is any problem or the query is not executable in this compinent, returns with null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports mermission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: contains content identifier collection in the desired order defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        IQueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context);
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem or the query is not executable in this compinent, returns with null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports mermission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: string value collection of the content property values.
        /// Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        IQueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context);
    }

    /// <summary>
    /// Defines a permission checker method for authorize the query hit candidates.
    /// </summary>
    public interface IPermissionFilter
    {
        /// <summary>
        /// Authorizes a query hit candidate.
        /// </summary>
        bool IsPermitted(int nodeId, bool isLastPublic, bool isLastDraft);
    }
    public interface IPermissionFilterFactory
    {
        IPermissionFilter Create(SnQuery query, IQueryContext context);
    }

    public interface IQueryResult<out T>
    {
        IEnumerable<T> Hits { get; }
        int TotalCount { get; }
    }
}
