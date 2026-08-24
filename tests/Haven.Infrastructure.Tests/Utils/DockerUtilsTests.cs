using System.Formats.Tar;

using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Utils;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Tests.Utils;

[Category("Unit")]
public sealed class DockerUtilsTests
{
    [Test]
    public void BuildImageTag_WithAliases_UsesAliasFormat()
    {
        var serviceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        var tag = DockerUtils.BuildImageTag("myapp", "prod", "api", serviceId);

        tag.ShouldBe("haven-myapp-prod-api");
    }

    [Test]
    public void BuildImageTag_WithoutAliases_UsesLegacyFormat()
    {
        var serviceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        var tag = DockerUtils.BuildImageTag(null, null, null, serviceId);

        tag.ShouldBe("haven-service-550e8400e29b41d4a716446655440000");
    }

    [Test]
    public void BuildContainerName_WithAliases_UsesAliasFormat()
    {
        var id = Guid.NewGuid();

        var name = DockerUtils.BuildContainerName("myapp", "prod", "api", "my-service", id);

        name.ShouldBe("haven-myapp-prod-api");
    }

    [Test]
    public void BuildContainerName_WithoutAliases_CreatesLegacyNameWithPrefix()
    {
        var id = Guid.NewGuid();

        var name = DockerUtils.BuildContainerName(null, null, null, "my-service", id);

        name.ShouldStartWith("haven-");
        name.Length.ShouldBeLessThanOrEqualTo(63);
    }

    [Test]
    public void BuildContainerName_WithoutAliases_ContainsShortId()
    {
        var id = Guid.NewGuid();
        var shortId = id.ToString("N")[..12];

        var name = DockerUtils.BuildContainerName(null, null, null, "my-service", id);

        name.ShouldEndWith(shortId);
    }

    [Test]
    public void BuildContainerName_WithoutAliases_WithInvalidCharacters_NormalizesName()
    {
        var id = Guid.NewGuid();

        var name = DockerUtils.BuildContainerName(null, null, null, "My@Service#123", id);

        name.ShouldNotContain("@");
        name.ShouldNotContain("#");
        name.Length.ShouldBeLessThanOrEqualTo(63);
    }

    [Test]
    public void BuildContainerName_WithoutAliases_WithVeryLongName_TruncatesToMaxLength()
    {
        var id = Guid.NewGuid();
        var longName = new string('a', 100);

        var name = DockerUtils.BuildContainerName(null, null, null, longName, id);

        name.Length.ShouldBeLessThanOrEqualTo(63);
    }

    [Test]
    public void BuildContainerLabels_IncludesHavenManagedLabel()
    {
        var project = Project.Create("test-project", description: "desc");
        var environment = project.AddEnvironment("dev");
        var service = project.AddService(environment.Id, "my-service", ServiceType.DockerImage, ExposureMode.Internal);

        var trackedService = GetServiceWithRelations(service, environment, project);
        var labels = DockerUtils.BuildContainerLabels(trackedService);

        labels.ShouldContainKey("haven.managed");
        labels["haven.managed"].ShouldBe("true");
    }

    [Test]
    public void BuildContainerLabels_IncludesServiceId()
    {
        var project = Project.Create("test-project", description: "desc");
        var environment = project.AddEnvironment("dev");
        var service = project.AddService(environment.Id, "my-service", ServiceType.DockerImage, ExposureMode.Internal);

        var trackedService = GetServiceWithRelations(service, environment, project);
        var labels = DockerUtils.BuildContainerLabels(trackedService);

        labels.ShouldContainKey("haven.service.id");
        labels["haven.service.id"].ShouldBe(service.Id.ToString());
    }

    [Test]
    public void BuildContainerLabels_IncludesServiceName()
    {
        var project = Project.Create("test-project", description: "desc");
        var environment = project.AddEnvironment("dev");
        var service = project.AddService(environment.Id, "my-service", ServiceType.DockerImage, ExposureMode.Internal);

        var trackedService = GetServiceWithRelations(service, environment, project);
        var labels = DockerUtils.BuildContainerLabels(trackedService);

        labels.ShouldContainKey("haven.service.name");
        labels["haven.service.name"].ShouldBe("my-service");
    }

    [Test]
    public void BuildContainerLabels_IncludesEnvironmentAndProjectNames()
    {
        var project = Project.Create("my-project", description: "desc");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "my-service", ServiceType.DockerImage, ExposureMode.Internal);

        var trackedService = GetServiceWithRelations(service, environment, project);
        var labels = DockerUtils.BuildContainerLabels(trackedService);

        labels.ShouldContainKey("haven.environment.name");
        labels["haven.environment.name"].ShouldBe("staging");
        labels.ShouldContainKey("haven.project.name");
        labels["haven.project.name"].ShouldBe("my-project");
    }

    [Test]
    public void Normalize_ValidInput_ReturnsLowercase()
    {
        var result = DockerUtils.Normalize("MyService");

        result.ShouldBe("myservice");
    }

    [Test]
    public void Normalize_InvalidCharacters_ReplacedWithHyphens()
    {
        var result = DockerUtils.Normalize("My@Service#123");

        result.ShouldNotContain("@");
        result.ShouldNotContain("#");
    }

    [Test]
    public void Normalize_ConsecutiveHyphens_Collapsed()
    {
        var result = DockerUtils.Normalize("My---Service");

        result.ShouldBe("my-service");
    }

    [Test]
    public void Normalize_NullOrEmpty_ReturnsUnknown()
    {
        DockerUtils.Normalize(null!).ShouldBe("unknown");
        DockerUtils.Normalize(string.Empty).ShouldBe("unknown");
        DockerUtils.Normalize("   ").ShouldBe("unknown");
    }

    [Test]
    public async Task CreateTarArchiveFromContent_CreatesDockerfileEntry()
    {
        var content = "FROM ubuntu:22.04\nRUN echo hello";

        var stream = await DockerUtils.CreateTarArchiveFromContentAsync(content, CancellationToken.None);

        stream.ShouldNotBeNull();
        stream.Length.ShouldBeGreaterThan(0);

        stream.Position = 0;
        using var reader = new TarReader(stream);
        var entry = await reader.GetNextEntryAsync();
        entry.ShouldNotBeNull();
        entry.Name.ShouldBe("Dockerfile");
    }

    [Test]
    public async Task CreateTarArchiveFromContent_PreservesContent()
    {
        var content = "FROM ubuntu:22.04\nRUN echo hello";

        var stream = await DockerUtils.CreateTarArchiveFromContentAsync(content, CancellationToken.None);

        stream.Position = 0;
        using var reader = new TarReader(stream);
        var entry = await reader.GetNextEntryAsync();
        entry.ShouldNotBeNull();

        using var dataStream = new MemoryStream();
        await entry.DataStream!.CopyToAsync(dataStream);
        var readContent = System.Text.Encoding.UTF8.GetString(dataStream.ToArray());
        readContent.ShouldBe(content);
    }

    [Test]
    public async Task CreateTarArchiveFromDirectory_CreatesStreamWithFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"haven-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Dockerfile"), "FROM ubuntu:22.04");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "app.py"), "print('hello')");

        try
        {
            var stream = await DockerUtils.CreateTarArchiveFromDirectoryAsync(tempDir, CancellationToken.None);

            stream.ShouldNotBeNull();
            stream.Length.ShouldBeGreaterThan(0);

            stream.Position = 0;
            using var reader = new TarReader(stream);
            var entryNames = new List<string>();
            TarEntry? entry;
            while ((entry = await reader.GetNextEntryAsync()) != null)
            {
                entryNames.Add(entry.Name);
            }

            entryNames.Count.ShouldBeGreaterThanOrEqualTo(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateTarArchiveFromDirectory_NonExistentDirectory_Throws()
    {
        var nonExistentPath = "/nonexistent/path/that/does/not/exist";

        await Should.ThrowAsync<DirectoryNotFoundException>(
            () => DockerUtils.CreateTarArchiveFromDirectoryAsync(nonExistentPath, CancellationToken.None));
    }

    [Test]
    public async Task CreateTarArchiveFromDirectory_EmptyDirectory_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"haven-empty-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await Should.ThrowAsync<InvalidOperationException>(
                () => DockerUtils.CreateTarArchiveFromDirectoryAsync(tempDir, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateTarArchiveFromDirectory_UsesForwardSlashPaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"haven-test-{Guid.NewGuid()}");
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "file.txt"), "content");

        try
        {
            var stream = await DockerUtils.CreateTarArchiveFromDirectoryAsync(tempDir, CancellationToken.None);

            stream.Position = 0;
            using var reader = new TarReader(stream);
            var entry = await reader.GetNextEntryAsync();
            entry.ShouldNotBeNull();
            entry.Name.ShouldNotContain("\\");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static Service GetServiceWithRelations(Service service, Environment environment, Project project)
    {
        typeof(Environment).GetProperty(nameof(Environment.Project))?.SetValue(environment, project);
        typeof(Service).GetProperty(nameof(Service.Environment))?.SetValue(service, environment);
        return service;
    }

    [Test]
    public void BuildEnvironmentVariableStrings_NullInput_ReturnsEmptyList()
    {
        var result = DockerUtils.BuildEnvironmentVariableStrings(null);

        result.ShouldBeEmpty();
    }

    [Test]
    public void BuildEnvironmentVariableStrings_FormatsAsKeyEqualsValue()
    {
        var envs = new List<EnvironmentVariables>
        {
            new() { Key = "FOO", Value = "bar" },
            new() { Key = "BAZ", Value = "qux" }
        };

        var result = DockerUtils.BuildEnvironmentVariableStrings(envs);

        result.ShouldBe(["FOO=bar", "BAZ=qux"]);
    }

    [Test]
    public void TryBuildListenAddress_Internal_ReturnsLoopback()
    {
        DockerUtils.TryBuildListenAddress(ExposureMode.Internal).ShouldBe("127.0.0.1");
    }

    [Test]
    public void TryBuildListenAddress_External_ReturnsAllInterfaces()
    {
        DockerUtils.TryBuildListenAddress(ExposureMode.External).ShouldBe("0.0.0.0");
    }

    [Test]
    public void TryBuildListenAddress_Custom_ReturnsAllInterfaces()
    {
        DockerUtils.TryBuildListenAddress(ExposureMode.Custom).ShouldBe("0.0.0.0");
    }

    [Test]
    public void TryBuildListenAddress_None_ReturnsNull()
    {
        DockerUtils.TryBuildListenAddress(ExposureMode.None).ShouldBeNull();
    }

    [Test]
    public void BuildPortBindings_DefaultMode_UsesListenAddressAsHostIp()
    {
        var result = DockerUtils.BuildPortBindings(["8080:80"], ExposureMode.Internal, "127.0.0.1");

        result.Warnings.ShouldBeEmpty();
        result.ExposedPorts.ShouldContainKey("80/tcp");
        result.PortBindings.ShouldContainKey("80/tcp");
        var binding = result.PortBindings["80/tcp"].ShouldHaveSingleItem();
        binding.HostIP.ShouldBe("127.0.0.1");
        binding.HostPort.ShouldBe("8080");
    }

    [Test]
    public void BuildPortBindings_CustomModeWithExplicitIp_UsesExplicitIp()
    {
        var result = DockerUtils.BuildPortBindings(["10.0.0.5:8080:80"], ExposureMode.Custom, "0.0.0.0");

        var binding = result.PortBindings["80/tcp"].ShouldHaveSingleItem();
        binding.HostIP.ShouldBe("10.0.0.5");
        binding.HostPort.ShouldBe("8080");
    }

    [Test]
    public void BuildPortBindings_CustomModeWithoutExplicitIp_DefaultsToListenAddress()
    {
        var result = DockerUtils.BuildPortBindings(["8080:80"], ExposureMode.Custom, "0.0.0.0");

        var binding = result.PortBindings["80/tcp"].ShouldHaveSingleItem();
        binding.HostIP.ShouldBe("0.0.0.0");
        binding.HostPort.ShouldBe("8080");
    }

    [Test]
    public void BuildPortBindings_InvalidFormat_SkipsAndReturnsWarning()
    {
        var result = DockerUtils.BuildPortBindings(["not-a-port"], ExposureMode.Internal, "127.0.0.1");

        result.ExposedPorts.ShouldBeEmpty();
        result.PortBindings.ShouldBeEmpty();
        result.Warnings.ShouldHaveSingleItem();
    }

    [Test]
    public void BuildPortBindings_PortWithProtocolSuffix_PreservesProtocol()
    {
        var result = DockerUtils.BuildPortBindings(["8080:80/udp"], ExposureMode.Internal, "127.0.0.1");

        result.ExposedPorts.ShouldContainKey("80/udp");
        result.PortBindings.ShouldContainKey("80/udp");
    }

    [Test]
    public void BuildTraefikLabels_NullEntry_ReturnsEmpty()
    {
        DockerUtils.BuildTraefikLabels(null).ShouldBeEmpty();
    }

    [Test]
    public void BuildTraefikLabels_NoDomains_ReturnsEmpty()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());

        DockerUtils.BuildTraefikLabels(entry).ShouldBeEmpty();
    }

    [Test]
    public void BuildTraefikLabels_SingleDomain_BuildsRouterAndServiceLabels()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("app.example.com", 8080);

        var labels = DockerUtils.BuildTraefikLabels(entry);

        labels["traefik.enable"].ShouldBe("true");
        var routerName = labels.Keys.Single(k => k.StartsWith("traefik.http.routers.") && k.EndsWith(".rule"))
            .Split('.')[3];
        labels[$"traefik.http.routers.{routerName}.rule"].ShouldBe("Host(`app.example.com`)");
        labels[$"traefik.http.routers.{routerName}.entrypoints"].ShouldBe("web");
        labels[$"traefik.http.routers.{routerName}.service"].ShouldBe(routerName);
        labels[$"traefik.http.services.{routerName}.loadbalancer.server.port"].ShouldBe("8080");
    }

    [Test]
    public void BuildTraefikLabels_MultipleDomains_ProducesDistinctRouters()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        entry.AddDomain("one.example.com", 8080);
        entry.AddDomain("two.example.com", 9090);

        var labels = DockerUtils.BuildTraefikLabels(entry);

        var routerNames = labels.Keys
            .Where(k => k.StartsWith("traefik.http.routers.") && k.EndsWith(".rule"))
            .Select(k => k.Split('.')[3])
            .Distinct()
            .ToList();

        routerNames.Count.ShouldBe(2);
        labels.Values.ShouldContain("Host(`one.example.com`)");
        labels.Values.ShouldContain("Host(`two.example.com`)");
        labels.Values.ShouldContain("9090");
        labels.Values.ShouldContain("8080");
    }

    [Test]
    public void BuildTraefikLabels_TlsDisabled_DoesNotAddSecureRouterOrRedirect()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("app.example.com", 8080);

        var labels = DockerUtils.BuildTraefikLabels(entry);

        var routerName = labels.Keys.Single(k => k.StartsWith("traefik.http.routers.") && k.EndsWith(".rule"))
            .Split('.')[3];
        labels.ShouldNotContainKey($"traefik.http.routers.{routerName}.middlewares");
        labels.Keys.ShouldNotContain(k => k.EndsWith(".tls"));
        labels.Keys.ShouldNotContain(k => k.EndsWith(".tls.certresolver"));
    }

    [Test]
    public void BuildTraefikLabels_TlsModeAcme_AddsSecureRouterAndHttpsRedirectWithCertResolver()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("secure.example.com", 8080, tlsMode: TlsMode.Acme);

        var labels = DockerUtils.BuildTraefikLabels(entry);

        var routerName = labels.Keys
            .Single(k => k.StartsWith("traefik.http.routers.") && k.EndsWith(".rule") && !k.Contains("-secure"))
            .Split('.')[3];
        var secureRouterName = $"{routerName}-secure";

        var redirectMiddleware = $"{routerName}-redirect";
        labels[$"traefik.http.routers.{routerName}.middlewares"].ShouldBe(redirectMiddleware);
        labels[$"traefik.http.middlewares.{redirectMiddleware}.redirectscheme.scheme"].ShouldBe("https");

        labels[$"traefik.http.routers.{secureRouterName}.rule"].ShouldBe("Host(`secure.example.com`)");
        labels[$"traefik.http.routers.{secureRouterName}.entrypoints"].ShouldBe("websecure");
        labels[$"traefik.http.routers.{secureRouterName}.service"].ShouldBe(routerName);
        labels[$"traefik.http.routers.{secureRouterName}.tls"].ShouldBe("true");
        labels[$"traefik.http.routers.{secureRouterName}.tls.certresolver"].ShouldBe("letsencrypt");
    }

    [Test]
    public void BuildTraefikLabels_TlsModeCustom_AddsSecureRouterWithoutCertResolver()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("custom.example.com", 8080, tlsMode: TlsMode.Custom);

        var labels = DockerUtils.BuildTraefikLabels(entry);

        var routerName = labels.Keys
            .Single(k => k.StartsWith("traefik.http.routers.") && k.EndsWith(".rule") && !k.Contains("-secure"))
            .Split('.')[3];
        var secureRouterName = $"{routerName}-secure";

        labels[$"traefik.http.routers.{secureRouterName}.tls"].ShouldBe("true");
        labels.ShouldNotContainKey($"traefik.http.routers.{secureRouterName}.tls.certresolver");
    }

    [Test]
    public void BuildTraefikDashboardLabels_NullEntry_ReturnsEmpty()
    {
        DockerUtils.BuildTraefikDashboardLabels(null, "admin", "hash").ShouldBeEmpty();
    }

    [Test]
    public void BuildTraefikDashboardLabels_RoutesToApiInternal_NotALoadbalancerPort()
    {
        var entry = ServiceRegistryEntry.CreateForSidecar(Guid.NewGuid());
        entry.AddDomain("traefik.example.com", 8080);

        var labels = DockerUtils.BuildTraefikDashboardLabels(entry, null, null);

        var routerName = labels.Keys.Single(k => k.StartsWith("traefik.http.routers.") && k.EndsWith(".rule"))
            .Split('.')[3];
        labels[$"traefik.http.routers.{routerName}.service"].ShouldBe("api@internal");
        labels.Keys.ShouldNotContain(k => k.Contains("loadbalancer"));
    }

    [Test]
    public void BuildTraefikDashboardLabels_NoCredentials_NoAuthMiddleware()
    {
        var entry = ServiceRegistryEntry.CreateForSidecar(Guid.NewGuid());
        entry.AddDomain("traefik.example.com", 8080);

        var labels = DockerUtils.BuildTraefikDashboardLabels(entry, null, null);

        labels.Keys.ShouldNotContain(k => k.Contains("basicauth"));
        labels.Keys.ShouldNotContain(k => k.EndsWith(".middlewares") && labels[k].Contains("-auth"));
    }

    [Test]
    public void BuildTraefikDashboardLabels_WithCredentials_AddsBasicAuthMiddlewareOnPlainRouter()
    {
        var entry = ServiceRegistryEntry.CreateForSidecar(Guid.NewGuid());
        var domain = entry.AddDomain("traefik.example.com", 8080);

        var labels = DockerUtils.BuildTraefikDashboardLabels(entry, "admin", "$2a$hash");

        var authMiddleware = $"{domain.RouterName}-auth";
        labels[$"traefik.http.middlewares.{authMiddleware}.basicauth.users"].ShouldBe("admin:$2a$hash");
        labels[$"traefik.http.routers.{domain.RouterName}.middlewares"].ShouldBe(authMiddleware);
    }

    [Test]
    public void BuildTraefikDashboardLabels_TlsEnabledWithCredentials_AttachesAuthToSecureRouterOnly()
    {
        var entry = ServiceRegistryEntry.CreateForSidecar(Guid.NewGuid());
        var domain = entry.AddDomain("traefik.example.com", 8080, tlsMode: TlsMode.Acme);

        var labels = DockerUtils.BuildTraefikDashboardLabels(entry, "admin", "$2a$hash");

        var authMiddleware = $"{domain.RouterName}-auth";
        var redirectMiddleware = $"{domain.RouterName}-redirect";
        labels[$"traefik.http.routers.{domain.RouterName}.middlewares"].ShouldBe(redirectMiddleware);
        labels[$"traefik.http.routers.{domain.SecureRouterName}.middlewares"].ShouldBe(authMiddleware);
    }

    [Test]
    public void BuildTraefikLabels_InternalBasePathSet_AddsAddPrefixMiddlewareOnPlainRouter()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("api.example.com", 8080, internalBasePath: "/api/v1");

        var labels = DockerUtils.BuildTraefikLabels(entry);

        var addPrefixMiddleware = $"{domain.RouterName}-addprefix";
        labels[$"traefik.http.middlewares.{addPrefixMiddleware}.addprefix.prefix"].ShouldBe("/api/v1");
        labels[$"traefik.http.routers.{domain.RouterName}.middlewares"].ShouldBe(addPrefixMiddleware);
    }

    [Test]
    public void BuildTraefikLabels_InternalBasePathNotSet_NoAddPrefixMiddleware()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("app.example.com", 8080);

        var labels = DockerUtils.BuildTraefikLabels(entry);

        labels.Keys.ShouldNotContain(k => k.Contains("addprefix"));
        labels.ShouldNotContainKey($"traefik.http.routers.{domain.RouterName}.middlewares");
    }

    [Test]
    public void BuildTraefikLabels_InternalBasePathWithTls_AddsAddPrefixOnSecureRouterOnly()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("api.example.com", 8080, tlsMode: TlsMode.Acme, internalBasePath: "/api/v1");

        var labels = DockerUtils.BuildTraefikLabels(entry);

        var addPrefixMiddleware = $"{domain.RouterName}-addprefix";
        var redirectMiddleware = $"{domain.RouterName}-redirect";
        labels[$"traefik.http.routers.{domain.RouterName}.middlewares"].ShouldBe(redirectMiddleware);
        labels[$"traefik.http.routers.{domain.SecureRouterName}.middlewares"].ShouldBe(addPrefixMiddleware);
    }

    [Test]
    public void BuildTraefikDashboardLabels_InternalBasePathSetOnDomain_IsIgnored()
    {
        var entry = ServiceRegistryEntry.CreateForSidecar(Guid.NewGuid());
        var domain = entry.AddDomain("traefik.example.com", 8080, internalBasePath: "/should-be-ignored");

        var labels = DockerUtils.BuildTraefikDashboardLabels(entry, null, null);

        labels.Keys.ShouldNotContain(k => k.Contains("addprefix"));
        labels.ShouldNotContainKey($"traefik.http.routers.{domain.RouterName}.middlewares");
    }
}