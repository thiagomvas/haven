using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services.Queries;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Services.Queries.GetServiceDashboard;

public sealed class GetServiceDashboardHandler(
    IServiceRepository serviceRepository,
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagRepository featureFlagRepository,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    IOptionsMonitor<NetworkOptions> networkOptions)
    : IQueryHandler<GetServiceDashboardQuery, ServiceDashboardDto>
{
    public async ValueTask<Result<ServiceDashboardDto>> Handle(GetServiceDashboardQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null || service.EnvironmentId != query.EnvironmentId)
            return Error.NotFoundFor("Service", query.ServiceId);

        var dto = service.ToDashboardDto();
        dto.WebhookUrl = BuildWebhookUrl(service.Token);

        var envVars = await environmentVariableService.BuildVariablesForServiceAsync(query.ServiceId, cancellationToken);
        dto.EnvironmentVariables = envVars.Select(ev => ev.ToDto()).ToList();

        var flags = await featureFlagRepository.GetForServiceListAsync(query.ServiceId, cancellationToken);
        dto.FeatureFlags = flags.Select(ff => ff.ToDto()).ToList();

        var registry = await serviceRegistryEntryRepository.GetForServiceAsync(query.ServiceId, cancellationToken);
        if (registry is not null)
            dto.Registry = registry.ToRegistryDto();

        return Result<ServiceDashboardDto>.Success(dto);
    }

    private string BuildWebhookUrl(string token)
    {
        var path = $"/webhooks/deploy/{token}";
        var host = networkOptions.CurrentValue.BuildHost();
        return host is not null ? $"{host}{path}" : path;
    }
}