---
title:  "Choice Field"
source_url: 'https://github.com/SenseNet/sensenet/blob/jwt/docs/choice-field.md'
category: Concepts
version: v6.0
tags: [fields, choice, select, checkbox, radiobutton]
---

# Choice Field

Choice Field is a multi-purpose field used to allow the user to choose one or more options. It can be rendered as a select tag, radio buttons, or checkboxes.

Choice field as a dropdownlist can be used just the same as a common html select. The options could have the standard properties like selected, disabled, label and value. Value and label properties and the text of the option could be localized.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/fields/Select.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/fields/Radiobuttongroup.png" style="margin: 20px auto" />

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/fields/Checkboxes.png" style="margin: 20px auto" />

The chosen option is indexed in the fulltext index by all its localized values and texts (so you can search by value or text) and indexed in field index by its value. If you wanna sort list items by a choice field, they will be ordered by the selected options value and the items with extravalue will be at the end of the list.

## Enum options

The choice field can be built up using an Enumeration defined in source code.

```xml
<Options>
  <Enum type="SenseNet.Search.FilterStatus" resourceClass="MyResourceClassName" />
</Options>
```

In that case, you do not have to modify the CTD to localize the options of the field.

## Field handler

- handler: *SenseNet.ContentRepository.Fields.ChoiceField*
- short name: **Choice**

Usage in CTD:

```xml
   <Field name="Data1" type="Choice">
   ...
   <Configuration>
      <Options>
          <Option value="1" selected="true">value 1</Option>
          <Option value="2">value 2</Option>
          ...
      </Options>
   </Configuration>
   </Field>
```

## Supported Field Controls

- [DropDown Field Control](dropdown-fieldcontrol.md): a select box, which allows the user to choose one value from an option list.
- [RadioButtonGroup Field Control](radiobuttongroup-fieldcontrol.md): group of selectable radiobuttons.
- [CheckBoxGroup Field Control](checkboxgroup-fieldcontrol.md): group of selectable checkboxes.

## Configuration

The following properties can be set in the field's configuration:

- **AllowMultiple**: (optional) allows multiple selection.
- **AllowExtraValue**: (optional) allows to add an extra value to the field.
- **DisplayChoices**: (optional) specifies the type of the field control which will handle the current field ('DropDown','RadioButtons','CheckBoxes').

## Example

Fully featured example:

```xml
<Field name="Style" type="Choice">
      <DisplayName>Style</DisplayName>
      <Description>This field contains the style of the car</Description>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>true</AllowExtraValue>
        <Options>
          <Option value="Sedan" selected='true'>Sedan</Option>
          <Option value="Coupe">Coupe</Option>
          <Option value="Cabrio">Cabrio</Option>
          <Option value="Roadster">Roadster</Option>
          <Option value="SUV">SUV</Option>
          <Option value="Van">Van</Option>
        </Options>
        <DisplayChoices>RadioButtons</DisplayChoices>
      </Configuration>
</Field>
```

The above example configures the Choice field so that:

- it is not allowed to choose more than one option
- it is allowed to add an extra value
- there's a list of the options
- the field will be displayed as list of radiobuttons