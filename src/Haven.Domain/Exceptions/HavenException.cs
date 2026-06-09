namespace Haven.Domain.Exceptions;

public abstract class HavenException : Exception
{
    protected HavenException(string message) : base(message)
    {
    }

    protected HavenException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public HavenException() : base()
    {
    }
}