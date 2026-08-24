using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Queries.GetTraefikDashboardAuth;

public sealed class GetTraefikDashboardAuthHandler(IOptionsMonitor<TraefikOptions> options)
    : IQueryHandler<GetTraefikDashboardAuthQuery, TraefikDashboardAuthDto>
{
    public ValueTask<Result<TraefikDashboardAuthDto>> Handle(GetTraefikDashboardAuthQuery request, CancellationToken ct)
    {
        var current = options.CurrentValue;
        return ValueTask.FromResult(Result<TraefikDashboardAuthDto>.Success(new TraefikDashboardAuthDto
        {
            Enabled = current.DashboardAuthPasswordHash is not null,
            Username = current.DashboardAuthUsername
        }));
    }
}
