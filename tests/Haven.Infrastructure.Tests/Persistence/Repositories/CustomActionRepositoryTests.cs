using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Testing.Common;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Repositories;

[Category("Unit")]
public sealed class CustomActionRepositoryTests
{
    private HavenDbContext _context = null!;
    private CustomActionRepository _sut = null!;

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateUnitDbContext();
        _sut = new CustomActionRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private async Task<Service> AddServiceAsync(string name)
    {
        var project = Project.Create($"project-{name}");
        var environment = project.AddEnvironment("production", "prod");
        var service = environment.AddService(name, ServiceType.DockerImage, ExposureMode.None);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return service;
    }

    private async Task<CustomAction> AddActionAsync(Guid serviceId, string name, ActionConfig? config = null)
    {
        var action = CustomAction.Create(serviceId, name, name.ToLowerInvariant(), "desc", "icon",
            config ?? new ExecActionConfig(["ls"], null, null, null), ["perm"], ActionRisk.Safe,
            TimeSpan.FromSeconds(10));
        _context.CustomActions.Add(action);
        await _context.SaveChangesAsync();
        return action;
    }

    [Test]
    public async Task GetByIdAsync_ExistingAction_ReturnsIt()
    {
        var service = await AddServiceAsync("api");
        var action = await AddActionAsync(service.Id, "Restart");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(action.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ActionName.ShouldBe("Restart");
        result.Token.ShouldBe(action.Token);
        result.RequiredPermissions.ShouldBe(["perm"]);
        result.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetByIdAsync_ExecConfig_RoundTripsThroughJson()
    {
        var service = await AddServiceAsync("api");
        var config = new ExecActionConfig(["echo", "hi"], "/app", "root", ShellType.Bash);
        var action = await AddActionAsync(service.Id, "Exec", config);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(action.Id, CancellationToken.None);

        var loaded = result!.Config.ShouldBeOfType<ExecActionConfig>();
        loaded.Command.ShouldBe(["echo", "hi"]);
        loaded.WorkingDir.ShouldBe("/app");
        loaded.User.ShouldBe("root");
        loaded.Shell.ShouldBe(ShellType.Bash);
    }

    [Test]
    public async Task GetByIdAsync_HttpConfig_RoundTripsThroughJson()
    {
        var service = await AddServiceAsync("api");
        var config = new HttpActionConfig(HttpMethod.Post, "https://example.com/hook",
            new Dictionary<string, string> { ["X-Key"] = "v" }, "{}", [200, 204]);
        var action = await AddActionAsync(service.Id, "Http", config);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(action.Id, CancellationToken.None);

        var loaded = result!.Config.ShouldBeOfType<HttpActionConfig>();
        loaded.Method.ShouldBe(HttpMethod.Post);
        loaded.Url.ShouldBe("https://example.com/hook");
        loaded.Headers["X-Key"].ShouldBe("v");
        loaded.Body.ShouldBe("{}");
        loaded.SuccessStatusCodes.ShouldBe([200, 204]);
    }

    [Test]
    public async Task GetForServiceAsync_ReturnsOnlyThatServicesActionsOrderedByName()
    {
        var service = await AddServiceAsync("api");
        var other = await AddServiceAsync("worker");
        await AddActionAsync(service.Id, "Zeta");
        await AddActionAsync(service.Id, "Alpha");
        await AddActionAsync(other.Id, "Other");

        var result = await _sut.GetForServiceAsync(service.Id, CancellationToken.None);

        result.Select(a => a.ActionName).ShouldBe(["Alpha", "Zeta"]);
    }

    [Test]
    public async Task GetForServiceAsync_NoActions_ReturnsEmpty()
    {
        var service = await AddServiceAsync("api");

        var result = await _sut.GetForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
