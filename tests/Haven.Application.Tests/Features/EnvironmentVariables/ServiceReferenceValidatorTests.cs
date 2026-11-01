using Haven.Application.Features.EnvironmentVariables;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Shouldly;

namespace Haven.Application.Tests.Features.EnvironmentVariables;

[TestFixture]
public class ServiceReferenceValidatorTests
{
    private static Service Make(string name, string alias) =>
        Service.Create(Guid.NewGuid(), name, ServiceType.DockerImage, ExposureMode.None, alias: alias);

    private static string Ref(string key) => "X=${{ services." + key + " }}";

    [Test]
    public void NoReferences_ReturnsNull()
    {
        var a = Make("a", "a");
        ServiceReferenceValidator.Validate(a, "FOO=bar", [a], new Dictionary<Guid, string>()).ShouldBeNull();
    }

    [Test]
    public void UnknownService_ReturnsError()
    {
        var a = Make("a", "a");
        var error = ServiceReferenceValidator.Validate(a, Ref("db.outputs.url"), [a], new Dictionary<Guid, string>());
        error.ShouldNotBeNull();
        error.Message.ShouldContain("does not exist");
    }

    [Test]
    public void UnknownOutput_ReturnsError()
    {
        var a = Make("a", "a");
        var db = Make("db", "db");
        var error = ServiceReferenceValidator.Validate(a, Ref("db.outputs.url"), [a, db],
            new Dictionary<Guid, string> { [db.Id] = "" });
        error.ShouldNotBeNull();
        error.Message.ShouldContain("not defined");
    }

    [Test]
    public void ExistingOutput_ReturnsNull()
    {
        var a = Make("a", "a");
        var db = Make("db", "db");
        db.AddComputedProperty("url", "Url", "x", false);
        ServiceReferenceValidator.Validate(a, Ref("db.outputs.url"), [a, db],
            new Dictionary<Guid, string> { [db.Id] = "" }).ShouldBeNull();
    }

    [Test]
    public void SelfReference_ReturnsError()
    {
        var a = Make("a", "a");
        ServiceReferenceValidator.Validate(a, Ref("a.env.X"), [a], new Dictionary<Guid, string>())
            .ShouldNotBeNull();
    }

    [Test]
    public void IndirectCycle_ReturnsError()
    {
        var a = Make("a", "a");
        var b = Make("b", "b");
        var c = Make("c", "c");
        var contents = new Dictionary<Guid, string>
        {
            [b.Id] = Ref("c.env.X"),
            [c.Id] = Ref("a.env.X")
        };
        var error = ServiceReferenceValidator.Validate(a, Ref("b.env.X"), [a, b, c], contents);
        error.ShouldNotBeNull();
        error.Message.ShouldContain("a -> b -> c -> a");
    }

    [Test]
    public void ChainWithoutCycle_ReturnsNull()
    {
        var a = Make("a", "a");
        var b = Make("b", "b");
        var c = Make("c", "c");
        var contents = new Dictionary<Guid, string> { [b.Id] = Ref("c.env.X"), [c.Id] = "" };
        ServiceReferenceValidator.Validate(a, Ref("b.env.X"), [a, b, c], contents).ShouldBeNull();
    }
}
