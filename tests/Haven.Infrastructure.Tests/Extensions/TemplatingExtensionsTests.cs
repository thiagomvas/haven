using Haven.Application.Common.Templating;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Extensions;

using Shouldly;

namespace Haven.Infrastructure.Tests.Extensions;

[TestFixture]
[Category("Unit")]
public sealed class TemplatingExtensionsTests
{
    private static Domain.Aggregates.Service CreateService() =>
        Domain.Aggregates.Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None, "app");

    private static Dictionary<string, IReadOnlyDictionary<string, TemplateNamespaceResolver>> Siblings() => new()
    {
        ["db"] = new Dictionary<string, TemplateNamespaceResolver>
        {
            [TemplateNamespaces.Outputs] = k => k == "connection_string" ? "postgres://db" : null,
            [TemplateNamespaces.EnvVariables] = k => k == "A.B" ? "dotted" : null,
        }
    };

    private static IReadOnlyDictionary<string, TemplateNamespaceResolver> Resolve(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, TemplateNamespaceResolver>>? siblings) =>
        CreateService().ResolveTemplateNamespaces([], [], null, null, siblings);

    [Test]
    public void ResolveTemplateNamespaces_ShouldAlwaysIncludeServicesNamespace()
    {
        Resolve(null).ShouldContainKey(TemplateNamespaces.Services);
    }

    [Test]
    public void ServicesNamespace_WhenNoSiblingResolvers_ShouldResolveToNull()
    {
        Resolve(null)[TemplateNamespaces.Services]("db.outputs.connection_string").ShouldBeNull();
    }

    [Test]
    public void ServicesNamespace_ShouldResolveAliasScopeKey()
    {
        Resolve(Siblings())[TemplateNamespaces.Services]("db.outputs.connection_string").ShouldBe("postgres://db");
    }

    [Test]
    public void ServicesNamespace_ShouldPassRemainderIncludingDotsAsKey()
    {
        Resolve(Siblings())[TemplateNamespaces.Services]("db.env.A.B").ShouldBe("dotted");
    }

    [TestCase("")]
    [TestCase("db")]
    [TestCase("db.")]
    [TestCase("db.outputs")]
    [TestCase("db.outputs.")]
    [TestCase(".outputs.connection_string")]
    [TestCase("db..connection_string")]
    public void ServicesNamespace_WhenKeyMalformed_ShouldResolveToNull(string key)
    {
        Resolve(Siblings())[TemplateNamespaces.Services](key).ShouldBeNull();
    }

    [TestCase("nope.outputs.connection_string")]
    [TestCase("db.nope.connection_string")]
    [TestCase("db.outputs.nope")]
    public void ServicesNamespace_WhenAliasScopeOrKeyUnknown_ShouldResolveToNull(string key)
    {
        Resolve(Siblings())[TemplateNamespaces.Services](key).ShouldBeNull();
    }

    [Test]
    public void ServicesNamespace_ShouldWorkThroughTemplateExpressionResolver()
    {
        var result = TemplateExpressionResolver.Resolve(
            "conn=${{ services.db.outputs.connection_string | urlencode }} missing=${{ services.db.outputs.nope }}",
            Resolve(Siblings()));

        result.ShouldBe("conn=postgres%3A%2F%2Fdb missing=${{ services.db.outputs.nope }}");
    }
}
