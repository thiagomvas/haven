namespace Haven.Domain;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
    bool IsDeleted => DeletedAt.HasValue;
    void MarkDeleted();
}
