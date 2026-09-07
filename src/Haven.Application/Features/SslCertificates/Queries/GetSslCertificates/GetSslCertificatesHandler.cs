using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.SslCertificates.Queries.GetSslCertificates;

public sealed class GetSslCertificatesHandler(ISslCertificateRepository sslCertificateRepository)
    : IQueryHandler<GetSslCertificatesQuery, List<SslCertificateDto>>
{
    public async ValueTask<Result<List<SslCertificateDto>>> Handle(GetSslCertificatesQuery query, CancellationToken cancellationToken)
    {
        var certificates = await sslCertificateRepository.GetAllAsync(cancellationToken);

        var dtos = new List<SslCertificateDto>(certificates.Count);
        foreach (var certificate in certificates)
        {
            dtos.Add(new SslCertificateDto
            {
                Id = certificate.Id,
                Name = certificate.Name,
                SubjectCommonName = certificate.SubjectCommonName,
                NotBefore = certificate.NotBefore,
                NotAfter = certificate.NotAfter,
                IsExpired = certificate.IsExpired,
                AttachedDomainCount = await sslCertificateRepository.GetAttachedDomainCountAsync(certificate.Id, cancellationToken),
                CreatedAt = certificate.CreatedAt,
                UpdatedAt = certificate.UpdatedAt
            });
        }

        return Result<List<SslCertificateDto>>.Success(dtos);
    }
}