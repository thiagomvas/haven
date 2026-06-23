using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Queries.GetTelemetry;

public sealed class GetTelemetryHandler(IOptionsMonitor<TelemetryOptions> options)
    : IQueryHandler<GetTelemetryQuery, TelemetryOptions>
{
    public ValueTask<Result<TelemetryOptions>> Handle(GetTelemetryQuery request, CancellationToken ct)
        => ValueTask.FromResult(Result<TelemetryOptions>.Success(options.CurrentValue));
}
