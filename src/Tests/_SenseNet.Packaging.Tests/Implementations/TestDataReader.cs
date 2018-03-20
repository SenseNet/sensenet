using System;
using System.Collections;
using System.Data.Common;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace SenseNet.Packaging.Tests.Implementations
{
    public class TestDataReader : DbDataReader
    {
        private readonly string[] _columnNames;
        private readonly object[][] _records;
        private int _currentRecord = -1;

        public TestDataReader() { }

        public TestDataReader(string[] columnNames, object[][] records)
        {
            _columnNames = columnNames;
            _records = records;
        }

        public override string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            return DBNull.Value == _records[_currentRecord][ordinal];
        }

        public override int FieldCount { get; }

        public override object this[int ordinal] => throw new NotImplementedException();

        public override object this[string name] => throw new NotImplementedException();

        public override bool HasRows { get; }
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            if (_records == null)
                return false;
            if (_currentRecord + 1 >= _records.Length)
                return false;
            _currentRecord++;
            return true;
        }

        public override int Depth { get; }

        public override int GetOrdinal(string name)
        {
            for (int i = 0; i < _columnNames.Length; i++)
                if (0 == string.Compare(name, _columnNames[i], StringComparison.InvariantCultureIgnoreCase))
                    return i;
            return -1;
        }

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            return Convert.ToInt32(_records[_currentRecord][ordinal]);
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return Convert.ToDateTime(_records[_currentRecord][ordinal]);
        }

        public override string GetString(int ordinal)
        {
            return (string)_records[_currentRecord][ordinal];
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
