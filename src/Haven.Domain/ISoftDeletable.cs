namespace Haven.Domain;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set;  }
}
