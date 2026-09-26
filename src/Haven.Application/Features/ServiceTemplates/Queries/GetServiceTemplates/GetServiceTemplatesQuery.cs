using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplates;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetServiceTemplatesQuery : IQuery<IReadOnlyList<ServiceTemplateSummaryDto>>
{
}
