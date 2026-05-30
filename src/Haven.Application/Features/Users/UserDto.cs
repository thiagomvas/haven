namespace Haven.Application.Features.Users;

public record UserDto(Guid Id, string Name, string Email, bool IsAdmin, bool RequirePasswordChange);
