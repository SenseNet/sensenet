using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Diagnostics;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AuditLogTests : TestBase
    {
        private class TestAuditEvent : IAuditEvent
        {
            public int EventId { get; } = 1;
            public string Message { get; } = "Message";
            public string Title { get; } = "Title";
        }

        [TestMethod]
        public void AuditEventInfo_EncodeProperty()
        {
            var testEvent = new TestAuditEvent();
            var eventInfo = new AuditEventInfo(testEvent, new Dictionary<string, object>()
            {
                { "Prop1", "<a>html value &'\"</a>"}
            });

            var xDoc = new XmlDocument();
            xDoc.LoadXml(eventInfo.FormattedMessage);

            // Note: we use the InnerXml property instead of InnerText because it gives us 
            // the raw (undecoded) value instead of a decoded xml format.
            var propValue = xDoc.SelectSingleNode("/LogEntry/ExtendedProperties/Prop1")?.InnerXml;

            Assert.AreEqual("&lt;a&gt;html value &amp;'\"&lt;/a&gt;", propValue);
        }
    }
}
