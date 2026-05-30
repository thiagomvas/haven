namespace Haven.Domain.Exceptions;

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : HavenException(message);
