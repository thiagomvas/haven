using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetServiceComputedOutputValue;

/// <summary>
/// Returns the resolved plaintext value of a computed output. Gated behind the same permission
/// used for secret-variable CRUD, since a computed output can embed a decrypted secret — this is
/// the only place that ever sends such a value to the client.
/// </summary>
[RequirePermission(Permissions.ProjectManagement.ManageSecrets)]
public sealed class GetServiceComputedOutputValueQuery : IQuery<ComputedOutputValueDto>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ServiceId { get; init; }
    public string Key { get; init; } = default!;
}
