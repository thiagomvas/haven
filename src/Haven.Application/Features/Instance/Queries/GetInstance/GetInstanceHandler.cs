using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Instance.Dtos;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Instance.Queries.GetInstance;

public sealed class GetInstanceHandler(IOptionsMonitor<InstanceOptions> instanceOptions)
    : IQueryHandler<GetInstanceQuery, InstanceDto>
{
    public ValueTask<Result<InstanceDto>> Handle(GetInstanceQuery request, CancellationToken ct)
    {
        var opts = instanceOptions.CurrentValue;
        var dto = new InstanceDto(opts.InstanceName, opts.Timezone, opts.TimeFormat);
        return ValueTask.FromResult(Result<InstanceDto>.Success(dto));
    }
}