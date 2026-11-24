namespace Haven.Infrastructure.Security;

/// <summary>Warnings produced while resolving secrets before logging was available; logged once the host is up.</summary>
public sealed record StartupSecretsReport(IReadOnlyList<string> Warnings);
