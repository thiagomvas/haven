using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplates;

public sealed class GetServiceTemplatesHandler(IServiceTemplateRepository repository)
    : IQueryHandler<GetServiceTemplatesQuery, IReadOnlyList<ServiceTemplateSummaryDto>>
{
    public async ValueTask<Result<IReadOnlyList<ServiceTemplateSummaryDto>>> Handle(
        GetServiceTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var templates = await repository.GetAllAsync(cancellationToken);

        IReadOnlyList<ServiceTemplateSummaryDto> dtos = templates
            .Select(t => new ServiceTemplateSummaryDto
            {
                Id = t.Id,
                Version = t.Version.ToString(),
                Name = t.Name,
                Icon = t.Icon,
                Category = t.Category
            })
            .ToList();

        return Result<IReadOnlyList<ServiceTemplateSummaryDto>>.Success(dtos);
    }
}
