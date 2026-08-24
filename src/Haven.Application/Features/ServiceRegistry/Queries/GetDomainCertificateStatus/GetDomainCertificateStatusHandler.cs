using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetDomainCertificateStatus;

public sealed class GetDomainCertificateStatusHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ITraefikApiClient traefikApiClient)
    : IQueryHandler<GetDomainCertificateStatusQuery, DomainCertificateStatusDto>
{
    public async ValueTask<Result<DomainCertificateStatusDto>> Handle(GetDomainCertificateStatusQuery query, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetByDomainIdAsync(query.DomainId, cancellationToken);
        var domain = entry?.Domains.FirstOrDefault(d => d.Id == query.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), query.DomainId);

        if (domain.TlsMode == TlsMode.None)
            return new DomainCertificateStatusDto { TlsMode = TlsMode.None };

        if (domain.TlsMode == TlsMode.Custom)
            return BuildCustomStatus(domain);

        return await BuildAcmeStatusAsync(domain, cancellationToken);
    }

    private static DomainCertificateStatusDto BuildCustomStatus(ServiceRegistryDomain domain)
    {
        var certificate = domain.Certificate;
        if (certificate is null)
        {
            return new DomainCertificateStatusDto
            {
                TlsMode = TlsMode.Custom,
                SourceOfTruth = "Database"
            };
        }

        var now = DateTimeOffset.UtcNow;
        return new DomainCertificateStatusDto
        {
            TlsMode = TlsMode.Custom,
            SourceOfTruth = "Database",
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            SubjectCommonName = certificate.SubjectCommonName,
            IsExpired = certificate.IsExpired,
            DaysUntilExpiry = (int)(certificate.NotAfter - now).TotalDays,
            HostnameMismatch = !certificate.MatchesHostname(domain.Hostname)
        };
    }

    private async Task<DomainCertificateStatusDto> BuildAcmeStatusAsync(ServiceRegistryDomain domain, CancellationToken cancellationToken)
    {
        var routerResult = await traefikApiClient.GetRouterInfoAsync(domain.SecureRouterName, cancellationToken);
        if (routerResult.IsFailure)
        {
            return new DomainCertificateStatusDto
            {
                TlsMode = TlsMode.Acme,
                SourceOfTruth = "TraefikApi",
                TraefikReachable = false
            };
        }

        return new DomainCertificateStatusDto
        {
            TlsMode = TlsMode.Acme,
            SourceOfTruth = "TraefikApi",
            TraefikReachable = true,
            RouterStatus = routerResult.Value.Status,
            Errors = routerResult.Value.Errors
        };
    }
}
