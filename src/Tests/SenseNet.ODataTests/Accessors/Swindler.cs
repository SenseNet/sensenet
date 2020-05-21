using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ODataTests.Accessors
{
    public class Swindler<T> : IDisposable
    {
        private readonly T _original;
        private readonly Action<T> _setter;
        public Swindler(T hack, Func<T> getter, Action<T> setter)
        {
            _original = getter();
            _setter = setter;
            setter(hack);
        }

        public void Dispose()
        {
            _setter(_original);
        }
    }
}
