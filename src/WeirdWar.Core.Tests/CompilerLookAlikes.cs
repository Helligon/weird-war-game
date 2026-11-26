// Look-alike types for the TEC-013 guard tests: they must NOT be treated as compiler-generated.

namespace WeirdWar.Core.Tests.Fakes
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class EmbeddedAttribute : Attribute { }
}

namespace Microsoft.CodeAnalysis
{
    [WeirdWar.Core.Tests.Fakes.Embedded]
    internal sealed class FakeMarkedType
    {
        internal sealed class Nested { }
    }

    internal sealed class UnmarkedType { }
}
