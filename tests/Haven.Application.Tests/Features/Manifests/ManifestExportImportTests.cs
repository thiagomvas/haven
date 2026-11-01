using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments.Commands.ImportEnvironment;
using Haven.Application.Features.Environments.Queries.ExportEnvironmentManifest;
using Haven.Application.Features.Manifests;
using Haven.Application.Features.Projects.Commands.ImportProject;
using Haven.Application.Features.Projects.Queries.ExportProjectManifest;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Tests.Features.Manifests;

/// <summary>
/// Builds a full project graph, exports it through the real export handlers, imports the resulting YAML
/// through the real import handlers and checks the imported graph matches the original. Repositories are
/// substituted, so entities handed to them are captured and inspected directly.
/// </summary>
[Category("Unit")]
public sealed class ManifestExportImportTests
{
    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private INetworkRepository _networkRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IFeatureFlagRepository _featureFlagRepository = null!;

    private List<FeatureFlag> _importedFlags = null!;
    private List<Network> _importedNetworks = null!;

    private ExportProjectManifestHandler _exportProject = null!;
    private ExportEnvironmentManifestHandler _exportEnvironment = null!;
    private ImportProjectHandler _importProject = null!;
    private ImportEnvironmentHandler _importEnvironment = null!;

    private Project _source = null!;

    [SetUp]
    public void SetUp()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _networkRepository = Substitute.For<INetworkRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _featureFlagRepository = Substitute.For<IFeatureFlagRepository>();

        _importedFlags = [];
        _importedNetworks = [];
        _featureFlagRepository
            .AddAsync(Arg.Do<IEnumerable<FeatureFlag>>(flags => _importedFlags.AddRange(flags)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _networkRepository
            .AddAsync(Arg.Do<Network>(n => _importedNetworks.Add(n)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _projectRepository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var importer = new ManifestImporter(_environmentRepository, _networkRepository, _serviceRepository, _featureFlagRepository);
        _exportProject = new ExportProjectManifestHandler(_projectRepository);
        _exportEnvironment = new ExportEnvironmentManifestHandler(_environmentRepository);
        _importProject = new ImportProjectHandler(_projectRepository, importer);
        _importEnvironment = new ImportEnvironmentHandler(_projectRepository, importer);

        _source = BuildSourceGraph();
        _projectRepository.GetByIdAsync(_source.Id, Arg.Any<CancellationToken>()).Returns(_source);
        foreach (var environment in _source.Environments)
            _environmentRepository.GetByIdWithServicesAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
    }

    // ---------------------------------------------------------------- project

    [Test]
    public async Task ProjectFullGraph_ExportThenImport_ProducesTheSameGraph()
    {
        var yaml = await ExportProjectAsync(includeEnvironments: true, includeServices: true);

        var imported = await ImportProjectAsync(yaml, includeEnvironments: true, includeServices: true);

        Snapshot(imported).ShouldBe(Snapshot(_source));
    }

    [Test]
    public async Task ProjectFullGraph_ImportCreatesOneNetworkPerEnvironment()
    {
        var yaml = await ExportProjectAsync(true, true);

        await ImportProjectAsync(yaml, true, true);

        _importedNetworks.Count.ShouldBe(_source.Environments.Count);
    }

    [Test]
    public async Task ProjectWithEnvironmentsOnly_ExportThenImport_KeepsEnvironmentsAndDropsServices()
    {
        var yaml = await ExportProjectAsync(includeEnvironments: true, includeServices: false);

        var imported = await ImportProjectAsync(yaml, includeEnvironments: true, includeServices: false);

        imported.Environments.Select(e => e.Name).ShouldBe(_source.Environments.Select(e => e.Name), ignoreOrder: true);
        imported.Environments.SelectMany(e => e.Services).ShouldBeEmpty();
    }

    [Test]
    public async Task ProjectOnly_ExportThenImport_KeepsProjectFieldsAndNoEnvironments()
    {
        var yaml = await ExportProjectAsync(includeEnvironments: false, includeServices: false);

        var imported = await ImportProjectAsync(yaml, includeEnvironments: false, includeServices: false);

        imported.Name.ShouldBe(_source.Name);
        imported.Alias.ShouldBe(_source.Alias);
        imported.Description.ShouldBe(_source.Description);
        imported.Environments.ShouldBeEmpty();
        _importedNetworks.ShouldBeEmpty();
    }

    [Test]
    public async Task ProjectImport_FromFullExport_CanBeNarrowedAtImportTime()
    {
        var yaml = await ExportProjectAsync(true, true);

        var withoutServices = await ImportProjectAsync(yaml, includeEnvironments: true, includeServices: false);
        var projectOnly = await ImportProjectAsync(yaml, includeEnvironments: false, includeServices: false);

        withoutServices.Environments.Count.ShouldBe(_source.Environments.Count);
        withoutServices.Environments.SelectMany(e => e.Services).ShouldBeEmpty();
        projectOnly.Environments.ShouldBeEmpty();
    }

    [Test]
    public async Task ProjectExport_WithoutEnvironments_OmitsThemFromYaml()
    {
        var yaml = await ExportProjectAsync(includeEnvironments: false, includeServices: false);

        yaml.ShouldContain("name: shop");
        yaml.ShouldNotContain("environments");
        yaml.ShouldNotContain("services");
    }

    [Test]
    public async Task ProjectExport_WithEnvironmentsButNoServices_OmitsServicesFromYaml()
    {
        var yaml = await ExportProjectAsync(includeEnvironments: true, includeServices: false);

        yaml.ShouldContain("environments");
        yaml.ShouldNotContain("services");
    }

    [Test]
    public async Task ProjectImport_GeneratesNewIdsForEverything()
    {
        var yaml = await ExportProjectAsync(true, true);

        var imported = await ImportProjectAsync(yaml, true, true);

        imported.Id.ShouldNotBe(_source.Id);
        AllIds(imported).Intersect(AllIds(_source)).ShouldBeEmpty();
    }

    [Test]
    public async Task ProjectImport_SameManifestTwice_ProducesDistinctGraphs()
    {
        var yaml = await ExportProjectAsync(true, true);

        var first = await ImportProjectAsync(yaml, true, true);
        var second = await ImportProjectAsync(yaml, true, true);

        AllIds(first).Intersect(AllIds(second)).ShouldBeEmpty();
        Snapshot(first).ShouldBe(Snapshot(second));
    }

    [Test]
    public async Task ProjectImport_NameAndAliasOverrides_ReplaceManifestValues()
    {
        var yaml = await ExportProjectAsync(true, true);

        var result = await _importProject.Handle(
            new ImportProjectCommand { Yaml = yaml, Name = "copy", Alias = "cpy", IncludeEnvironments = true },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var imported = CapturedProject();
        imported.Name.ShouldBe("copy");
        imported.Alias.ShouldBe("cpy");
    }

    [Test]
    public async Task ProjectImport_WhenNameAlreadyExists_ReturnsConflictAndAddsNothing()
    {
        var yaml = await ExportProjectAsync(true, true);
        _projectRepository.ExistsWithNameAsync("shop", Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _importProject.Handle(
            new ImportProjectCommand { Yaml = yaml, IncludeEnvironments = true }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("CONFLICT");
        await _projectRepository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProjectImport_WithInvalidYaml_ReturnsValidationError()
    {
        var result = await _importProject.Handle(
            new ImportProjectCommand { Yaml = "name: [unclosed" }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("VALIDATION");
        await _projectRepository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProjectImport_WithDuplicateEnvironmentNames_ReturnsValidationError()
    {
        const string yaml = """
            id: 11111111-1111-1111-1111-111111111111
            name: dup
            environments:
            - id: 22222222-2222-2222-2222-222222222222
              name: dev
              networkName: n1
            - id: 33333333-3333-3333-3333-333333333333
              name: DEV
              networkName: n2
            """;

        var result = await _importProject.Handle(
            new ImportProjectCommand { Yaml = yaml, IncludeEnvironments = true }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("VALIDATION");
    }

    [Test]
    public async Task ProjectExport_ForUnknownProject_ReturnsNotFound()
    {
        var result = await _exportProject.Handle(
            new ExportProjectManifestQuery { ProjectId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public void ProjectImportValidator_RejectsServicesWithoutEnvironments()
    {
        var result = new ImportProjectValidator().Validate(
            new ImportProjectCommand { Yaml = "name: x", IncludeEnvironments = false, IncludeServices = true });

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void ProjectExportValidator_RejectsServicesWithoutEnvironments()
    {
        var result = new ExportProjectManifestValidator().Validate(
            new ExportProjectManifestQuery { ProjectId = Guid.NewGuid(), IncludeEnvironments = false, IncludeServices = true });

        result.IsValid.ShouldBeFalse();
    }

    // ------------------------------------------------------------ environment

    [Test]
    public async Task EnvironmentWithServices_ExportThenImport_ProducesTheSameEnvironment()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: true);
        var target = CreateTargetProject();

        var imported = await ImportEnvironmentAsync(target, yaml, includeServices: true);

        // The network name is derived from the owning project's alias, so it follows the target project.
        imported.NetworkName.ShouldBe("haven-tgt-dev");
        Snapshot(imported).ShouldBe(Snapshot(sourceEnvironment).Replace("haven-shop-dev", "haven-tgt-dev"));
        target.Environments.ShouldContain(imported);
    }

    [Test]
    public async Task EnvironmentWithoutServices_ExportThenImport_KeepsOnlyTheEnvironment()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: false);
        var target = CreateTargetProject();

        var imported = await ImportEnvironmentAsync(target, yaml, includeServices: true);

        yaml.ShouldNotContain("services");
        imported.Name.ShouldBe(sourceEnvironment.Name);
        imported.Services.ShouldBeEmpty();
    }

    [Test]
    public async Task EnvironmentImport_FromFullExport_CanSkipServices()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: true);
        var target = CreateTargetProject();

        var imported = await ImportEnvironmentAsync(target, yaml, includeServices: false);

        imported.Services.ShouldBeEmpty();
    }

    [Test]
    public async Task EnvironmentImport_GeneratesNewIdsAndANetwork()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: true);

        var imported = await ImportEnvironmentAsync(CreateTargetProject(), yaml, includeServices: true);

        imported.Id.ShouldNotBe(sourceEnvironment.Id);
        imported.Services.Select(s => s.Id).Intersect(sourceEnvironment.Services.Select(s => s.Id)).ShouldBeEmpty();
        _importedNetworks.Count.ShouldBe(1);
    }

    [Test]
    public async Task EnvironmentImport_AliasOverrideAndName_AreApplied()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: true);
        var target = CreateTargetProject();

        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = target.Id, Yaml = yaml, Name = "qa", Alias = "qa" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var imported = target.Environments.Single();
        imported.Name.ShouldBe("qa");
        imported.Alias.ShouldBe("qa");
    }

    [Test]
    public async Task EnvironmentImport_WhenNameAlreadyExistsInProject_ReturnsConflict()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: true);
        var target = CreateTargetProject();
        target.AddEnvironment("dev", "other");

        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = target.Id, Yaml = yaml, Alias = "fresh" }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("CONFLICT");
    }

    [Test]
    public async Task EnvironmentImport_WhenAliasAlreadyExistsInProject_ReturnsConflict()
    {
        var sourceEnvironment = _source.Environments.Single(e => e.Name == "dev");
        var yaml = await ExportEnvironmentAsync(sourceEnvironment, includeServices: true);
        var target = CreateTargetProject();
        target.AddEnvironment("other", "dev");

        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = target.Id, Yaml = yaml, Name = "fresh" }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("CONFLICT");
    }

    [Test]
    public async Task EnvironmentImport_WithoutAnyAlias_ReturnsValidationError()
    {
        const string yaml = """
            id: 22222222-2222-2222-2222-222222222222
            name: dev
            networkName: n1
            """;
        var target = CreateTargetProject();

        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = target.Id, Yaml = yaml }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("VALIDATION");
        target.Environments.ShouldBeEmpty();
    }

    [Test]
    public async Task EnvironmentImport_ForUnknownProject_ReturnsNotFound()
    {
        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = Guid.NewGuid(), Yaml = "name: dev" }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public async Task EnvironmentImport_WithInvalidYaml_ReturnsValidationError()
    {
        var target = CreateTargetProject();

        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = target.Id, Yaml = "name: [unclosed" }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("VALIDATION");
    }

    [Test]
    public async Task EnvironmentExport_ForUnknownEnvironment_ReturnsNotFound()
    {
        var result = await _exportEnvironment.Handle(
            new ExportEnvironmentManifestQuery { EnvironmentId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    // ---------------------------------------------------------------- helpers

    private static Project BuildSourceGraph()
    {
        var project = Project.Create("shop", "shop", "An online shop");

        var dev = project.AddEnvironment("dev", "dev", "Development");
        var api = project.AddService(dev.Id, "api", ServiceType.DockerImage, ExposureMode.External, "api",
            new DockerConfig
            {
                Image = "ghcr.io/acme/api:1.2.3",
                Ports = ["8080:80", "9090:90"],
                CommandArgs = ["--verbose", "--port=80"],
                RestartPolicy = RestartPolicy.Always,
                MemoryLimit = 512 * 1024 * 1024,
                MemoryReservation = 256 * 1024 * 1024,
                NanoCpus = 1_500_000_000
            });
        api.AddVolume(VolumeType.Named, "data", "/var/data", "api-data", backupEnabled: true);
        api.AddVolume(VolumeType.HostPath, "logs", "/var/log/app", "/srv/logs", readOnly: true);
        api.AddFeatureFlag("Beta", FeatureFlagType.EnvironmentVariable, "BETA_ENABLED", "Turns on beta", "true", FeatureFlagValueType.Bool);
        api.AddFeatureFlag("Limit", FeatureFlagType.EnvironmentVariable, "MAX_ITEMS", null, "25", FeatureFlagValueType.Number);
        api.CustomActions.Add(CustomAction.Create(
            api.Id, "Clear cache", "clear-cache", "Clears the cache", "trash",
            new ExecActionConfig(["redis-cli", "flushall"], "/app", "root", ShellType.Sh),
            ["services.update"], ActionRisk.RequireConfirmation, TimeSpan.FromSeconds(45),
            [new CustomActionInput("scope", "Scope", "Which cache", true, "all")]));

        project.AddService(dev.Id, "worker", ServiceType.Dockerfile, ExposureMode.Internal, "wrk",
            new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://github.com/acme/worker.git",
                Branch = "main",
                FilePath = "docker/Dockerfile",
                BuildContext = ".",
                GitCredentialId = Guid.NewGuid(),
                BuildArgs = new Dictionary<string, string> { ["NODE_ENV"] = "production" },
                Ports = ["5000"]
            });

        project.AddService(dev.Id, "cache", ServiceType.DockerImage, ExposureMode.Internal, "cch",
            new DockerConfig { Image = "redis:7" });

        var prod = project.AddEnvironment("prod", "prd", "Production");
        project.AddService(prod.Id, "api", ServiceType.DockerImage, ExposureMode.External, "api",
            new DockerConfig { Image = "ghcr.io/acme/api:1.0.0", Ports = ["80:80"] });

        return project;
    }

    private Project CreateTargetProject()
    {
        var target = Project.Create("target", "tgt");
        _projectRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        return target;
    }

    private async Task<string> ExportProjectAsync(bool includeEnvironments, bool includeServices)
    {
        var result = await _exportProject.Handle(
            new ExportProjectManifestQuery
            {
                ProjectId = _source.Id,
                IncludeEnvironments = includeEnvironments,
                IncludeServices = includeServices
            },
            CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private async Task<string> ExportEnvironmentAsync(Environment environment, bool includeServices)
    {
        var result = await _exportEnvironment.Handle(
            new ExportEnvironmentManifestQuery { EnvironmentId = environment.Id, IncludeServices = includeServices },
            CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private async Task<Project> ImportProjectAsync(string yaml, bool includeEnvironments, bool includeServices)
    {
        var result = await _importProject.Handle(
            new ImportProjectCommand { Yaml = yaml, IncludeEnvironments = includeEnvironments, IncludeServices = includeServices },
            CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.IsSuccess ? null : result.Error.Message);
        return CapturedProject();
    }

    private async Task<Environment> ImportEnvironmentAsync(Project target, string yaml, bool includeServices)
    {
        var result = await _importEnvironment.Handle(
            new ImportEnvironmentCommand { ProjectId = target.Id, Yaml = yaml, IncludeServices = includeServices },
            CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.IsSuccess ? null : result.Error.Message);
        return target.Environments.Single(e => e.Id == result.Value);
    }

    /// <summary>The most recent project the import handler passed to the repository.</summary>
    private Project CapturedProject() =>
        _projectRepository.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IProjectRepository.AddAsync))
            .Select(c => (Project)c.GetArguments()[0]!)
            .Last();

    private static IEnumerable<Guid> AllIds(Project project) =>
        new[] { project.Id }
            .Concat(project.Environments.Select(e => e.Id))
            .Concat(project.Environments.SelectMany(e => e.Services).Select(s => s.Id))
            .Concat(project.Environments.SelectMany(e => e.Services).SelectMany(s => s.Volumes).Select(v => v.Id))
            .Concat(project.Environments.SelectMany(e => e.Services).SelectMany(s => s.CustomActions).Select(a => a.Id));

    // Everything that should survive a round trip; ids, tokens and timestamps are deliberately excluded.
    private string Snapshot(Project project) => JsonSerializer.Serialize(new
    {
        project.Name,
        project.Alias,
        project.Description,
        Environments = project.Environments.OrderBy(e => e.Name).Select(SnapshotOf).ToList()
    }, SnapshotOptions);

    private string Snapshot(Environment environment) => JsonSerializer.Serialize(SnapshotOf(environment), SnapshotOptions);

    private object SnapshotOf(Environment environment) => new
    {
        environment.Name,
        environment.Alias,
        environment.Description,
        environment.NetworkName,
        Services = environment.Services.OrderBy(s => s.Name).Select(SnapshotOf).ToList()
    };

    private object SnapshotOf(Service service) => new
    {
        service.Name,
        service.Alias,
        service.Type,
        service.ExposureMode,
        SourceConfig = service.SourceConfig is null ? (JsonElement?)null : JsonSerializer.SerializeToElement((object)service.SourceConfig, SnapshotOptions),
        Volumes = service.Volumes.OrderBy(v => v.Name).Select(v => new
        {
            v.Type, v.Name, v.Target, v.Source, v.ReadOnly, v.BackupEnabled
        }).ToList(),
        // A null flag description comes back as "" (FeatureFlagManifest defaults it), so the two are treated as equal.
        // Source flags live on the service; imported ones are handed to the feature flag repository instead.
        FeatureFlags = service.FeatureFlags.Concat(_importedFlags.Where(f => f.ServiceId == service.Id))
            .OrderBy(f => f.Name).Select(f => new
            {
                f.Name, f.Type, f.Key, Description = f.Description ?? string.Empty, f.Value, f.ValueType
            }).ToList(),
        CustomActions = service.CustomActions.OrderBy(a => a.ActionName).Select(a => new
        {
            a.ActionName,
            a.Alias,
            a.ActionDescription,
            a.Icon,
            Config = JsonSerializer.SerializeToElement((object)a.Config, SnapshotOptions),
            a.RequiredPermissions,
            a.Risk,
            a.Timeout,
            a.Inputs
        }).ToList()
    };
}
