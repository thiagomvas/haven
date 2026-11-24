using System.Reflection;

using Haven.Application.Common;

using Shouldly;

namespace Haven.Architecture.Tests.StructureRules;

[TestFixture]
[Category("Architecture")]
public class AccessControlRules
{
    private static IEnumerable<Type> MessageTypes() =>
        new[] { Assemblies.Application, Assemblies.Infrastructure }
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(Mediator.IMessage).IsAssignableFrom(t)
                        && !typeof(Mediator.INotification).IsAssignableFrom(t));

    private static int AccessRuleCount(Type t) =>
        (t.IsDefined(typeof(AdminOnlyAttribute), false) ? 1 : 0)
        + (t.IsDefined(typeof(RequirePermissionAttribute), false) ? 1 : 0)
        + (t.IsDefined(typeof(AuthenticatedOnlyAttribute), false) ? 1 : 0)
        + (t.IsDefined(typeof(AnonymousAccessAttribute), false) ? 1 : 0);

    [Test]
    public void EveryMessage_ShouldDeclareAccessRule()
    {
        var missing = MessageTypes().Where(t => AccessRuleCount(t) == 0).Select(t => t.FullName).Order().ToList();

        missing.ShouldBeEmpty("Every command/query must declare [RequirePermission], [AdminOnly], [AuthenticatedOnly] or [AnonymousAccess] " +
                              "(PermissionBehavior fails closed otherwise):\n" + string.Join("\n", missing));
    }

    [Test]
    public void EveryMessage_ShouldDeclareExactlyOneKindOfAccessRule()
    {
        var conflicting = MessageTypes().Where(t => AccessRuleCount(t) > 1).Select(t => t.FullName).Order().ToList();

        conflicting.ShouldBeEmpty("Combining access rules is ambiguous:\n" + string.Join("\n", conflicting));
    }
}