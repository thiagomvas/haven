using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Configuration.Commands.UpdateTelemetry;

[AdminOnly]
public sealed record UpdateTelemetryCommand(TelemetryOptions Options) : ICommand<TelemetryOptions>;
