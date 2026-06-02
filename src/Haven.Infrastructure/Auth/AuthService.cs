using System.Security.Cryptography;
using System.Text;
using FastEndpoints.Security;
using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Haven.Infrastructure.Auth;

public class AuthService(HavenDbContext context, IConfiguration configuration) : IAuthService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Error.Unauthorized;

        var sessionId = Guid.NewGuid();
        var accessToken = GenerateAccessToken(user, sessionId);
        var (rawRefreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, sessionId);

        context.RefreshTokens.Add(refreshTokenEntity);
        await context.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, rawRefreshToken));
    }

    public async Task<Result<AuthResponse>> RegisterAsync(string name, string email, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = User.Create(name, email, passwordHash, isAdmin: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sessionId = Guid.NewGuid();
        var accessToken = GenerateAccessToken(user, sessionId);
        var (rawRefreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, sessionId);
        context.RefreshTokens.Add(refreshTokenEntity);
        await context.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, rawRefreshToken));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored is null || !stored.IsActive)
            return Error.Unauthorized;

        var user = await context.Users.FindAsync(stored.UserId);
        if (user is null)
            return Error.Unauthorized;

        // Rotate: revoke the consumed token and issue a fresh pair under the same session
        stored.Revoke();

        var accessToken = GenerateAccessToken(user, stored.SessionId);
        var (rawNewRefreshToken, newRefreshTokenEntity) = CreateRefreshToken(user.Id, stored.SessionId);

        context.RefreshTokens.Add(newRefreshTokenEntity);
        await context.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, rawNewRefreshToken));
    }

    public async Task<Result> LogoutAsync(Guid sessionId)
    {
        var tokens = await context.RefreshTokens
            .Where(t => t.SessionId == sessionId && !t.RevokedAt.HasValue)
            .ToListAsync();

        foreach (var token in tokens)
            token.Revoke();

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<bool>> SetPasswordAsync(Guid userId, string newPassword)
    {
        var user = await context.Users.FindAsync(userId);

        if (user is null)
            return Error.NotFoundFor(nameof(User), userId);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.RequirePasswordChange = false;
        await context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<Guid>> CreateUserAsync(string name, string email, string temporaryPassword)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        var user = User.CreatePending(name, email, passwordHash);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return Result<Guid>.Success(user.Id);
    }

    private string GenerateAccessToken(User user, Guid sessionId)
    {
        var jwt = configuration.GetSection("Jwt");
        return JwtBearer.CreateToken(o =>
        {
            o.SigningKey = jwt["Secret"]!;
            o.Issuer = jwt["Issuer"];
            o.Audience = jwt["Audience"];
            o.ExpireAt = DateTime.UtcNow.Add(AccessTokenLifetime);
            o.User.Claims.Add(("sub", user.Id.ToString()));
            o.User.Claims.Add(("email", user.Email));
            o.User.Claims.Add(("name", user.Name));
            o.User.Claims.Add(("sessionId", sessionId.ToString()));
            if (user.IsAdmin)
                o.User.Claims.Add(("role", "Admin"));
        });
    }

    private (string rawToken, RefreshToken entity) CreateRefreshToken(Guid userId, Guid sessionId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var entity = RefreshToken.Create(userId, sessionId, HashToken(rawToken), DateTime.UtcNow.Add(RefreshTokenLifetime));
        return (rawToken, entity);
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
