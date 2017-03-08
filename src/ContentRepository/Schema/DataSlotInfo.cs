using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;

namespace  SenseNet.ContentRepository.Schema
{
	[System.Diagnostics.DebuggerDisplay("Slot={DataType}, AcceptedTypes={DumpAcceptedTypes()}")]
	internal class DataSlotInfo : IComparable<DataSlotInfo>, IComparable
	{
		public int SlotIndex { get; private set; }
		public RepositoryDataType DataType { get; private set; }
		public Type[] AcceptedTypes { get; private set; }

		public DataSlotInfo(int slotIndex, RepositoryDataType dataType, Type[] acceptedTypes)
		{
			SlotIndex = slotIndex;
			DataType = dataType;
			AcceptedTypes = acceptedTypes;
		}

		public int CompareTo(DataSlotInfo other)
		{
			return SlotIndex.CompareTo(other.SlotIndex);
		}
		public int CompareTo(object obj)
		{
			return CompareTo((DataSlotInfo)obj);
		}

		private string DumpAcceptedTypes()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Type t in AcceptedTypes)
				sb.Append(sb.Length > 0 ? ", " : "").Append(t.Name);
			return sb.ToString();
		}
	}
}