using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;

public sealed class AttachDomainCertificateHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ISslCertificateRepository sslCertificateRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<AttachDomainCertificateCommand, AttachDomainCertificateResult>
{
    public async ValueTask<Result<AttachDomainCertificateResult>> Handle(AttachDomainCertificateCommand command, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetByDomainIdAsync(command.DomainId, cancellationToken);
        var domain = entry?.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        if (domain.TlsMode != TlsMode.Custom)
            return Error.InvalidOperation("The domain's TLS mode must be 'Custom' before a certificate can be attached.");

        var certificate = await sslCertificateRepository.GetByIdAsync(command.CertificateId, cancellationToken);
        if (certificate is null)
            return Error.NotFoundFor(nameof(SslCertificate), command.CertificateId);

        domain.SslCertificateId = certificate.Id;
        domain.Certificate = certificate;

        var writeResult = await traefikDynamicConfigWriter.WriteDomainCertificateAsync(
            domain.Id, certificate.CertificatePem, certificate.PrivateKeyPem, cancellationToken);
        if (writeResult.IsFailure)
            return writeResult.Error;

        var warnings = new List<string>();
        if (certificate.IsExpired)
            warnings.Add("The certificate has already expired.");
        if (!certificate.MatchesHostname(domain.Hostname))
            warnings.Add($"The certificate's subject/SANs do not include '{domain.Hostname}'.");

        return Result<AttachDomainCertificateResult>.Success(new AttachDomainCertificateResult
        {
            CertificateId = certificate.Id,
            NotAfter = certificate.NotAfter,
            Warnings = warnings
        });
    }
}
