using FluentValidation;
using Haven.Domain;

namespace Haven.Application.Features.Services.Commands.CreateService;

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceCommand>
{
    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    public CreateServiceValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Service name cannot be empty.")
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Service name may only contain lowercase letters, digits, and hyphens.")
            .Must(name => !ReservedNames.Contains(name))
            .WithMessage("Service name is reserved and cannot be used.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Service type is invalid.");

        RuleFor(x => x.ExposureMode)
            .IsInEnum()
            .WithMessage("Exposure mode is invalid.");
    }
}
