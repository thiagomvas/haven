using FluentValidation;

using Haven.Application.Common;

namespace Haven.Application.Features.Users.Commands.SetUserPermissions;

public sealed class SetUserPermissionsValidator : AbstractValidator<SetUserPermissionsCommand>
{

    public SetUserPermissionsValidator()
    {
        RuleForEach(x => x.Permissions)
            .Must(p => Permissions.All.Contains(p))
            .WithMessage("'{PropertyValue}' is not a valid permission.");
    }
}