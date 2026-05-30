namespace Haven.Application.Common.Contracts;

public record MeResponse(Guid Id, string Name, string Email, bool RequirePasswordChange, bool IsAdmin);
