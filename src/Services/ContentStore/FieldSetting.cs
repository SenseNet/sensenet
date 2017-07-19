//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using sn = SenseNet.ContentRepository.Schema;

//namespace SenseNet.Services.ContentStore
//{
//    public class FieldSetting
//    {
//        private sn.FieldSetting sourceData;

//        public string DisplayName
//        {
//            get { return sourceData.DisplayName; }
//        }

//        public string FullName
//        {
//            get { return sourceData.FullName; }
//        }

//        public string BindingName
//        {
//            get { return sourceData.BindingName; }
//        }

//        public string TypeName
//        {
//            get { return sourceData.ShortName; }
//        }

//        public string OwnerName
//        {
//            get { return sourceData.Owner.Name; }
//        }

//        public string OwnerTitle
//        {
//            get { return sourceData.Owner.DisplayName; }
//        }

//        public FieldSetting(sn.FieldSetting value)
//        {
//            sourceData = value;
//        }
//    }
//}
