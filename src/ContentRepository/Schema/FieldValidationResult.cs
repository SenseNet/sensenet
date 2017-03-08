using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Schema
{
    public class FieldValidationResult
    {
        public static readonly FieldValidationResult Successful;
        private Dictionary<string, object> _parameters;
        public string Category { get; private set; }

        static FieldValidationResult()
        {
            Successful = new FieldValidationResult("Successful");
        }
        public FieldValidationResult(string category)
        {
            Category = category;
        }

        public void AddParameter(string name, object value)
        {
            if (_parameters == null)
                _parameters = new Dictionary<string, object>();
            _parameters.Add(name, value);
        }
        public object GetParameter(string name)
        {
            if (_parameters == null)
                return null;
            if (!_parameters.ContainsKey(name))
                return null;
            return _parameters[name];
        }
        public string[] GetParameterNames()
        {
            if (_parameters == null)
                return new string[0];
            return _parameters.Keys.ToArray<string>();
        }
        public string FormatMessage(string formatString)
        {
            // For example: "Minimum length is {MinLength} but current length is {CurrentLength}."
            if (_parameters == null)
                return formatString;
            string result = formatString;
            foreach (var name in _parameters.Keys)
                result = result.Replace(String.Concat("{", name, "}"), _parameters[name].ToString());
            return result;
        }
    }
}
