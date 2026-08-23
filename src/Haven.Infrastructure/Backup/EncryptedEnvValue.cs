using Haven.Application.Common.Interfaces;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Backup;

/// <summary>
/// Environment variables are where secrets typically live, and are encrypted at rest in the DB via
/// EncryptedValueConverter - but manifests are plain files that end up committed to git, so every
/// value written to a manifest env-var side-car must be encrypted the same way. The "enc:v1:" prefix
/// keeps encrypted lines visually distinguishable from plaintext, in case the format ever needs to
/// tell the two apart.
/// </summary>
public static class EncryptedEnvValue
{
    private const string Prefix = "enc:v1:";

    public static IReadOnlyList<EnvironmentVariables> EncryptAll(
        IReadOnlyList<EnvironmentVariables> variables, IEncryptionService encryptionService)
        => variables
            .Select(v => new EnvironmentVariables
            {
                Key = v.Key,
                Value = v.Value is null ? null : Prefix + encryptionService.Encrypt(v.Value),
                ParentId = v.ParentId,
                ParentType = v.ParentType
            })
            .ToList();

    public static void DecryptInPlace(List<EnvironmentVariables> variables, IEncryptionService encryptionService)
    {
        foreach (var variable in variables)
        {
            if (variable.Value is not null && variable.Value.StartsWith(Prefix, StringComparison.Ordinal))
                variable.Value = encryptionService.Decrypt(variable.Value[Prefix.Length..]);
        }
    }
}