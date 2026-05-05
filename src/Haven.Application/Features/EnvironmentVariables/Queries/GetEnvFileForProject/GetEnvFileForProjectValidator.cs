using FluentValidation;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;

public class GetEnvFileForProjectValidator : AbstractValidator<GetEnvFileForProjectQuery>
{
    public GetEnvFileForProjectValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .NotNull();
    }
    
}