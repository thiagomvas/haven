using FluentValidation;

namespace Haven.Application.Features.Services.Commands.ExportServiceToDockerCompose;

public sealed class ExportServiceToDockerComposeValidator : AbstractValidator<ExportServiceToDockerComposeCommand>
{
    public ExportServiceToDockerComposeValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("Service ID cannot be empty.");
    }
}
