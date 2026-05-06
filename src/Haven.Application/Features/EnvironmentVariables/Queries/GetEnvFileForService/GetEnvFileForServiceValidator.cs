using FluentValidation;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForService;

public class GetEnvFileForServiceValidator : AbstractValidator<GetEnvFileForServiceQuery>
{
    public GetEnvFileForServiceValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .NotNull();
    }
}