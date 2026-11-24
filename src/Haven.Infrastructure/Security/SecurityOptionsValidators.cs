using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Security;

/// <summary>
/// Structural validation, run on startup. Whether a placeholder value is acceptable depends on the
/// environment and is decided earlier by <see cref="StartupSecretsResolver"/>.
/// </summary>
public sealed class EncryptionOptionsValidator : IValidateOptions<EncryptionOptions>
{
    public ValidateOptionsResult Validate(string? name, EncryptionOptions options)
    {
        var error = SecretPolicy.ValidateEncryptionKeyStructure(options.Key);
        return error is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(error);
    }
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var error = SecretPolicy.ValidateJwtSecretStructure(options.Secret);
        return error is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(error);
    }
}