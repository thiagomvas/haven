namespace Haven.Domain.Exceptions;

public sealed class ForbiddenException : HavenException
{
    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ForbiddenException() : base("You do not have permission to perform this action.")
    {
    }
    
    public ForbiddenException(string message) : base(message)
    {
    }
}