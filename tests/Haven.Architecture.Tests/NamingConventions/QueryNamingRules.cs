using Haven.Application.Common.Messaging;

using NetArchTest.Rules;

namespace Haven.Architecture.Tests.NamingConventions;

[TestFixture]
[Category("Architecture")]
public class QueryNamingRules
{
    [Test]
    public void Queries_ShouldEndWithQuery()
    {
        var result = Types.InAssemblies([Assemblies.Application])
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Query'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }
    
    [Test]
    public void QueryHandlers_ShouldEndWithHandler()
    {
        var result = Types.InAssemblies([Assemblies.Application])
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Handler'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }

    [Test]
    public void QueryHandlers_ShouldStartWithTheirQueryNames()
    {
        HandlerNamingAssertions.HandlersShouldStartWithTheirRequestNames(
            Assemblies.Application,
            [typeof(IQueryHandler<,>)],
            "Query"
        );
    }
}