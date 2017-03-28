using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace SenseNet.Core.Tests.Implementations
{
    public class TestDbParameterCollection : DbParameterCollection
    {
        private List<DbParameter> _parameters = new List<DbParameter>();

        public override int Add(object value)
        {
            var p = value as DbParameter;
            _parameters.Add(p);
            return _parameters.Count - 1;
        }

        public override bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public override void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            throw new NotImplementedException();
        }

        public override int Count => _parameters.Count;
        public override object SyncRoot { get { return ((ICollection)_parameters).SyncRoot; } }

        public override int IndexOf(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override DbParameter GetParameter(int index)
        {
            return _parameters[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(string value)
        {
            throw new NotImplementedException();
        }

        public override void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public override void AddRange(Array values)
        {
            throw new NotImplementedException();
        }
    }
}
