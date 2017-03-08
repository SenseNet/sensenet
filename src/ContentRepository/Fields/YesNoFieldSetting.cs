using System.Collections.Generic;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    public class YesNoFieldSetting : ChoiceFieldSetting
    {
        public const string YesValue = "yes";
        public const string NoValue = "no";

        public YesNoFieldSetting()
        {
            SetDefaultsInternal();
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            SetDefaultsInternal();
        }

        protected void SetDefaultsInternal()
        {
            _allowMultiple = false;
            _allowExtraValue = false;

            _options = new List<ChoiceOption>
                           {
                               new ChoiceOption(YesValue, SenseNetResourceManager.GetResourceKey("Ctd", "Enum-FieldSettingContent-YesNo-Yes")), 
                               new ChoiceOption(NoValue, SenseNetResourceManager.GetResourceKey("Ctd", "Enum-FieldSettingContent-YesNo-No"))
                           };
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            FieldSetting fieldSetting;

            fieldSetting = fmd[AllowExtraValueName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fieldSetting = fmd[AllowMultipleName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fieldSetting = fmd[OptionsName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fieldSetting = fmd[DisplayChoicesName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fieldSetting = fmd[CompulsoryName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            var fs = new ChoiceFieldSetting
            {
                Name = DefaultValueName,
                DisplayName = GetTitleString(DefaultValueName),
                Description = GetDescString(DefaultValueName),
                FieldClassName = typeof(ChoiceField).FullName,
                AllowMultiple = false,
                AllowExtraValue = false,
                Options = new List<ChoiceOption>(_options),
                DisplayChoice = Fields.DisplayChoice.DropDown,
                VisibleBrowse = FieldVisibility.Show,
                VisibleEdit = FieldVisibility.Show,
                VisibleNew = FieldVisibility.Show
            };

            fmd[DefaultValueName].FieldSetting = fs;

            return fmd;
        }
    }
}
