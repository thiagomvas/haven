using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(string email, string password);
    Task<Result<AuthResponse>> RegisterAsync(string name, string email, string password);
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken);
    Task<Result<bool>> SetPasswordAsync(Guid userId, string newPassword);
}