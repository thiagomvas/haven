using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.UploadDomainCertificate;

public sealed class UploadDomainCertificateValidator : AbstractValidator<UploadDomainCertificateCommand>
{
    public UploadDomainCertificateValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.DomainId).ValidId();

        RuleFor(x => x.CertificatePem)
            .NotEmpty()
            .WithMessage("Certificate PEM is required.");

        RuleFor(x => x.PrivateKeyPem)
            .NotEmpty()
            .WithMessage("Private key PEM is required.");
    }
}
