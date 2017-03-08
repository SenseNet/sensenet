using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Reflection;

namespace SenseNet.ContentRepository.Storage
{
	public class XmlValidator
	{
		private XmlSchema _schema;
		private List<ValidationEventArgs> _errors;
		private List<ValidationEventArgs> _warnings;

		public List<ValidationEventArgs> Errors
		{
			get { return _errors; }
		}
		public List<ValidationEventArgs> Warnings
		{
			get { return _warnings; }
		}
		public bool HasError
		{
			get { return _errors.Count > 0; }
		}
		public bool HasWarning
		{
			get { return _warnings.Count > 0; }
		}

		public XmlValidator()
		{
			_errors = new List<ValidationEventArgs>();
			_warnings = new List<ValidationEventArgs>();
		}

		public void LoadSchema(string schemaXml)
		{
			if (schemaXml == null)
				throw new ArgumentNullException("schemaXml");

			ClearErrors();
			StringReader stringReader = new StringReader(schemaXml);
			XmlTextReader xmlReader = new XmlTextReader(stringReader);
			_schema = XmlSchema.Read(xmlReader, ValidationCallback);
		}
		public void LoadSchema(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			ClearErrors();
			_schema = XmlSchema.Read(stream, ValidationCallback);
		}
        public static XmlValidator LoadFromManifestResource(Assembly resourceOwner, string manifestResourceName)
        {
            if (manifestResourceName == null)
                throw new ArgumentNullException("manifestResourceName");
            if (resourceOwner == null)
                throw new ArgumentNullException("resourceOwner");
            Stream stream = resourceOwner.GetManifestResourceStream(manifestResourceName);
            if (stream == null)
                throw new ArgumentException(String.Concat("Resource is not found. Assembly: ", resourceOwner.FullName, ", Resource name: ", manifestResourceName));
            XmlValidator schema = new XmlValidator();
            schema.LoadSchema(stream);
            return schema;
        }

		public bool Validate(string xml)
		{
			if (xml == null)
				throw new ArgumentNullException("xml");
			if (_schema == null)
				throw new InvalidOperationException(SR.Exceptions.XmlSchema.Msg_SchemaNotLoaded);

			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
			settings.ValidationType = ValidationType.Schema;
			settings.Schemas = new XmlSchemaSet();
			settings.Schemas.Add(_schema);
			StringReader stringReader = new StringReader(xml);
			XmlReader xmlValidator = XmlReader.Create(stringReader, settings);
			ClearErrors();
			while (xmlValidator.Read()) ;
			return !HasError;
		}
		public bool Validate(IXPathNavigable xml)
		{
			if (xml == null)
				throw new ArgumentNullException("xml");
			if (_schema == null)
				throw new InvalidOperationException(SR.Exceptions.XmlSchema.Msg_SchemaNotLoaded);

			XmlSchemaSet schema = new XmlSchemaSet();
			schema.Add(_schema);
			ClearErrors();

			XPathNavigator nav = xml.CreateNavigator();
			nav.MoveToFirstChild();

			if (schema.Schemas(nav.NamespaceURI).Count > 0)
				return xml.CreateNavigator().CheckValidity(schema, ValidationCallback);

			return false;
		}

		private void ClearErrors()
		{
			_errors.Clear();
			_warnings.Clear();
		}
		private void ValidationCallback(object sender, ValidationEventArgs e)
		{
			switch (e.Severity)
			{
				case XmlSeverityType.Error:
					_errors.Add(e);
					break;
				case XmlSeverityType.Warning:
					_warnings.Add(e);
					break;
				default:
					break;
			}
		}
    }
}