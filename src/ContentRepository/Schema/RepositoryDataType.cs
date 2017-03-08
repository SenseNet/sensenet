using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

namespace  SenseNet.ContentRepository.Schema
{
	public enum RepositoryDataType
    {
		NotDefined = 0,
        String = DataType.String,
		Text = DataType.Text,
		Int = DataType.Int,
		Currency = DataType.Currency,
		DateTime = DataType.DateTime,
		Binary = DataType.Binary,
		Reference = DataType.Reference
    }
}