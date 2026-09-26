using FluentValidation;

namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplateById;

public sealed class GetServiceTemplateByIdValidator : AbstractValidator<GetServiceTemplateByIdQuery>
{
    public GetServiceTemplateByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Template ID is required.");
    }
}
