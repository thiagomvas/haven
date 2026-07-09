using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Volumes.Queries.GetVolumesOptions;

public sealed class GetVolumesOptionsHandler(IOptionsMonitor<VolumesOptions> options)
    : IQueryHandler<GetVolumesOptionsQuery, VolumesOptions>
{
    public ValueTask<Result<VolumesOptions>> Handle(GetVolumesOptionsQuery request, CancellationToken ct)
        => ValueTask.FromResult(Result<VolumesOptions>.Success(options.CurrentValue));
}