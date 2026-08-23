namespace Haven.Infrastructure.Backup;

/// <summary>
/// Generalizes the create/update/delete-by-key diff every manifest type needs when computing what a
/// restore would change. Adding a new manifest-tracked entity type only requires a key selector and a
/// change comparer here, instead of a hand-written Compute*Diff method per type.
/// </summary>
public static class ManifestDiffEngine
{
    public static (List<T> Created, List<T> Updated, List<T> Deleted) Compute<T, TKey>(
        IReadOnlyList<T> snapshot,
        IReadOnlyList<T> current,
        Func<T, TKey> getKey,
        Func<T, T, bool> hasChanges)
        where TKey : notnull
    {
        var snapshotByKey = snapshot.ToDictionary(getKey);
        var currentByKey = current.ToDictionary(getKey);

        var created = snapshot.Where(s => !currentByKey.ContainsKey(getKey(s))).ToList();
        var updated = snapshot.Where(s => currentByKey.TryGetValue(getKey(s), out var c) && hasChanges(s, c)).ToList();
        var deleted = current.Where(c => !snapshotByKey.ContainsKey(getKey(c))).ToList();

        return (created, updated, deleted);
    }
}