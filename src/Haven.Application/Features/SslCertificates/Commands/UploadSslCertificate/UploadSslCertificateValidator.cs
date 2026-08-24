using FluentValidation;

namespace Haven.Application.Features.SslCertificates.Commands.UploadSslCertificate;

public sealed class UploadSslCertificateValidator : AbstractValidator<UploadSslCertificateCommand>
{
    public UploadSslCertificateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("A name is required.")
            .MaximumLength(200);

        RuleFor(x => x.CertificatePem)
            .NotEmpty()
            .WithMessage("Certificate PEM is required.");

        RuleFor(x => x.PrivateKeyPem)
            .NotEmpty()
            .WithMessage("Private key PEM is required.");
    }
}
