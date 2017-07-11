using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Web;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Fields
{
    public class PasswordCheckResult
    {
        /// <summary>
        /// Score indicating the strength of the password
        /// </summary>
        public int Score { get; set; }
        /// <summary>
        /// Validation message
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Indicates if the password is considered acceptable or not
        /// </summary>
        public bool Valid { get; set; }
    }

    public class PasswordFieldValidationResult : FieldValidationResult
    {
        public PasswordCheckResult CheckResult { get; set; }

        public PasswordFieldValidationResult(string category)
            : base(category)
        {
        }
    }

    public class PasswordFieldSetting : ShortTextFieldSetting
    {
        public const string DoNotMatchError = "PasswordsDoNotMatch";
        public const string OriginalIsInvalidError = "OriginalPasswordIsInvalid";
        public const string CannotBeNullError = "PasswordCannotBeNull";
        public const string DataMustBePasswordData = "Data must be PasswordField.PasswordData";

        public const string ReenterTitleName = "ReenterTitle";
        public const string ReenterDescriptionName = "ReenterDescription";
        public const string PasswordHistoryLengthName = "PasswordHistoryLength";

        private string _reenterTitle;
        private string _reenterDescription;
        private int? _passwordHistoryLength;

        public string ReenterTitle
        {
            get
            {
                if (_reenterTitle != null)
                    return SenseNetResourceManager.Current.GetString(_reenterTitle);

                var parentPasswordFieldSetting = this.ParentFieldSetting as PasswordFieldSetting;
                if (parentPasswordFieldSetting == null)
                    return null;
                return parentPasswordFieldSetting.ReenterTitle;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ReenterTitle is not allowed within readonly instance.");
                _reenterTitle = value;
            }
        }
        public string ReenterDescription
        {
            get
            {
                if (_reenterDescription != null)
                    return SenseNetResourceManager.Current.GetString(_reenterDescription);

                var parentPasswordFieldSetting = this.ParentFieldSetting as PasswordFieldSetting;
                if (parentPasswordFieldSetting == null)
                    return null;
                return parentPasswordFieldSetting.ReenterDescription;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ReenterDescription is not allowed within readonly instance.");
                _reenterDescription = value;
            }
        }
        public int PasswordHistoryLength
        {
            get
            {
                if (_passwordHistoryLength.HasValue)
                    return _passwordHistoryLength.Value;
                var parentPasswordFieldSetting = this.ParentFieldSetting as PasswordFieldSetting;
                if (parentPasswordFieldSetting == null)
                    return 0;
                return parentPasswordFieldSetting.PasswordHistoryLength;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ReenterDescription is not allowed within readonly instance.");
                _passwordHistoryLength = value;
            }
        }


        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            if (configurationElement == null) throw new ArgumentNullException("configurationElement");
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);
            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case ReenterTitleName:
                        _reenterTitle = node.InnerXml;
                        break;
                    case ReenterDescriptionName:
                        _reenterDescription = node.InnerXml;
                        break;
                    case PasswordHistoryLengthName:
                        _passwordHistoryLength = Int32.Parse(node.InnerXml);
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _reenterTitle = GetConfigurationStringValue(info, ReenterTitleName, null);
            _reenterDescription = GetConfigurationStringValue(info, ReenterDescriptionName, null);
            _passwordHistoryLength = GetConfigurationNullableValue<int>(info, PasswordHistoryLengthName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(ReenterTitleName, _reenterTitle);
            result.Add(ReenterDescriptionName, _reenterDescription);
            result.Add(PasswordHistoryLengthName, _passwordHistoryLength);
            return result;
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();
            this._reenterDescription = null;
            this._reenterTitle = null;
            this._passwordHistoryLength = null;
        }
        public override FieldValidationResult ValidateData(object value, Field field)
        {
            if (value == null)
                return new FieldValidationResult(CannotBeNullError);

            var data = value as PasswordField.PasswordData;
            if (data == null)
            {
                var sdata = value as string;
                if (sdata != null)
                    data = new PasswordField.PasswordData { Text = sdata };
            }
            if (data == null)
                return new FieldValidationResult(DataMustBePasswordData);

            // check password
            var hash = data.Hash;
            if (!String.IsNullOrEmpty(data.Text))
            {
                hash = ((PasswordField)field).EncodePassword(data.Text);
                data.Hash = hash;
            }

            var user = field.Content.ContentHandler as User;
            if (user == null)
            {
                var checkResult = this.CheckPassword(hash, null);
                if (!checkResult.Valid)
                {
                    var validationResult = new PasswordFieldValidationResult(checkResult.Message);
                    validationResult.CheckResult = checkResult;
                    return validationResult;
                }
            }
            else
            {
                var checkResult = this.CheckPassword(hash, user.GetOldPasswords());
                if (!checkResult.Valid)
                {
                    var validationResult = new PasswordFieldValidationResult(checkResult.Message);
                    validationResult.CheckResult = checkResult;
                    return validationResult;
                }
            }

            var origValue = field.OriginalValue as PasswordField.PasswordData;
            bool isValidOriginalValue = !String.IsNullOrEmpty(origValue.Hash) || this.Compulsory != true;

            if (data.Text == null && data.Hash == null && !isValidOriginalValue)
                return new FieldValidationResult(OriginalIsInvalidError);

            if (data.Text != null)
                return base.ValidateData(data.Text, field);

            return FieldValidationResult.Successful;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            WriteElement(writer, this._reenterTitle, ReenterTitleName);
            WriteElement(writer, this._reenterDescription, ReenterDescriptionName);
            WriteElement(writer, this._passwordHistoryLength, PasswordHistoryLengthName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var pwdFieldSetting = (PasswordFieldSetting)source;

            ReenterTitle = pwdFieldSetting.ReenterTitle;
            ReenterDescription = pwdFieldSetting.ReenterDescription;
            PasswordHistoryLength = pwdFieldSetting.PasswordHistoryLength;

        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Add("ReenterTitle", new FieldMetadata
            {
                FieldName = "ReenterTitle",
                PropertyType = typeof(string),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(string)),
                DisplayName = "ReenterTitle",
                CanRead = true,
                CanWrite = true
            });

            fmd.Add("ReenterDescription", new FieldMetadata
            {
                FieldName = "ReenterDescription",
                PropertyType = typeof(string),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(string)),
                DisplayName = "ReenterDescription",
                CanRead = true,
                CanWrite = true
            });

            fmd.Add("PasswordHistoryLength", new FieldMetadata
            {
                FieldName = "PasswordHistoryLength",
                PropertyType = typeof(int?),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(int?)),
                DisplayName = "PasswordHistoryLength",
                CanRead = true,
                CanWrite = true
            });

            return fmd;
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new SenseNet.Search.Indexing.NotIndexedIndexFieldHandler();
        }

        public virtual PasswordCheckResult CheckPassword(string passwordHash, List<PasswordField.OldPasswordData> oldPasswords)
        {
            // default password check: every password is considered valid if the periodicity is valid
            return CheckRecurrence(passwordHash, oldPasswords);
        }
        public virtual PasswordCheckResult CheckRecurrence(string passwordHash, List<PasswordField.OldPasswordData> oldPasswords)
        {
            return CheckRecurrence(passwordHash, oldPasswords, PasswordHistoryLength);
        }
        private PasswordCheckResult CheckRecurrence(string passwordHash, List<PasswordField.OldPasswordData> oldPasswords, int passwordHistoryLength)
        {
            if (oldPasswords != null && oldPasswords.Count > 0)
            {
                var passwords = oldPasswords.OrderByDescending(k => k.ModificationDate).ToList();
                var length = Math.Min(passwordHistoryLength, oldPasswords.Count);
                for (int i = 0; i < length; i++)
                    if (passwords[i].Hash == passwordHash)
                        return new PasswordCheckResult { Valid = false, Message = String.Format("Password cannot be the same as one of the previous {0} passwords!", passwordHistoryLength) };
            }
            return new PasswordCheckResult { Valid = true };
        }
    }
}