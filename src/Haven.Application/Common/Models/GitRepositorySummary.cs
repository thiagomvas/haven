namespace Haven.Application.Common.Models;

public sealed record GitRepositorySummary(string Name, string FullName, string CloneUrl, bool IsPrivate);
