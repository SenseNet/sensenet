using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    [Obsolete("##", true)]
    internal class PropertyMap
    {
        private List<PropertyType> _binarySlots = new List<PropertyType>();
        private List<PropertyType> _referenceSlots = new List<PropertyType>();
        private List<PropertyType> _textSlots = new List<PropertyType>();

        public PropertyType[] BinarySlots
        {
            get { return _binarySlots.ToArray(); }
        }
        public PropertyType[] ReferenceSlots
        {
            get { return _referenceSlots.ToArray(); }
        }
        public PropertyType[] TextSlots
        {
            get { return _textSlots.ToArray(); }
        }

        internal PropertyMap(Dictionary<int, NodeBuilder> buildersByVersionId)
        {
            List<NodeType> nodeTypes = new List<NodeType>();
            List<ContentListType> listTypes = new List<ContentListType>();

            foreach(NodeBuilder builder in buildersByVersionId.Values)
            {
                NodeToken token = builder.Token;

                // Map properties from each NodeType (once)
                if(!nodeTypes.Contains(token.NodeType))
                {
                    nodeTypes.Add(token.NodeType);
                    foreach(PropertyType pt in token.NodeType.PropertyTypes)
                        AddPropertyMapping(pt);
                }

                // Map properties from each ContentListType, if exists (once)
                if (token.ContentListType != null && !listTypes.Contains(token.ContentListType))
                {
                    listTypes.Add(token.ContentListType);
                    foreach (PropertyType pt in token.ContentListType.PropertyTypes)
                        AddPropertyMapping(pt);
                }

            }
        }

		private void AddPropertyMapping(PropertyType pt)
		{
			switch (pt.DataType)
			{
				case DataType.String:
				case DataType.Int:
				case DataType.DateTime:
				case DataType.Currency:
					break;
				case DataType.Reference:
					if (!_referenceSlots.Contains(pt))
						_referenceSlots.Add(pt);
					break;
				case DataType.Binary:
					if (!_binarySlots.Contains(pt))
						_binarySlots.Add(pt);
					break;
				case DataType.Text:
					if (!_textSlots.Contains(pt))
						_textSlots.Add(pt);
					break;
				default:
					throw new NotSupportedException(pt.DataType.ToString());
			}
		}

        // Returns a valid mapping if "pt" appears in the given page context.
        public static string GetValidMapping(int queryedPage, PropertyType pt)
        {
            string physicalName;
            int pageSize;
            switch(pt.DataType)
            {
                case DataType.String:
                    physicalName = SqlProvider.StringMappingPrefix;
                    pageSize = SqlProvider.StringPageSize;
                    break;
                case DataType.Int:
                    physicalName = SqlProvider.IntMappingPrefix;
                    pageSize = SqlProvider.IntPageSize;
                    break;
                case DataType.DateTime:
                    physicalName = SqlProvider.DateTimeMappingPrefix;
                    pageSize = SqlProvider.DateTimePageSize;
                    break;
                case DataType.Currency:
                    physicalName = SqlProvider.CurrencyMappingPrefix;
                    pageSize = SqlProvider.CurrencyPageSize;
                    break;
                default:
                    return string.Empty;
            }

            int mappingIndex = pt.Mapping;
            int page = mappingIndex / pageSize;
            if(page != queryedPage)
                return string.Empty;

            int index = mappingIndex - (page * pageSize);
			return string.Concat(physicalName, index + 1);
		}


    }
}