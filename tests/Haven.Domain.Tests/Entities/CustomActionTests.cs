using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[Category("Unit")]
public sealed class CustomActionTests
{
    private static CustomAction Create(Guid? serviceId = null) =>
        CustomAction.Create(serviceId ?? Guid.NewGuid(), "Flush", "flush", "Flushes cache", "trash",
            new ExecActionConfig(["redis-cli", "flushall"], null, null, null), ["services.execute"],
            ActionRisk.RequireConfirmation, TimeSpan.FromSeconds(30));

    [Test]
    public void Create_SetsAllProperties()
    {
        var serviceId = Guid.NewGuid();

        var action = Create(serviceId);

        action.ServiceId.ShouldBe(serviceId);
        action.ActionName.ShouldBe("Flush");
        action.Alias.ShouldBe("flush");
        action.ActionDescription.ShouldBe("Flushes cache");
        action.Icon.ShouldBe("trash");
        action.Config.ShouldBeOfType<ExecActionConfig>();
        action.RequiredPermissions.ShouldBe(["services.execute"]);
        action.Risk.ShouldBe(ActionRisk.RequireConfirmation);
        action.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void Create_GeneratesPrefixedToken()
    {
        Create().Token.ShouldStartWith("hca_");
    }

    [Test]
    public void Create_GeneratesDistinctTokensPerAction()
    {
        Create().Token.ShouldNotBe(Create().Token);
    }

    [Test]
    public void RegenerateToken_ReplacesToken()
    {
        var action = Create();
        var original = action.Token;

        action.RegenerateToken();

        action.Token.ShouldNotBe(original);
        action.Token.ShouldStartWith("hca_");
    }
}