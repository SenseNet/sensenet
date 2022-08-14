namespace SenseNet.OpenApiTests.TestTypes1 { internal enum TestEnum { Item1, Item2 } }
namespace SenseNet.OpenApiTests.TestTypes2 { internal enum TestEnum { ItemA, ItemB, ItemC } }

namespace SenseNet.OpenApiTests.TestTypes3
{
    internal abstract class BaseClass             { public string Property1 { get; set; } }
    internal          class Class1    : BaseClass { public string Property2 { get; set; } }
    internal          class Class2    : Class1    { public string Property3 { get; set; } }
}
