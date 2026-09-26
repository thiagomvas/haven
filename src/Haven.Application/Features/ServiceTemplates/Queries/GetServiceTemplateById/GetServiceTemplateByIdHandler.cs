using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceTemplates.Contracts;

namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplateById;

public sealed class GetServiceTemplateByIdHandler(IServiceTemplateRepository repository)
    : IQueryHandler<GetServiceTemplateByIdQuery, ServiceTemplateDto>
{
    public async ValueTask<Result<ServiceTemplateDto>> Handle(
        GetServiceTemplateByIdQuery query,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (template is null)
            return Error.NotFoundFor(nameof(ServiceTemplate), query.Id);

        var dto = new ServiceTemplateDto
        {
            Id = template.Id,
            Version = template.Version.ToString(),
            Name = template.Name,
            Icon = template.Icon,
            Category = template.Category,
            Inputs = template.Inputs
                .Select(i => new TemplateInputFieldDto
                {
                    Key = i.Key,
                    Type = i.Type,
                    Label = i.Label,
                    DefaultValue = i.DefaultValue,
                    Immutable = i.Immutable,
                    Options = i.Options
                })
                .ToList()
        };

        return Result<ServiceTemplateDto>.Success(dto);
    }
}
