using System;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class DataProviderExtensions
    {
        public static IDataProcedure AddParameter(this IDataProcedure proc, string name, object value,
            DbType? type = null, int size = 0)
        {
            var param = (IDbDataParameter)proc.CreateParameter();
            param.ParameterName = name;
            param.DbType = DeriveParameterType(name, value, type, out var prmSize);
            param.Size = size > 0 ? size : prmSize;
            param.Value = value;
            proc.Parameters.Add(param);

            return proc;
        }
        private static DbType DeriveParameterType(string paramName, object value, DbType? type, out int size)
        {
            size = 0;
            if (type.HasValue)
                return type.Value;

            if (value is string stringValue)
            {
                size = stringValue.Length;
                return DbType.String;
            }
            if (value is bool)
                return DbType.Boolean;
            if (value is byte)
                return DbType.Byte;
            if (value is short)
                return DbType.Int16;
            if (value is int)
                return DbType.Int32;
            if (value is long)
                return DbType.Int64;
            if (value is DateTime)
                return DbType.DateTime;
            if (value is float)
                return DbType.Single;
            if (value is double)
                return DbType.Double;
            if (value is decimal)
                return DbType.Decimal;
            if (value is Guid)
                return DbType.Guid;
            if (value is byte[] binaryValue)
            {
                size = binaryValue.Length;
                return DbType.Binary;
            }

            throw new ArgumentException("Cannot derive DbType from the value'. Specify the " +
                                        $"'type' parameter for the parameter '{paramName}.");
        }

    }
}
