using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Scripting
{
    public interface IEvaluator
    {
        string Evaluate(string source);
    }
}