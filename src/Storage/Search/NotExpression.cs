using System;

namespace SenseNet.ContentRepository.Storage.Search
{
	public class NotExpression : Expression
	{
	    private Expression _expression;

		public Expression Expression
		{
			get { return _expression; }
		}

		public NotExpression(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			_expression = expression;
		}


		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("Not", NodeQuery.XmlNamespace);
			_expression.WriteXml(writer);
			writer.WriteEndElement();
		}
	}
}