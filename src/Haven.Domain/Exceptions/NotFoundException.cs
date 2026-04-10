namespace Haven.Domain.Exceptions;

public class NotFoundException : HavenException
{
    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with ID {resourceId} was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
