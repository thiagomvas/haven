using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(string email, string password);
    Task<Result<AuthResponse>> RegisterAsync(string name, string email, string password);
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken);
    Task<Result<bool>> SetPasswordAsync(Guid userId, string newPassword);
    Task<Result> LogoutAsync(Guid sessionId);
    Task<Result<Guid>> CreateUserAsync(string email, bool isAdmin = false);
    Task<Result<InviteTokenResult>> CreateInviteTokenAsync(Guid userId);
    Task<Result> RevokeInviteTokensForUserAsync(Guid userId);
    Task<Result<AuthResponse>> AcceptInviteAsync(string rawToken, string name, string password);
}