using NetArchTest.Rules;

using Riok.Mapperly.Abstractions;

namespace Haven.Architecture.Tests.NamingConventions;

[TestFixture]
[Category("Architecture")]
public class MapperNamingRules
{
    [Test]
    public void Mappers_ShouldEndWithMapper()
    {
        var result = Types.InAssemblies([Assemblies.Application])
            .That()
            .HaveCustomAttribute(typeof(MapperAttribute))
            .Should()
            .HaveNameEndingWith("Mapper")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Mapper'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }

}