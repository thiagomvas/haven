using Bogus;

using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.CreateEnvironment;
using Haven.Application.Features.Projects.Commands.CreateProject;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain;
using Haven.Domain.ValueObjects;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Developer;

public class SeedEndpoint : EndpointWithoutRequest<ApiResponse<SeedResult>>
{
    private readonly IMediator _mediator;

    public SeedEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/dev/seed");
        AllowAnonymous();
        Options(x => x.WithTags("Developer"));
        Summary(s =>
        {
            s.Summary = "Seed database with sample data";
            s.Description = "Creates a project with environments and services for development/testing purposes.";
            s[201] = "Created";
        });
    }

    private static string GenerateValidServiceName(Faker faker)
    {
        // Generate a name with only lowercase letters and hyphens
        var word1 = new string(faker.Commerce.ProductName()
            .ToLower()
            .Where(c => char.IsLower(c) || (char.IsDigit(c)))
            .ToArray());
        var word2 = new string(faker.Random.Word()
            .ToLower()
            .Where(c => char.IsLower(c) || char.IsDigit(c))
            .ToArray());

        var name = $"{word1}-{word2}".TrimStart('-').TrimEnd('-');
        return string.IsNullOrEmpty(name) ? "service" : name;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var envNames = new[] { "Development", "Staging", "Production" };
        var containerImages = new[]
        {
            ("traefik:latest", 8080, "traefikproxy"),
            ("traefik:latest", 8081, "traefikdash"),
            ("whoami:latest", 80, "whoami"),
            ("whoami:latest", 8080, "whoamiapi"),
        };

        var faker = new Faker();
        var projectName = faker.Commerce.ProductName();
        var projectDesc = faker.Lorem.Sentence();

        // Create project
        var createProjectCmd = new CreateProjectCommand { Name = projectName, Description = projectDesc };
        var projectResult = await _mediator.Send(createProjectCmd, ct);

        if (projectResult.IsFailure)
        {
            await this.SendResultAsync(Result<SeedResult>.Failure(projectResult.Error), ct);
            return;
        }

        var projectId = projectResult.Value;
        var envIds = new List<Guid>();

        // Create 2-3 environments
        var envCount = faker.Random.Int(2, 3);
        for (int i = 0; i < envCount; i++)
        {
            var envName = envNames[i % envNames.Length];
            var createEnvCmd = new CreateEnvironmentCommand
            {
                ProjectId = projectId,
                Name = envName,
                Description = faker.Lorem.Sentence()
            };
            var envResult = await _mediator.Send(createEnvCmd, ct);

            if (envResult.IsFailure)
            {
                await this.SendResultAsync(Result<SeedResult>.Failure(envResult.Error), ct);
                return;
            }

            envIds.Add(envResult.Value);
        }

        var serviceIds = new Dictionary<Guid, List<Guid>>();

        // Create services in each environment
        foreach (var envId in envIds)
        {
            var serviceCount = faker.Random.Int(3, 6);
            var services = new List<Guid>();

            for (int i = 0; i < serviceCount; i++)
            {
                var (imageName, port, _) = containerImages[i % containerImages.Length];
                var serviceName = GenerateValidServiceName(faker);

                var dockerConfig = new DockerConfig
                {
                    Image = imageName,
                    Ports = new List<string> { $"{port}:80" },
                    Volumes = [],
                    EnvironmentVariables = new List<string> { "NODE_ENV=development" },
                    RestartPolicy = RestartPolicy.UnlessStopped
                };

                var createServiceCmd = new CreateServiceCommand
                {
                    ProjectId = projectId,
                    EnvironmentId = envId,
                    Name = serviceName,
                    Type = ServiceType.DockerImage,
                    ExposureMode = faker.PickRandom<ExposureMode>(),
                    DockerConfig = dockerConfig
                };

                var serviceResult = await _mediator.Send(createServiceCmd, ct);

                if (serviceResult.IsFailure)
                {
                    await this.SendResultAsync(Result<SeedResult>.Failure(serviceResult.Error), ct);
                    return;
                }

                services.Add(serviceResult.Value);
            }

            serviceIds[envId] = services;
        }

        var result = new SeedResult
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Environments = envIds.Count,
            Services = serviceIds.Values.SelectMany(x => x).Count()
        };

        var seedResult = Result<SeedResult>.CreatedFor(result);
        await this.SendResultAsync(seedResult, ct);
    }
}

public class SeedResult
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int Environments { get; set; }
    public int Services { get; set; }
}