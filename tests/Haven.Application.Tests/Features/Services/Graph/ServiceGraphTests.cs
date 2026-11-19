using Haven.Application.Features.Services.Graph;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Shouldly;
using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Tests.Features.Services.Graph;

[TestFixture]
public class ServiceGraphTests
{
    private Environment _environment = null!;

    [SetUp]
    public void SetUp()
    {
        _environment = Environment.Create(Guid.NewGuid(), "dev");
    }

    private Service Add(string name, string? alias) =>
        _environment.AddService(name, ServiceType.DockerImage, ExposureMode.None, alias);

    private static string Ref(string key) => "${{ services." + key + " }}";

    private static Dictionary<Guid, IReadOnlyCollection<string?>> Values(params (Service Service, string?[] Values)[] entries) =>
        entries.ToDictionary(e => e.Service.Id, e => (IReadOnlyCollection<string?>)e.Values);

    private ServiceGraph Build(Dictionary<Guid, IReadOnlyCollection<string?>>? values = null) =>
        ServiceGraph.FromEnvironment(_environment, values ?? new());

    private static ServiceNode NodeOf(ServiceGraph graph, Service service) =>
        graph.Nodes.Single(n => n.Service.Id == service.Id);

    [Test]
    public void EmptyEnvironment_HasNoNodes()
    {
        Build().Nodes.ShouldBeEmpty();
    }

    [Test]
    public void CreatesOneNodePerService_WithNoDependencies_WhenNoValues()
    {
        var a = Add("a", "a");
        var b = Add("b", "b");

        var graph = Build();

        graph.Nodes.Count.ShouldBe(2);
        graph.Nodes.Select(n => n.Name).ShouldBe([a.Name, b.Name], ignoreOrder: true);
        graph.Nodes.ShouldAllBe(n => n.Dependencies.Count == 0);
    }

    [Test]
    public void ReferenceInValue_CreatesDependency()
    {
        var app = Add("app", "app");
        var db = Add("db", "db");

        var graph = Build(Values((app, [Ref("db.outputs.url")])));

        NodeOf(graph, app).Dependencies.ShouldHaveSingleItem().ShouldBeSameAs(NodeOf(graph, db));
        NodeOf(graph, db).Dependencies.ShouldBeEmpty();
    }

    [Test]
    public void ReferenceEmbeddedInLargerValue_CreatesDependency()
    {
        var app = Add("app", "app");
        var db = Add("db", "db");

        var graph = Build(Values((app, ["postgres://user:${{ services.db.env.PASSWORD | urlencode }}@host/db"])));

        NodeOf(graph, app).Dependencies.ShouldHaveSingleItem().ShouldBeSameAs(NodeOf(graph, db));
    }

    [Test]
    public void MultipleReferencesToSameService_AreDeduplicated()
    {
        var app = Add("app", "app");
        Add("db", "db");

        var graph = Build(Values((app, [Ref("db.outputs.url"), Ref("db.env.PASSWORD"), Ref("db.outputs.url")])));

        NodeOf(graph, app).Dependencies.Count.ShouldBe(1);
    }

    [Test]
    public void ReferencesToDifferentServices_CreateAllDependencies()
    {
        var app = Add("app", "app");
        var db = Add("db", "db");
        var cache = Add("cache", "cache");

        var graph = Build(Values((app, [Ref("db.outputs.url"), Ref("cache.env.HOST")])));

        NodeOf(graph, app).Dependencies.ShouldBe([NodeOf(graph, db), NodeOf(graph, cache)], ignoreOrder: true);
    }

    [Test]
    public void DependenciesAreDirected()
    {
        var app = Add("app", "app");
        var db = Add("db", "db");

        var graph = Build(Values((app, [Ref("db.outputs.url")]), (db, ["PLAIN=value"])));

        NodeOf(graph, app).Dependencies.Count.ShouldBe(1);
        NodeOf(graph, db).Dependencies.ShouldBeEmpty();
    }

    [Test]
    public void ChainedDependencies_AreLinkedThroughNodes()
    {
        var web = Add("web", "web");
        var api = Add("api", "api");
        var db = Add("db", "db");

        var graph = Build(Values((web, [Ref("api.env.URL")]), (api, [Ref("db.env.URL")])));

        var apiNode = NodeOf(graph, web).Dependencies.Single();
        apiNode.ShouldBeSameAs(NodeOf(graph, api));
        apiNode.Dependencies.Single().ShouldBeSameAs(NodeOf(graph, db));
    }

    [Test]
    public void SelfReference_IsIgnored()
    {
        var app = Add("app", "app");

        var graph = Build(Values((app, [Ref("app.env.X")])));

        NodeOf(graph, app).Dependencies.ShouldBeEmpty();
    }

    [Test]
    public void UnknownAlias_IsIgnored()
    {
        var app = Add("app", "app");
        Add("db", "db");

        var graph = Build(Values((app, [Ref("missing.env.X")])));

        NodeOf(graph, app).Dependencies.ShouldBeEmpty();
    }

    [Test]
    public void NonServicesNamespaces_AreNotDependencies()
    {
        var app = Add("app", "app");
        Add("db", "db");

        var graph = Build(Values((app, ["${{ env.db }}", "${{ secrets.db }}", "${{ runtime.db }}", "db.outputs.url"])));

        NodeOf(graph, app).Dependencies.ShouldBeEmpty();
    }

    [Test]
    public void ServiceWithoutAlias_CannotBeReferenced_AndCanStillHaveDependencies()
    {
        var app = Add("app", null);
        var db = Add("db", "db");
        Add("other", null);

        var graph = Build(Values((app, [Ref("db.env.X"), Ref("other.env.X")])));

        NodeOf(graph, app).Dependencies.ShouldHaveSingleItem().ShouldBeSameAs(NodeOf(graph, db));
    }

    [Test]
    public void NullAndEmptyValues_AreSkipped()
    {
        var app = Add("app", "app");
        Add("db", "db");

        var graph = Build(Values((app, [null, "", Ref("db.env.X")])));

        NodeOf(graph, app).Dependencies.Count.ShouldBe(1);
    }

    [Test]
    public void CircularReferences_AreRepresentedWithoutLooping()
    {
        var a = Add("a", "a");
        var b = Add("b", "b");

        var graph = Build(Values((a, [Ref("b.env.X")]), (b, [Ref("a.env.X")])));

        NodeOf(graph, a).Dependencies.Single().ShouldBeSameAs(NodeOf(graph, b));
        NodeOf(graph, b).Dependencies.Single().ShouldBeSameAs(NodeOf(graph, a));
    }

    [Test]
    public void ValuesForServicesOutsideEnvironment_AreIgnored()
    {
        Add("app", "app");
        var values = new Dictionary<Guid, IReadOnlyCollection<string?>>
        {
            [Guid.NewGuid()] = [Ref("app.env.X")]
        };

        Should.NotThrow(() => Build(values).Nodes.ShouldAllBe(n => n.Dependencies.Count == 0));
    }

    [Test]
    public void GetDependants_ReturnsServicesReferencingTarget()
    {
        var db = Add("db", "db");
        var api = Add("api", "api");
        var worker = Add("worker", "worker");
        var other = Add("other", "other");

        var graph = Build(Values(
            (api, [Ref("db.outputs.conn")]),
            (worker, [Ref("db.outputs.conn"), Ref("api.outputs.url")]),
            (other, ["plain"])));

        graph.GetDependants(db.Id).Select(n => n.Service.Id).ShouldBe([api.Id, worker.Id], ignoreOrder: true);
        graph.GetDependants(api.Id).Select(n => n.Service.Id).ShouldBe([worker.Id]);
        graph.GetDependants(other.Id).ShouldBeEmpty();
    }
}
