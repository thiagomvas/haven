using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Business.Queries.FuzzySearch;

[RequirePermission(Permissions.ProjectManagement.Read)]
public class FuzzySearchQuery : IQuery<IEnumerable<FuzzySearchResult>>
{
    public string Query { get; set; }
    public int Count { get; set; } = 10;
    public string[]? Scopes { get; set; }
}