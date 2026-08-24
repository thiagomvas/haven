using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;

public sealed class AttachDomainCertificateValidator : AbstractValidator<AttachDomainCertificateCommand>
{
    public AttachDomainCertificateValidator()
    {
        RuleFor(x => x.DomainId).ValidId();
        RuleFor(x => x.CertificateId).ValidId();
    }
}
