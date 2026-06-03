using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetSystemInformation;

public class GetSystemInformationHandler(ISystemService service) : IQueryHandler<GetSystemInformationQuery, SystemInformation>
{
    public async ValueTask<Result<SystemInformation>> Handle(GetSystemInformationQuery query, CancellationToken cancellationToken)
    {
        return await service.GetSystemInformationAsync(cancellationToken);
    }
}