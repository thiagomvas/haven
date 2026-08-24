using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.RemoveDomainCertificate;

public sealed class RemoveDomainCertificateValidator : AbstractValidator<RemoveDomainCertificateCommand>
{
    public RemoveDomainCertificateValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.DomainId).ValidId();
    }
}
