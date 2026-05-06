using FluentValidation;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForEnvironment;

public class GetEnvFileForEnvironmentValidator : AbstractValidator<GetEnvFileForEnvironmentQuery>
{
    public GetEnvFileForEnvironmentValidator()
    {
        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .NotNull();
    }
}