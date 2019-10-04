# Overview 
Content templates are predefined templates for creating new content. You can access the templated creation functionality from code as well. 

# Details
## Creating content from content template
Use the following syntax to create a content or a content structure from template: 

```c#
var content = ContentTemplate.CreateTemplated(parentpath, templatepath);
content.Save();
```

The CreateTemplated function has the following overloads:
```c#
// loads the first appropriate template for the provided content type, and creates a content under the specified targetPath
Content CreateTemplated(string contentTypeName, string templateName, string targetPath)
 
// creates a content under the specified parentPath using the template at the given templatePath
Content CreateTemplated(string parentPath, string templatePath)
 
// creates a content with the provided name (nameBase parameter) under the specified parent, from the given template
Content CreateTemplated(Node parent, Node template, string nameBase)
```

## Resolving Templates
A template for a given content type can be resolved with the GetTemplatesForType function. You can use the following syntax: 

```c#
// loads templates from /Root/ContentTemplates for the given content type (contentTypeName)
IEnumerable<T> GetTemplatesForType<T>(string contentTypeName)
 
// loads templates from under the given context (contextPath) for the given content type (contentTypeName)
IEnumerable<T> GetTemplatesForType<T>(string contentTypeName, string contextPath)
```

# Examples/Tutorials
**Creating a workspace from a template**
The following example shows a custom function that creates a workspace using the given template at the specified path: 

```c#
private static Content CreateWorkspace(string targetPath, string templatePath, Dictionary<string, object> properties = null)
{
    var parentPath = RepositoryPath.GetParentPath(targetPath);
    var name = RepositoryPath.GetFileName(targetPath);
    var parent = Node.LoadNode(parentPath);
    var template = Node.LoadNode(templatePath);
    var workspace = ContentTemplate.CreateTemplated(parent, template, name);
    workspace["Name"] = name;
    if (properties != null)
    {
        foreach (var key in properties.Keys)
        {
            workspace[key] = properties[key];
        }
    }
 
    workspace.Save();
    return workspace;
}
```

**Loading a given template**

The following example shows how to load the first corresponding template given the template name and the content type name: 

```c#
var templateName = "MyProjectWorkspaceTemplate";
var contentTypeName = "ProjectWorkspace";
var templatesForContentType = GetTemplatesForType<Node>(contentTypeName);
var template = from t in templatesForContentType
               where t.Name.Equals(templateName)
               select t;
if (template.Count() < 1)
    throw new InvalidOperationException(String.Format("There is no template with {0} name for {1} contentType.", templateName, contentTypeName));
var templateNode = template.FirstOrDefault();
```

## Related links 
[Content - for Developers](http://wiki.sensenet.com/Content_-_for_Developers)
[Node - for Developers](http://wiki.sensenet.com/Node_-_for_Developers)
[Getting started - developing applications](http://wiki.sensenet.com/Getting_started_-_developing_applications)

## References
There are no related external articles for this article. 