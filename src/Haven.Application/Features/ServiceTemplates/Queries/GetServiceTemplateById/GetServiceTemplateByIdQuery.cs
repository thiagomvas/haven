using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplateById;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetServiceTemplateByIdQuery : IQuery<ServiceTemplateDto>
{
    public string Id { get; init; } = null!;
}
