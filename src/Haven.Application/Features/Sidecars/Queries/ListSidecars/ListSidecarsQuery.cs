using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Queries.ListSidecars;

[RequirePermission(Permissions.Sidecars.Read)]
public sealed class ListSidecarsQuery : IQuery<IReadOnlyList<SidecarDto>>;
