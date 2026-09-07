using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.Exceptions;

namespace Haven.Application.Features.SslCertificates.Commands.UploadSslCertificate;

public sealed class UploadSslCertificateHandler(ISslCertificateRepository sslCertificateRepository)
    : ICommandHandler<UploadSslCertificateCommand, UploadSslCertificateResult>
{
    public async ValueTask<Result<UploadSslCertificateResult>> Handle(UploadSslCertificateCommand command, CancellationToken cancellationToken)
    {
        SslCertificate certificate;
        try
        {
            certificate = SslCertificate.Create(command.Name, command.CertificatePem, command.PrivateKeyPem);
        }
        catch (ValidationException ex)
        {
            return Error.Validation(ex.Message);
        }

        await sslCertificateRepository.AddAsync(certificate, cancellationToken);

        var warnings = new List<string>();
        if (certificate.IsExpired)
            warnings.Add("The uploaded certificate has already expired.");

        return Result<UploadSslCertificateResult>.Success(new UploadSslCertificateResult
        {
            CertificateId = certificate.Id,
            NotAfter = certificate.NotAfter,
            Warnings = warnings
        });
    }
}