using System;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Sharing;

namespace SenseNet.ContentRepository.Fields
{
    //UNDONE: implement SharingInfo field

    [ShortName("Sharing")]
    [DataSlot(0, RepositoryDataType.Text, typeof(SharingHandler))]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    public class SharingField : Field
    {
        protected override bool HasExportData => true;

        public override object GetData()
        {
            // this cannot be null as it would prevent the system exporting the field
            return string.Empty;
        }
        public override void SetData(object value)
        {
            // Do nothing, sharing info is edited only through the sharing API 
            // or the ImportData method of this class.
        }

        protected override void ExportData(XmlWriter writer, ExportContext context)
        {
            throw new NotImplementedException();

            //var wm = RepositoryEnvironment.WorkingMode;
            //if (wm.Exporting)
            //    ExportDataPath(writer, context);
            //else
            //    ExportDataId(writer);
        }
        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            throw new NotImplementedException();
        }
    }
}