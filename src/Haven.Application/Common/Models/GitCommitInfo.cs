namespace Haven.Application.Common.Models;

public sealed record GitCommitInfo(string Sha, string Message, string Author, DateTimeOffset Timestamp);
