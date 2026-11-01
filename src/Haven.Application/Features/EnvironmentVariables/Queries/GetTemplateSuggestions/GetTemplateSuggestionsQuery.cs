using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetTemplateSuggestions;

/// <summary>
/// Returns the <c>${{ namespace.key }}</c> references that can be used when editing the variables of a
/// project, environment or service. Only key names are returned, never values.
/// </summary>
[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class GetTemplateSuggestionsQuery : IQuery<TemplateSuggestionsDto>
{
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }
}
