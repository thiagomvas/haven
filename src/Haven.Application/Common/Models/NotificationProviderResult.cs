namespace Haven.Application.Common.Models;

public sealed record NotificationProviderResult(bool Success, string SentPayload, string? Response, string? ErrorMessage);