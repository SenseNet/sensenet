using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.Packaging.Steps;
// ReSharper disable PossibleNullReferenceException

namespace SenseNet.Packaging.Steps.Internal
{
    public class MoveConfigElement : XmlEditorStep
    {
        public string Key { get; set; }
        public string SourceSection { get; set; }
        public string TargetSection { get; set; }
        public string DefaultValue { get; set; }


        protected override bool EditXml(XmlDocument doc, string path)
        {
            if (string.IsNullOrEmpty(Key))
                throw new ArgumentException("Missing 'Key'");
            if (string.IsNullOrEmpty(TargetSection))
                throw new ArgumentException("Missing 'TargetSection'");

            var sourceSection = string.IsNullOrEmpty(SourceSection) ? "appSettings" : SourceSection;
            var targetSection = TargetSection.Trim('/');
            var sourceXpath = $"/configuration/{sourceSection}/add[@key='{Key}']";
            var targetXpath = $"/configuration/{targetSection}/add[@key='{Key}']";

            var changed = false;
            string currentValue = null;

            var sourceElement = (XmlElement) doc.SelectSingleNode(sourceXpath);
            if (sourceElement != null)
            {
                currentValue = sourceElement.Attributes["value"]?.Value;
                sourceElement.ParentNode.RemoveChild(sourceElement);
                changed = true;
            }

            var targetElement = (XmlElement) doc.SelectSingleNode(targetXpath);
            if (targetElement != null)
            {
                currentValue = targetElement.Attributes["value"]?.Value;
                if (!string.IsNullOrEmpty(DefaultValue) && DefaultValue == currentValue)
                {
                    targetElement.ParentNode.RemoveChild(targetElement);
                    changed = true;
                }
                return changed;
            }
            
            if (currentValue == null || (!string.IsNullOrEmpty(DefaultValue) && DefaultValue == currentValue))
                return changed;

            // ensure section
            var targetSectionElement = EnsureTargetSection(TargetSection, doc);

            targetElement = doc.CreateElement("add");
            targetElement.SetAttribute("key", Key);
            targetElement.SetAttribute("value", currentValue);
            targetSectionElement.AppendChild(targetElement);

            return true;
        }

        private XmlElement EnsureTargetSection(string targetSection, XmlDocument doc)
        {
            var segments = targetSection.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var configSectionsElement = doc.SelectSingleNode("/configuration/configSections");
            if (configSectionsElement == null)
            {
                configSectionsElement = doc.CreateElement("configSections");
                doc.DocumentElement.InsertBefore(configSectionsElement, doc.DocumentElement.FirstChild);
            }

            var parentSectionElement = doc.DocumentElement;
            var parentSectionDefElement = configSectionsElement;
            XmlElement sectionElement = null;
            XmlElement sectionDefElement = null;
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                sectionElement = (XmlElement)parentSectionElement.SelectSingleNode(segment);
                if (sectionElement == null)
                {
                    sectionElement = doc.CreateElement(segment);
                    parentSectionElement.AppendChild(sectionElement);
                }

                var sectionDefElementName = i < segments.Length - 1 ? "sectionGroup" : "section";
                sectionDefElement = (XmlElement)parentSectionDefElement.SelectSingleNode($"{sectionDefElementName}[@name='{segment}']");
                if (sectionDefElement == null)
                {
                    sectionDefElement = doc.CreateElement(sectionDefElementName);
                    sectionDefElement.SetAttribute("name", segment);
                    if(sectionDefElementName == "section")
                        sectionDefElement.SetAttribute("type", "System.Configuration.NameValueSectionHandler");
                    parentSectionDefElement.AppendChild(sectionDefElement);
                }

                parentSectionElement = sectionElement;
                parentSectionDefElement = sectionDefElement;
            }

            return sectionElement;
        }
    }
}
