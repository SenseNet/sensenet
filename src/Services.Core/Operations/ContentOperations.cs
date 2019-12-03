using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Operations
{
    public static class ContentOperations
    {
        [ODataAction]
        [ContentType(N.Folder)]
        [AllowedRoles(N.Everyone)]
        [Scenario(N.GridToolbar)]
        public static BatchActionResponse CopyBatch(Content content, string targetPath, object[] paths)
        {
            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new ContentNotFoundException(targetPath);

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = paths.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    var copy = node.CopyToAndGetCopy(targetNode);
                    results.Add(new { copy.Id, copy.Path, copy.Name });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new { node?.Id, node?.Path, node?.Name },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }

        [ODataAction]
        [ContentType(N.Folder)]
        [AllowedRoles(N.Everyone)]
        [Scenario(N.GridToolbar)]
        public static BatchActionResponse MoveBatch(Content content, string targetPath, object[] paths)
        {
            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new ContentNotFoundException(targetPath);

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = paths.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    node.MoveTo(targetNode);
                    results.Add(new { node.Id, node.Path, node.Name });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new { node?.Id, node?.Path, node?.Name },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }


        [ODataAction]
        [ContentType(N.Folder)]
        [AllowedRoles(N.Everyone)]
        [Scenario(N.GridToolbar)]
        public static BatchActionResponse DeleteBatch(Content content, bool permanent, object[] paths)
        {
            // no need to throw an exception if no ids are provided: we simply do not have to delete anything
            if(paths == null || paths.Length == 0)
                return null;

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = paths.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    switch (node)
                    {
                        case GenericContent gc:
                            gc.Delete(permanent);
                            break;
                        case ContentType ct:
                            ct.Delete();
                            break;
                    }

                    results.Add(new { node.Id, node.Path, node.Name });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new { node?.Id, node?.Path },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }
    }
}
