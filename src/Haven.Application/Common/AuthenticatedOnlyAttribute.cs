namespace Haven.Application.Common;

/// <summary>
/// Marks a command/query that any authenticated caller may send, regardless of permissions
/// (e.g. reading or changing their own profile). Use <see cref="RequirePermissionAttribute"/> or
/// <see cref="AdminOnlyAttribute"/> for anything that touches shared resources.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthenticatedOnlyAttribute : Attribute
{
    /// <summary>
    /// When false, API tokens are rejected and only an interactive (JWT) session may send the message.
    /// Use it for credential management: a token holds only the scopes it was issued with, so it must not
    /// be able to mint tokens, rotate or revoke other tokens, or change its owner's password.
    /// </summary>
    public bool AllowApiTokens { get; init; } = true;
}