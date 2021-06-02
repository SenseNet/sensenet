using System;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{

    public interface IStatisticalDataProvider
    {
        Task WriteData(IStatisticalDataRecord data);
    }

}
