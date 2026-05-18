using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.FeatureFlags.Queries.GetServiceFeatureFlags;

public class GetServiceFeatureFlagsQuery : PagedQuery<FeatureFlagDto>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set;}
    public Guid ServiceId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 100;
}