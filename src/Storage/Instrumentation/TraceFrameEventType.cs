using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Services.Instrumentation
{
    internal enum TraceFrameEventType
    {
        UNSPECIFIED,
        Started,
        Finished,
        WrongDisposeOrder
    }

}
