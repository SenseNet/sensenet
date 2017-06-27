using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.Portal.OData.Typescript
{
    internal class TypescriptFieldSettingsVisitor : TypescriptModuleWriter
    {
        public TypescriptFieldSettingsVisitor(TypescriptGenerationContext context, TextWriter writer) : base(context, writer) { }

        protected override IMetaNode VisitSchema(ContentRepository.Schema.Metadata.Schema schema)
        {
            #region Write filestart
            _writer.WriteLine(@"/**
 * @module FieldSettings
 * @preferred
 * 
 * @description Module for FieldSettings.
 *
 * FieldSetting object is the implementation of the configuration element in a Sense/Net Content Type Definition.
 * The FieldSetting of a Field contains properties that define the behavior of the Field - for example a Field can be configured as read only or compulsory to fill.
 * FieldSettings helps us to autogenerate type and schema TS files from Sense/Net CTDs and use these files to reach all the configuration options of the Content Types fields on
 * client-side e.g. for validation.
 *
 * This module also contains some FieldSetting related enums to use them as types in properties e.g. visibitily or datetime mode options.
 */ /** */

import { ComplexTypes } from './SN';

    /**
     * Enum for Field visibility values.
     */
    export enum FieldVisibility { Show, Hide, Advanced }
    /**
     * Enum for Field output method values.
     */
    export enum OutputMethod { Default, Raw, Text, Html }
    /**
     * Enum for Choice Field control values.
     */
    export enum DisplayChoice { DropDown, RadioButtons, CheckBoxes }
    /**
     * Enum for DateTime Field mode values.
     */
    export enum DateTimeMode { None, Date, DateAndTime }
    /**
     * Enum for DateTime Field precision values.
     */
    export enum DateTimePrecision { Millisecond, Second, Minute, Hour, Day }
    /**
     * Enum for LongText field editor values.
     */
    export enum TextType { LongText, RichText, AdvancedRichText }
    /**
     * Enum for HyperLink field href values.
     */
    export enum UrlFormat { Hyperlink, Picture }

    export class FieldSetting {
        Name: string;
        DisplayName?: string;
        Description?: string;
        Icon?: string;
        ReadOnly?: boolean;
        Compulsory?: boolean;
        DefaultValue?: string;
        OutputMethod?: OutputMethod;
        VisibleBrowse?: FieldVisibility;
        VisibleNew?: FieldVisibility;
        VisibleEdit?: FieldVisibility;
        FieldIndex?: number;
        DefaultOrder?: number;
        ControlHint?: string;

        constructor(options: IFieldSettingOptions) {
            this.Name = options.name || 'Content';
            this.DisplayName = options.displayName;
            this.Icon = options.icon;
            this.ReadOnly = options.readOnly;
            this.Compulsory = options.compulsory;
            this.DefaultValue = options.defaultValue;
            this.OutputMethod = options.outputMethod;
            this.VisibleBrowse = options.visibleBrowse;
            this.VisibleEdit = options.visibleEdit;
            this.VisibleNew = options.visibleNew;
            this.FieldIndex = options.fieldIndex;
            this.DefaultOrder = options.defaultOrder;
            this.ControlHint = options.controlHint;

        }
    }

    export interface IFieldSettingOptions {
        name?: string;
        displayName?: string;
        description?: string;
        icon?: string;
        readOnly?: boolean;
        compulsory?: boolean;
        defaultValue?: string;
        outputMethod?: OutputMethod;
        visibleBrowse?: FieldVisibility;
        visibleNew?: FieldVisibility;
        visibleEdit?: FieldVisibility;
        fieldIndex?: number;
        defaultOrder?: number;
        controlHint?: string;
    }
");
            #endregion

            // Do not call base because only classes will be read.
            _indentCount++;
            Visit(schema.Classes);
            _indentCount--;

            return schema;
        }

        protected override IEnumerable<Class> VisitClasses(IEnumerable<Class> classes)
        {
            var visitedClasses = base.VisitClasses(classes);

            foreach (var item in _usedFieldSettings)
                WriteFieldSettingType(item.Value, _fieldSettingOccurences[item.Key]);

            return visitedClasses;
        }

        private void WriteFieldSettingType(Type type, List<string> occurence)
        {
            var propertyInfos = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                       BindingFlags.Public)
                .Where(p => !TypescriptCtdVisitor.FieldSettingPropertyBlackList.Contains(p.Name))
                .ToDictionary(
                    p => p.Name,
                    p => GetPropertyTypeName(p.PropertyType)
                );

            var parentTypeName = type.BaseType?.Name ?? "##NULL";
            var usedIn = occurence.Count == 0 ? "" : $"Used in {string.Join(", ", occurence)}";

            WriteLine();
            WriteLine($"// {usedIn}");

            WriteLine($"export class {type.Name} extends {parentTypeName} {{");
            _indentCount++;
            foreach (var item in propertyInfos)
                WriteLine($"{item.Key}?: {item.Value};");
            WriteLine();

            WriteLine($"constructor(options: I{type.Name}Options) {{");
            _indentCount++;
            WriteLine($"super(options);");
            foreach (var item in propertyInfos)
                WriteLine($"this.{item.Key} = options.{item.Key.ToCamelCase()};");
            _indentCount--;
            WriteLine("}");
            _indentCount--;
            WriteLine("}");
            WriteLine();

            WriteLine($"export interface I{type.Name}Options extends I{parentTypeName}Options {{");
            _indentCount++;
            foreach (var item in propertyInfos)
                WriteLine($"{item.Key.ToCamelCase()}?: {item.Value};");
            _indentCount--;
            WriteLine("}");
        }
        private string[] _wellKnownEnums =
        {
            "FieldVisibility", "OutputMethod", "DisplayChoice", "DateTimeMode",
            "DateTimePrecision", "TextType", "UrlFormat"
        };
        protected override string GetPropertyTypeName(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];
            return _wellKnownEnums.Contains(type.Name) ? type.Name : base.GetPropertyTypeName(type);
        }

        protected override IMetaNode VisitClass(Class @class)
        {
            _currentContentTypeName = @class.Name;
            return base.VisitClass(@class);
        }

        protected override IEnumerable<Property> VisitProperties(IEnumerable<Property> properties)
        {
            if (properties.Any(p => Context.PropertyBlacklist.Contains(p.Name)))
                return base.VisitProperties(properties.Where(p => p.IsLocal && !Context.PropertyBlacklist.Contains(p.Name)).ToArray());
            return base.VisitProperties(properties);
        }

        private string _currentContentTypeName;
        private Dictionary<string, List<string>> _fieldSettingOccurences = new Dictionary<string, List<string>>();
        private Dictionary<string, Type> _usedFieldSettings = new Dictionary<string, Type>();
        protected override IMetaNode VisitProperty(Property property)
        {
            // Do not call base because only thi property will be read.
            if (!property.IsLocal)
                return property;

            var fieldSetting = property.FieldSetting;
            var fieldSettingType = fieldSetting.GetType();
            var name = fieldSettingType.FullName;

            List<string> contentTypeNames;
            if (!_fieldSettingOccurences.TryGetValue(name, out contentTypeNames))
            {
                AddParentChain(fieldSettingType);
                contentTypeNames = new List<string>();
                _fieldSettingOccurences.Add(name, contentTypeNames);
                _usedFieldSettings.Add(name, fieldSettingType);
            }
            if (!contentTypeNames.Contains(_currentContentTypeName))
                contentTypeNames.Add(_currentContentTypeName);

            return property;
        }

        private void AddParentChain(Type fieldSettingType)
        {
            var type = fieldSettingType.BaseType;
            while (type != null && type != typeof(FieldSetting) && !_usedFieldSettings.ContainsKey(type.FullName))
            {
                _usedFieldSettings.Add(type.FullName, type);
                _fieldSettingOccurences.Add(type.FullName, new List<string>());
                type = type.BaseType;
            }

        }
    }
}
