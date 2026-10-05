using Haven.Domain.Enums;

namespace Haven.Domain.ValueObjects;

public sealed record ExecActionConfig(
    IReadOnlyList<string> Command,
    string? WorkingDir,
    string? User,
    ShellType? Shell) : ActionConfig;