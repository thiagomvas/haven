using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.SslCertificates.Queries.GetSslCertificates;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetSslCertificatesQuery : IQuery<List<SslCertificateDto>>;
