using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.HealthChecks.Queries.GetServiceHealthChecksQuery;

public class GetServiceHealthChecksValidator : AbstractValidator<GetServiceHealthChecksQuery>
{
    public GetServiceHealthChecksValidator()
    {
        RuleFor(x => x.ProjectId).ValidId();
        RuleFor(x => x.EnvironmentId).ValidId();
        RuleFor(x => x.ServiceId).ValidId();
    }
}
