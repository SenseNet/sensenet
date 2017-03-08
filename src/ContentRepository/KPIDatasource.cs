using System;
using System.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using System.Xml;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class KPIDatasource : Folder
    {
        // ===================================================== Constructors

        public KPIDatasource(Node parent) : this(parent, null) { }
		public KPIDatasource(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected KPIDatasource(NodeToken nt) : base(nt) { }

        // ===================================================== Properties

        [RepositoryProperty("KPIData", RepositoryDataType.Text)]
        public virtual string KPIData
        {
            get
            {
                return base.GetProperty<string>("KPIData");
            }
            set
            {
                this["KPIData"] = value;
            }
        }

        private List<KPIData> _kpiDataList;

        public List<KPIData> KPIDataList
        {
            get { return _kpiDataList ?? (_kpiDataList = new List<KPIData>()); }
        }

        // ===================================================== Property get/set

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "KPIData":
                    return this.KPIData;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "KPIData":
                    this.KPIData = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ===================================================== Helper methods

        public KPIData GetKPIData(string key)
        {
            KPIData kpiData = null;
            if (!string.IsNullOrEmpty(key))
                kpiData = this.KPIDataList.FirstOrDefault(kd => kd.Label == key);

            return kpiData ?? new KPIData(string.Empty, 0, 0);
        }

        // ===================================================== Cached data

        private const string CACHEDKPIDATAKEY = "CachedKPIData";
        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            var kpiData = (IDictionary<string, object>)base.GetCachedData(CACHEDKPIDATAKEY);
            if (kpiData != null)
            {
                SetExtendedData(kpiData);
                return;
            }

            kpiData = GetExtendedData();
            base.SetCachedData(CACHEDKPIDATAKEY, kpiData);
        }

        public IDictionary<string, object> GetExtendedData()
        {
            _kpiDataList = new List<KPIData>();
            var kpiDataDict = new Dictionary<string, object>();
            var kpiXmlValue = new XmlDocument();

            try
            {
                if (!string.IsNullOrEmpty(this.KPIData))
                    kpiXmlValue.LoadXml(this.KPIData);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            var kpiDataNodes = kpiXmlValue.SelectNodes("KPI/data");
            if (kpiDataNodes != null)
            {
                foreach (XmlNode kpiDataNode in kpiDataNodes)
                {
                    var key = kpiDataNode.Attributes["Label"] == null ? string.Empty : kpiDataNode.Attributes["Label"].Value;
                    if (string.IsNullOrEmpty(key) || kpiDataDict.ContainsKey(key)) 
                        continue;

                    var goal = kpiDataNode.SelectSingleNode("goal");
                    var actual = kpiDataNode.SelectSingleNode("actual");
                    if (goal == null || actual == null) 
                        continue;

                    int goalValue;
                    int actualValue;
                    
                    if (!int.TryParse(goal.InnerText, out goalValue) || !int.TryParse(actual.InnerText, out actualValue))
                        continue;

                    var kpiData = new KPIData(key, goalValue, actualValue);
                    kpiDataDict.Add(key, kpiData);
                    _kpiDataList.Add(kpiData);
                }
            }

            return kpiDataDict;
        }

        public void SetExtendedData(IDictionary<string, object> data)
        {
            _kpiDataList = new List<KPIData>();

            foreach (var key in data.Keys)
            {
                _kpiDataList.Add(data[key] as KPIData);
            }
        }
    }
}
