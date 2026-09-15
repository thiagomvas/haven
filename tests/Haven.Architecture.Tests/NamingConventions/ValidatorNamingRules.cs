using FluentValidation;

using NetArchTest.Rules;

namespace Haven.Architecture.Tests.NamingConventions;

[TestFixture]
[Category("Architecture")]
public class ValidatorNamingRules
{
    [Test]
    public void Validators_ShouldEndWithValidator()
    {
        var result = Types.InAssemblies([Assemblies.Application])
            .That()
            .ImplementInterface(typeof(IValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Validator'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }

    [Test]
    public void ValidatorsForCommandsOrQueries_ShouldStartWithTheirRequestNames()
    {
        NamingConventionAssertions.ValidatorsForCommandsOrQueriesShouldStartWithTheirRequestNames(Assemblies.Application);
    }
}