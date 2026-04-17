using Haven.Domain.Entities;

namespace Haven.Domain.Events;

public interface IEntityCreatedEvent
{
    Entity CreatedEntity { get; }
}
