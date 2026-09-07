using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;

public sealed class DeleteDomainValidator : AbstractValidator<DeleteDomainCommand>
{
    public DeleteDomainValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ServiceId.HasValue ^ x.SidecarId.HasValue)
            .WithMessage("Exactly one of ServiceId or SidecarId must be provided.");

        RuleFor(x => x.ServiceId.Value).ValidId().When(x => x.ServiceId.HasValue);
        RuleFor(x => x.SidecarId.Value).ValidId().When(x => x.SidecarId.HasValue);
        RuleFor(x => x.DomainId).ValidId();
    }
}