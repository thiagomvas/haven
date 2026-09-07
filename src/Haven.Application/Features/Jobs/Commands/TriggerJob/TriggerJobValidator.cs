using FluentValidation;

namespace Haven.Application.Features.Jobs.Commands.TriggerJob;

public class TriggerJobValidator : AbstractValidator<TriggerJobCommand>
{
    public TriggerJobValidator()
    {
        RuleFor(x => x.JobKey)
            .NotEmpty().WithMessage("JobKey is required.")
            .MaximumLength(100).WithMessage("JobKey must not exceed 100 characters.");
    }
    
}