using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;

public sealed class DeleteDomainValidator : AbstractValidator<DeleteDomainCommand>
{
    public DeleteDomainValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.DomainId).ValidId();
    }
}
