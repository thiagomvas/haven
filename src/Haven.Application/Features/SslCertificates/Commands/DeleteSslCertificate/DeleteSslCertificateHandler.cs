using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.SslCertificates.Commands.DeleteSslCertificate;

/// <summary>
/// Deletes a library certificate outright, detaching it from every domain it's currently attached
/// to first - those domains fall back to "Custom mode, no certificate" (same as never having had one)
/// rather than being deleted themselves.
/// </summary>
public sealed class DeleteSslCertificateHandler(
    ISslCertificateRepository sslCertificateRepository,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<DeleteSslCertificateCommand>
{
    public async ValueTask<Result> Handle(DeleteSslCertificateCommand command, CancellationToken cancellationToken)
    {
        var certificate = await sslCertificateRepository.GetByIdAsync(command.CertificateId, cancellationToken);
        if (certificate is null)
            return Error.NotFoundFor(nameof(SslCertificate), command.CertificateId);

        var attachedDomains = await serviceRegistryEntryRepository.GetDomainsByCertificateIdAsync(certificate.Id, cancellationToken);
        foreach (var domain in attachedDomains)
        {
            domain.SslCertificateId = null;
            domain.Certificate = null;
            await traefikDynamicConfigWriter.RemoveDomainCertificateAsync(domain.Id, cancellationToken);
        }

        await sslCertificateRepository.RemoveAsync(certificate, cancellationToken);

        return Result.Success();
    }
}
