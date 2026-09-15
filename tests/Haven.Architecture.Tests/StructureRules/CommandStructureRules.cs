using Haven.Application.Common.Messaging;

namespace Haven.Architecture.Tests.StructureRules;

[TestFixture]
[Category("Architecture")]
public class CommandStructureRules
{
    [Test]
    public void Commands_ShouldBeSealed()
    {
        SealedTypeAssertions.AssertAllSealed(Assemblies.Application, typeof(ICommand));
    }

    [Test]
    public void CommandsWithResponse_ShouldBeSealed()
    {
        SealedTypeAssertions.AssertAllSealed(Assemblies.Application, typeof(ICommand<>));
    }

    [Test]
    public void CommandHandlers_ShouldBeSealed()
    {
        SealedTypeAssertions.AssertAllSealed(Assemblies.Application, typeof(ICommandHandler<>));
        SealedTypeAssertions.AssertAllSealed(Assemblies.Application, typeof(ICommandHandler<,>));
    }
}
