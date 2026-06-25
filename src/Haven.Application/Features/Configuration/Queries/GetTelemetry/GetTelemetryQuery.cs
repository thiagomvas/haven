using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Configuration.Queries.GetTelemetry;

[AdminOnly]
public sealed record GetTelemetryQuery : IQuery<TelemetryOptions>;