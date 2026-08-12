using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.EnvironmentVariables.Queries.ExportEnvExample;

public class ExportEnvExampleHandler(IEnvironmentVariableService environmentVariableService, IEnvironmentVariableSerializer serializer, IFeatureFlagService featureFlagService) : IQueryHandler<ExportEnvExampleQuery, string>
{
    public async ValueTask<Result<string>> Handle(ExportEnvExampleQuery query, CancellationToken cancellationToken)
    {
        var envVars = query.ParentType switch
        {
            EnvironmentVariableParentType.Project => await environmentVariableService.BuildVariablesForProjectAsync(query.ParentId, cancellationToken),
            EnvironmentVariableParentType.Environment => await environmentVariableService.BuildVariablesForEnvironmentAsync(query.ParentId, cancellationToken),
            EnvironmentVariableParentType.Service => await environmentVariableService.BuildVariablesForServiceAsync(query.ParentId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(query.ParentType), query.ParentType, null)
        };

        if (query is { IncludeFeatureFlags: true, ParentType: EnvironmentVariableParentType.Service })
        {
            envVars.AddRange(await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(query.ParentId, cancellationToken));
        }

        var serialized = serializer.Serialize(envVars.OrderBy(e => e.Key), query.IncludeValues);
        return serialized;
    }
}