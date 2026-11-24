using Haven.Application.Common;
using Haven.Application.Common.Behaviors;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Exceptions;

using Mediator;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Common.Behaviours;

[Category("Unit")]
public sealed class PermissionBehaviorTests
{
    private const string Read = "projects.read";
    private const string Write = "projects.write";

    private ICurrentUserService _currentUser;
    private IPermissionRepository _permissions;
    private Guid _userId;
    private bool _nextCalled;
    private MessageHandlerDelegate<ProtectedMessage, object> _next;

    [SetUp]
    public void Setup()
    {
        _userId = Guid.NewGuid();
        _currentUser = Substitute.For<ICurrentUserService>();
        _currentUser.UserId.Returns(_userId);
        _currentUser.Scopes.Returns([]);
        _permissions = Substitute.For<IPermissionRepository>();
        _nextCalled = false;
        _next = (_, _) =>
        {
            _nextCalled = true;
            return ValueTask.FromResult(new object());
        };
    }

    private PermissionBehavior<TMessage, object> Sut<TMessage>() where TMessage : IMessage
        => new(_currentUser, _permissions);

    private MessageHandlerDelegate<TMessage, object> Next<TMessage>() where TMessage : IMessage
        => (_, _) =>
        {
            _nextCalled = true;
            return ValueTask.FromResult(new object());
        };

    private void UserHas(params string[] granted)
        => _permissions.UserHasPermissionAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => granted.Contains(call.ArgAt<string>(1)));

    private void AsApiToken(params string[] scopes)
    {
        _currentUser.IsApiToken.Returns(true);
        _currentUser.Scopes.Returns(scopes);
    }

    [Test]
    public async Task Handle_ShouldSkipChecks_WhenBackgroundContext()
    {
        _currentUser.IsBackgroundContext.Returns(true);

        await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldFailClosed_WhenMessageDeclaresNoAccessRule()
    {
        var act = async () => await Sut<UnmarkedMessage>().Handle(new UnmarkedMessage(), Next<UnmarkedMessage>(), CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        _nextCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenMessageAllowsAnonymous_AndNoUser()
    {
        _currentUser.UserId.Returns((Guid?)null);

        await Sut<AnonymousMessage>().Handle(new AnonymousMessage(), Next<AnonymousMessage>(), CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenAuthenticatedOnly_AndUserSignedIn()
    {
        await Sut<AuthenticatedMessage>().Handle(new AuthenticatedMessage(), Next<AuthenticatedMessage>(), CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenAuthenticatedOnly_AndNoUser()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var act = async () => await Sut<AuthenticatedMessage>().Handle(new AuthenticatedMessage(), Next<AuthenticatedMessage>(), CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
        _nextCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenAuthenticatedOnly_AndApiTokenAllowed()
    {
        AsApiToken();

        await Sut<AuthenticatedMessage>().Handle(new AuthenticatedMessage(), Next<AuthenticatedMessage>(), CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenInteractiveOnly_AndCallerIsApiToken()
    {
        AsApiToken(Read);

        var act = async () => await Sut<InteractiveOnlyMessage>().Handle(new InteractiveOnlyMessage(), Next<InteractiveOnlyMessage>(), CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
        _nextCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenInteractiveOnly_AndCallerIsSession()
    {
        await Sut<InteractiveOnlyMessage>().Handle(new InteractiveOnlyMessage(), Next<InteractiveOnlyMessage>(), CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenUserHasAllPermissions()
    {
        UserHas(Read, Write);

        await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenUserMissesAPermission()
    {
        UserHas(Read);

        var act = async () => await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
        _nextCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenNoUser()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var act = async () => await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenApiTokenHasScopesAndUserHasPermissions()
    {
        AsApiToken(Read, Write);
        UserHas(Read, Write);

        await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenApiTokenLacksScopeButUserHasPermission()
    {
        AsApiToken(Read);
        UserHas(Read, Write);

        var act = async () => await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
        _nextCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenApiTokenHasScopeButUserLostPermission()
    {
        AsApiToken(Read, Write);
        UserHas(Read);

        var act = async () => await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
        _nextCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldProceed_WhenApiTokenOwnerIsAdminWithoutExplicitPermissionRows()
    {
        // The repository reports admins as holding every permission.
        AsApiToken(Read, Write);
        _permissions.UserHasPermissionAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbidden_WhenApiTokenOwnerIsAdminButTokenLacksScope()
    {
        AsApiToken(Read);
        _permissions.UserHasPermissionAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var act = async () => await Sut<ProtectedMessage>().Handle(new ProtectedMessage(), _next, CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task Handle_ShouldProceedForAdminOnly_WhenUserIsAdmin()
    {
        _currentUser.IsAdmin.Returns(true);

        await Sut<AdminMessage>().Handle(new AdminMessage(), (_, _) =>
        {
            _nextCalled = true;
            return ValueTask.FromResult(new object());
        }, CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbiddenForAdminOnly_WhenUserIsNotAdmin()
    {
        _currentUser.IsAdmin.Returns(false);

        var act = async () => await Sut<AdminMessage>().Handle(new AdminMessage(), (_, _) =>
            ValueTask.FromResult(new object()), CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task Handle_ShouldProceedForAdminOnly_WhenApiTokenHasAdminScopeAndOwnerIsAdmin()
    {
        AsApiToken(Permissions.AdminScope);
        _permissions.IsUserAdminAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);

        await Sut<AdminMessage>().Handle(new AdminMessage(), (_, _) =>
        {
            _nextCalled = true;
            return ValueTask.FromResult(new object());
        }, CancellationToken.None);

        _nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldThrowForbiddenForAdminOnly_WhenApiTokenLacksAdminScope()
    {
        AsApiToken(Read, Write);
        _permissions.IsUserAdminAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await Sut<AdminMessage>().Handle(new AdminMessage(), (_, _) =>
            ValueTask.FromResult(new object()), CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task Handle_ShouldThrowForbiddenForAdminOnly_WhenApiTokenHasAdminScopeButOwnerIsNoLongerAdmin()
    {
        AsApiToken(Permissions.AdminScope);
        _permissions.IsUserAdminAsync(_userId, Arg.Any<CancellationToken>()).Returns(false);

        var act = async () => await Sut<AdminMessage>().Handle(new AdminMessage(), (_, _) =>
            ValueTask.FromResult(new object()), CancellationToken.None);

        await act.ShouldThrowAsync<ForbiddenException>();
    }

    [RequirePermission(Read)]
    [RequirePermission(Write)]
    public sealed record ProtectedMessage : IMessage;

    public sealed record UnmarkedMessage : IMessage;

    [AnonymousAccess]
    public sealed record AnonymousMessage : IMessage;

    [AuthenticatedOnly]
    public sealed record AuthenticatedMessage : IMessage;

    [AuthenticatedOnly(AllowApiTokens = false)]
    public sealed record InteractiveOnlyMessage : IMessage;

    [AdminOnly]
    public sealed record AdminMessage : IMessage;
}