using Haven.Application.Configuration;

namespace Haven.Application.Features.Instance.Dtos;

public sealed record InstanceDto(string InstanceName, string Timezone, TimeFormat TimeFormat, int DeploymentLogRetentionCount);