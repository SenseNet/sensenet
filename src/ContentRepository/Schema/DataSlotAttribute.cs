using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;

namespace  SenseNet.ContentRepository.Schema
{
	[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class DataSlotAttribute : Attribute
	{
		private int _slotIndex;
		private RepositoryDataType _dataType;
		private Type[] _acceptedTypes;

		public int SlotIndex
		{
			get { return _slotIndex; }
			set { _slotIndex = value; }
		}
		public RepositoryDataType DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}
		public Type[] AcceptedTypes
		{
			get { return _acceptedTypes; }
			set { _acceptedTypes = value; }
		}

		public DataSlotAttribute(int slotIndex, RepositoryDataType dataType, params Type[] acceptedTypes)
		{
			_slotIndex = slotIndex;
			_dataType = dataType;
			_acceptedTypes = acceptedTypes;
		}
	}
}