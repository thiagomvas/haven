using FluentValidation;
using Haven.Application.Common;

namespace Haven.Application.Features.Users.Commands.SetUserPermissions;

public sealed class SetUserPermissionsValidator : AbstractValidator<SetUserPermissionsCommand>
{
    private static readonly HashSet<string> KnownPermissions =
    [
        Permissions.Projects.Create,
        Permissions.Projects.Update,
        Permissions.Projects.Delete,
        Permissions.Projects.View,
        Permissions.Environments.Create,
        Permissions.Environments.Update,
        Permissions.Environments.Delete,
        Permissions.Environments.View,
        Permissions.Services.Create,
        Permissions.Services.Update,
        Permissions.Services.Delete,
        Permissions.Services.View,
        Permissions.Services.Deploy,
        Permissions.Users.Create,
        Permissions.Users.Update,
        Permissions.Users.Delete,
        Permissions.Users.View,
        Permissions.Users.ManagePermissions,
    ];

    public SetUserPermissionsValidator()
    {
        RuleForEach(x => x.Permissions)
            .Must(p => KnownPermissions.Contains(p))
            .WithMessage("'{PropertyValue}' is not a valid permission.");
    }
}
