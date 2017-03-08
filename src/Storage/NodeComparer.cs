using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    public class NodeComparer<T> : IComparer<T> where T : Node
    {
        public int Compare(T x, T y)
        {
            return x.Index.CompareTo(y.Index);
        }
    }
}