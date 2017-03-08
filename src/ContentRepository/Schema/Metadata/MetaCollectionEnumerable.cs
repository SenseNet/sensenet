using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema.Metadata
{
    public abstract class MetaCollectionEnumerable<T> : IEnumerable<T> where T : IMetaNode
    {
        public abstract IEnumerator<T> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class MetaClassEnumerable : MetaCollectionEnumerable<Class>
    {
        private string[] _blackList;

        public MetaClassEnumerable(string[] disabledContentTypeNames = null)
        {
            _blackList = disabledContentTypeNames;
            if (_blackList != null && _blackList.Length == 0)
                _blackList = null;
        }

        public override IEnumerator<Class> GetEnumerator()
        {
            // create dictionaries
            var classes = ContentType.GetContentTypes().ToDictionary(c => c.Name, c => new Class(c));
            var parents = classes.Values.ToDictionary(c => c,
                c => c.BaseClassName == null ? null : classes[c.BaseClassName]);

            // set effective see for all (Enabled property)
            foreach (var @class in classes.Values)
                @class.Enabled = @class.ContentType.Security.HasPermission(PermissionType.See);

            // set enable on parent chain (Enabled property)
            foreach (var @class in parents.Keys)
            {
                if (!@class.Enabled)
                    continue;
                Class parentClass = @class;
                while ((parentClass = parents[parentClass]) != null)
                    parentClass.Enabled = true;
            }

            // disable blacklisted classes
            if (_blackList != null)
            {
                foreach (var @class in parents.Keys)
                {
                    if (!@class.Enabled)
                        continue;
                    if (_blackList.Any(x => @class.IsInstaceOfOrDerivedFrom(x)))
                        @class.Enabled = false;
                }
            }

            // enumerate only enabled classes

            foreach (var @class in classes.Values.Where(c=>c.Enabled))
                yield return @class;
        }
    }

    public class MetaPropertyEnumerable : MetaCollectionEnumerable<Property>
    {
        private Class _class;
        private IEnumerable<FieldSetting> _fieldSettings;

        public MetaPropertyEnumerable(IEnumerable<FieldSetting> fieldSettings, Class @class)
        {
            _class = @class;
            _fieldSettings = fieldSettings;
        }

        public override IEnumerator<Property> GetEnumerator()
        {
            foreach (var fieldSetting in _fieldSettings)
                yield return new Property(fieldSetting, _class);
        }
    }

    public class MetaEnumOptionEnumerable : MetaCollectionEnumerable<EnumOption>
    {
        private Class _class;
        private ChoiceFieldSetting _choiceFielSetting;

        public MetaEnumOptionEnumerable(ChoiceFieldSetting choiceFieldSetting, Class @class)
        {
            _class = @class;
            _choiceFielSetting = choiceFieldSetting;
        }

        public override IEnumerator<EnumOption> GetEnumerator()
        {
            var enumTypeName = _choiceFielSetting.EnumTypeName;
            if (string.IsNullOrEmpty(enumTypeName))
            {
                var devNames = new List<string>();
                var index = 0;
                foreach (var option in _choiceFielSetting.Options)
                {
                    var name = option.StoredText ?? option.Text;
                    var devName = GetDevName(name, devNames, index++);
                    var value = option.Value;

                    yield return new EnumOption(devName, value, name, option, _choiceFielSetting, _class);
                }
            }
            else
            {
                var enumType = TypeResolver.GetType(enumTypeName);
                var names = Enum.GetNames(enumType);
                var values = Enum.GetValues(enumType).Cast<int>().ToArray();
                for (var i = 0; i < names.Length; i++)
                    yield return new EnumOption(names[i], values[i].ToString(), null, null, _choiceFielSetting, _class);
            }
        }
        private string GetDevName(string name, List<string> allNames, int index)
        {
            name = name.Split('.', ',', '-').Last();
            var chars = name.Where(char.IsLetterOrDigit).ToArray();
            if (chars.Length > 0)
            {
                if (char.IsLetter(chars[0]) || chars[0] == '_')
                {
                    name = new string(chars);
                    if (!allNames.Contains(name))
                    {
                        allNames.Add(name);
                        return name;
                    }
                }
            }

            return $"Option{index}";
        }
    }
}
