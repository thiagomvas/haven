using Haven.Application.Common.Messaging;

namespace Haven.Architecture.Tests.StructureRules;

[TestFixture]
[Category("Architecture")]
public class QueryStructureRules
{
    [Test]
    public void Queries_ShouldBeSealed()
    {
        SealedTypeAssertions.AssertAllSealed(Assemblies.Application, typeof(IQuery<>));
    }

    [Test]
    public void QueryHandlers_ShouldBeSealed()
    {
        SealedTypeAssertions.AssertAllSealed(Assemblies.Application, typeof(IQueryHandler<,>));
    }
}