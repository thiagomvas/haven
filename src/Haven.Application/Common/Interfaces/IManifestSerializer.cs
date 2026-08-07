using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Common.Interfaces;

public interface IManifestEntitySerializer
{
    Type EntityType { get; }
    Task WriteToAsync(object item, string basePath, CancellationToken ct = default);
}

public interface IManifestSerializer<T> : IManifestEntitySerializer
{
    Task WriteAsync(T item, CancellationToken ct = default);
    Task WriteToAsync(T item, string basePath, CancellationToken ct = default);
    Task RenameAsync(T item, string oldName, string newName, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ReadAsync(Guid parentId = default, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ReadFromAsync(string basePath, Guid parentId = default, CancellationToken ct = default);
    Task RemoveAsync(T item, CancellationToken ct = default);
    Task<string> ReadManifestAsync(T item, CancellationToken ct = default);
}