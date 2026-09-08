namespace Haven.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void OnAfterSave(Action action);

    /// <summary>
    /// Refreshes <paramref name="entity"/>'s current values from the database, discarding any
    /// unsaved in-memory changes. Used to pick up concurrent updates made by other units of work
    /// (e.g. a reactive Docker event handler) before this unit of work commits its own changes.
    /// </summary>
    Task ReloadAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class;

    /// <summary>
    /// Stops tracking <paramref name="entity"/> without touching the database. Used to discard an
    /// uncommitted entity after a concurrent write elsewhere wins a race to persist the "same" row
    /// first (e.g. two deployments of the same container racing to register it), so the entity can
    /// be re-fetched from the database instead of being retried as a duplicate insert.
    /// </summary>
    void Detach<TEntity>(TEntity entity) where TEntity : class;
}