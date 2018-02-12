---
title: "Field Setting"
source_url: 'https://github.com/SenseNet/sensenet/docs/field-setting-for-developers.md'
category: Development
version: v6.0
tags: [field, field, setting, content type, content type definition]
---

# Field Setting

The FieldSetting object is the implementation of the configuration element in a CTD. Every field has a FieldSetting object.

The FieldSetting object can be accessed via a property on every field:

```csharp
public abstract class Field
{
   public FieldSetting FieldSetting { get; private set; }
```

## Field and FieldSetting

The FieldSetting is an important extensibility point in the sensenet [Content Repository](content-repository.md). While the Field implements the usable .NET data type, the FieldSetting describes the type of the information stored in a semantic way. For example the IntegerField uses the System.Int32 type which is a technical type, whereas the IntegerFieldSetting can specify that the field's value must be between 1 and 80, which makes the field more suitable for storing lottery numbers.

## Default FieldSetting for a Field

Every field type has a default FieldSetting type, that is declared on the field implementation with the DefaultFieldSettingAttribute:

```csharp
[DefaultFieldSetting(typeof(ShortTextFieldSetting))]
public class ShortTextField : Field
```

## FieldSetting declaration in CTD

To define a specific FieldSetting implementation for a field, use the handler attribute of the Configuration element in the CTD providing the fully qualified name of the desired FieldSetting type:

```xml
    <Field name="Email" type="ShortText">
      <Configuration handler="SenseNet.ContentRepository.Fields.ActivationFieldSetting">
        <Enabled>true</Enabled>
        <MailFrom></MailFrom>
        <MailDefinition>
          Click the following link to activate your account:
          ##ActivationLink##
        </MailDefinition>
        <MailSubject>Activation subject</MailSubject>
        <IsBodyHtml>false</IsBodyHtml>
        <MailPriority>Low</MailPriority>
      </Configuration>
    </Field>
```

In this case the type of the field is *ShortText*, however we use the *ActivationFieldSetting* type instead of the more general *ShortTextFieldSetting* type. As you see the contents of the *Configuration* element depends on the used FieldSetting type.

## Built-in FieldSettings

The following diagram shows the built-in field types of sensenet:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/BuiltinFieldSettings.png" style="margin: 20px auto" />

## Common configuration elements

For a list and description of common configuration elements please refer to the following article:

- [CTD#Field definition](ctd.md)

## Validation mechanism

The FieldSetting not only specifies the type of information but it also validates according to its implementation. Validation can happen automatically on saving a content, or manually from code. The Field is able to store the validation status (valid, invalid, reason of error), thus if a validation has occurred the field will be revalidated if its data has changed or a previous validation has failed. When developing user interaction handling code we don't come across the validation mechanism, since it is done in the background. There are some properties however that indicate if a validation has failed or not. The following code snippet is an example for handling validation and other problems when using [content views](content-views.md):

```csharp
    contentView.UpdateContent();
    if (contentView.IsUserInputValid && content.IsValid)
    {
        try
        {
            content.Save();
        }
        catch (Exception ex)
        {
            Logger.WriteException(ex);
            contentView.ContentException = ex;
        }
    }
```

When a content view updates the underlying content all fields are validated by their field settings and the view's controls can access the validation results. If there is a problem with any field data the content's IsValid property will be set to false and the control can write back the reason to the form.