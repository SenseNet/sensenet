# Reference Field

The Reference [Field](field.md) is used for defining references to other content. When a content holds references pointing to other content (for example a group references users in its *Members* field) it is done using a Reference Field.

The following apply to the behavior of the Field:
- **number of references**: a Reference Field can be configured to hold either a *single* or *multiple* references.
- **default value**: one or more default references can be set via Field configuration.
- **types of referable content**: types of referable content can be restricted.
- **location of referable content**: location of referable content can be restricted.
- **set of referable content**: set of referable content can be defined with an optional query.
- **moving/renaming references**: when a referenced content is moved to another place or renamed it *does not break the connection* - the moved/renamed content is still visible in the Reference Field of the main content (this is because references are connected by content id, not path).
- **permission handling**: if the current user does not have see permissions to one of the references, that reference is not visible for the user.
- **copying content with references**: when a content with a Reference Field is copied, the newly created content will hold the same references as the source content. Copying source content references along with the source content does not affect this behavior, the newly created content will hold references to the *originally referenced content* and not the newly created copies.

### Field handler

- handler: *SenseNet.ContentRepository.Fields.ReferenceField*
- short name: *Reference*

Usage in CTD:

```xml
<Field name="Manager" type="Reference">
   ...
</Field>
```

### Supported Field Controls

- [ReferenceGrid Field Control](reference-grid-fieldcontrol.md): a simple grid displaying referenced content and provides a Content Picker to add references. The Content Picker can be configured through the Field's configuration.

### Configuration

The following properties can be set in the Field's Field Setting configuration:

- **AllowMultiple**: a boolean property defining whether multiple references are allowed or only a single content can be referenced. By default only a single reference is allowed.
- **AllowedTypes**: allowed content types can be defined by explicitly listing type names in *Type* xml elements. By default all content types can be referenced.
- **SelectionRoot**: allowed location of referable content can be defined by listing paths in *Path* xml elements. By default content can be referenced from under /Root
- **DefaultValue**: a default single content reference can be defined with its path. Default multiple references can be defined with a comma separated list. By default the Field contains no references.

> For a complete list of common Field Setting configuration properties see [CTD Field definition](content-type-definition.md#Field definition).

### For developers

Please check the [Node for developers](node-for-developers.md) article for advices on how can you work with reference properties in code.

## Examples/Tutorials

Fully featured example:

```xml
<Field name="MyReferenceField" type="Reference">
	<DisplayName>My reference field</DisplayName>
	<Description>Referenced content</Description>
	<Configuration>
		<AllowMultiple>true</AllowMultiple>
		<AllowedTypes>
			<Type>Folder</Type>
			<Type>File</Type>
		</AllowedTypes>
		<SelectionRoot>
			<Path>/Root/System</Path>
			<Path>/Root/Sites</Path>
		</SelectionRoot>
		<DefaultValue>/Root/System/SystemPlugins,/Root/System/Schema</DefaultValue>
	</Configuration>
</Field>
```

The above example configures the Reference Field so that:

- multiple references can be added
- only File and Folder content types can be added as references
- content can only be referenced from under _/Root/System_ and _/Root/Sites_
- by default _/Root/System/SystemPlugins_ and _/Root/System/Schema_ is referenced