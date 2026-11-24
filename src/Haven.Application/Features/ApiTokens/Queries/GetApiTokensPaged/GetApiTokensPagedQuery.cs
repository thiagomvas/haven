using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ApiTokens.Queries.GetApiTokensPaged;

[AuthenticatedOnly]
public class GetApiTokensPagedQuery : PagedQuery<ApiTokenResult>
{

}