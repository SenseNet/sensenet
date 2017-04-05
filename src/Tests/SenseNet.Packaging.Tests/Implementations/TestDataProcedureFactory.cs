using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Packaging.Tests.Implementations
{
    public class TestDataProcedureFactory : IDataProcedureFactory
    {
        public List<TestDataProcedure> Procedures { get; }
        public object ExpectedCommandResult { get; set; }

        public TestDataProcedureFactory(List<TestDataProcedure> procedures)
        {
            Procedures = procedures;
        }

        public IDataProcedure CreateProcedure()
        {
            var proc = new TestDataProcedure();
            proc.TraceList = Procedures;
            proc.ExpectedCommandResult = ExpectedCommandResult;
            return proc;
        }
    }
}
