using Haven.Application.Common.Interfaces.Repositories;

using NetArchTest.Rules;

namespace Haven.Architecture.Tests.NamingConventions;

[TestFixture]
[Category("Architecture")]
public class RepositoryNamingRules
{
    [Test]
    public void Repositories_ShouldEndWithRepository()
    {
        var result = Types.InAssemblies([Assemblies.Application, Assemblies.Infrastructure])
            .That()
            .ImplementInterface(typeof(IRepository))
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Repository'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }
}