using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Exceptions;

namespace Haven.Application.Features.ServiceRegistry.Commands.UploadDomainCertificate;

public sealed class UploadDomainCertificateHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    IDomainCertificateRepository domainCertificateRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<UploadDomainCertificateCommand, UploadDomainCertificateResult>
{
    public async ValueTask<Result<UploadDomainCertificateResult>> Handle(UploadDomainCertificateCommand command, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.ServiceId);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        if (domain.TlsMode != TlsMode.Custom)
            return Error.InvalidOperation("The domain's TLS mode must be 'Custom' before a certificate can be uploaded.");

        DomainCertificate certificate;
        try
        {
            var existing = await domainCertificateRepository.GetByDomainIdAsync(domain.Id, cancellationToken);
            if (existing is not null)
            {
                existing.Rotate(command.CertificatePem, command.PrivateKeyPem);
                certificate = existing;
            }
            else
            {
                certificate = DomainCertificate.Create(domain.Id, command.CertificatePem, command.PrivateKeyPem);
                await domainCertificateRepository.AddAsync(certificate, cancellationToken);
            }
        }
        catch (ValidationException ex)
        {
            return Error.Validation(ex.Message);
        }

        var writeResult = await traefikDynamicConfigWriter.WriteDomainCertificateAsync(
            domain.Id, certificate.CertificatePem, certificate.PrivateKeyPem, cancellationToken);
        if (writeResult.IsFailure)
            return writeResult.Error;

        var warnings = new List<string>();
        if (certificate.IsExpired)
            warnings.Add("The uploaded certificate has already expired.");
        if (!certificate.MatchesHostname(domain.Hostname))
            warnings.Add($"The certificate's subject/SANs do not include '{domain.Hostname}'.");

        return Result<UploadDomainCertificateResult>.Success(new UploadDomainCertificateResult
        {
            CertificateId = certificate.Id,
            NotAfter = certificate.NotAfter,
            Warnings = warnings
        });
    }
}
