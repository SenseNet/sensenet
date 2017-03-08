using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace SenseNet.ContentRepository.Fields
{
    [Serializable]
    public class SurveyRule
    {
        public static readonly string EXTRAVALUEID = "*#*EXTRAVALUE*#*";

        private string _answer;
        private string _answerId;
        private string _jumpToPage;
        private int _pages;

        public string Answer
        {
            get { return _answer; }
            set { _answer = value; }
        }

        public string AnswerId
        {
            get { return _answerId; }
            set { _answerId = value; }
        }

        public string JumpToPage
        {
            get { return _jumpToPage; }
            set { _jumpToPage = value; }
        }
        public int Pages
        {
            get { return _pages; }
            set { _pages = value; }
        }


        public SurveyRule(string answer, string answerId, string jumpToPage, int pages)
		{
            _answer = answer;
            _answerId = answerId;
            _jumpToPage = jumpToPage;
            _pages = pages;
		}

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Rule");
            writer.WriteAttributeString("Answer", this._answer);
            writer.WriteAttributeString("AnswerId", this._answerId);

            writer.WriteString(this._jumpToPage);

            writer.WriteEndElement();
            writer.Flush();
        }

        public static string GetExtraValueText()
        {
            return HttpContext.GetGlobalResourceObject("Survey", "ExtraValueText") as string;
        }
    }
}
