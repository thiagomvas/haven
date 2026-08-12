using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.EnvironmentVariables.Queries.ExportEnvExample;

[RequirePermission(Permissions.ProjectManagement.Read)]
public class ExportEnvExampleQuery : IQuery<string>
{
    public bool IncludeValues { get; set; }
    public bool IncludeFeatureFlags { get; set; } = true;
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }
}