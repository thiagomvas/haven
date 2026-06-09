using FluentValidation;

namespace Haven.Application.Features.Instance.Commands.UpdateInstance;

public sealed class UpdateInstanceValidator : AbstractValidator<UpdateInstanceCommand>
{
    public UpdateInstanceValidator()
    {
        RuleFor(x => x.InstanceName)
            .NotEmpty().WithMessage("Instance name is required.")
            .MaximumLength(64).WithMessage("Instance name cannot exceed 64 characters.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .Must(BeValidTimezone).WithMessage("Timezone must be a valid IANA timezone identifier.");
    }

    private static bool BeValidTimezone(string timezone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            return false;
        }
    }
}