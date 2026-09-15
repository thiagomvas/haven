using Haven.Application.Common.Messaging;

using NetArchTest.Rules;

namespace Haven.Architecture.Tests.NamingConventions;

[TestFixture]
[Category("Architecture")]
public class CommandNamingRules
{
    [Test]
    public void Commands_ShouldEndWithCommand()
    {
        var result = Types.InAssemblies([Assemblies.Application])
            .That()
            .ImplementInterface(typeof(ICommand))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Command'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }
    
    [Test]
    public void CommandHandlers_ShouldEndWithHandler()
    {
        var result = Types.InAssemblies([Assemblies.Application])
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following classes do not follow the naming convention (should end with 'Handler'): {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }

    [Test]
    public void CommandHandlers_ShouldStartWithTheirCommandNames()
    {
        HandlerNamingAssertions.HandlersShouldStartWithTheirRequestNames(
            Assemblies.Application,
            [typeof(ICommandHandler<>), typeof(ICommandHandler<,>)],
            "Command"
        );
    }
}