using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Haven.Infrastructure.Services;

public class ServiceRegistry(
    IServiceRegistryEntryRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<ServiceRegistry> logger) : IServiceRegistry
{
    public async Task<ServiceRegistryEntry> EnsureServiceRegisteredAsync(Guid serviceId, CancellationToken ct = default)
    {
        var existing = await GetForServiceAsync(serviceId, ct);
        if (existing is not null) return existing;

        var entry = ServiceRegistryEntry.Create(serviceId);
        await repository.InsertAsync(entry, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_service_registry_service_id"))
        {
            unitOfWork.Detach(entry);
            return (await GetForServiceAsync(serviceId, ct))!;
        }

        logger.LogInformation("Registered new service with ID {ServiceId} in the service registry", serviceId);
        return entry;
    }

    public async Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await repository.GetForServiceAsync(serviceId, ct);
    }

    public async Task<ServiceRegistryEntry> EnsureSidecarRegisteredAsync(Guid sidecarId, CancellationToken ct = default)
    {
        var existing = await GetForSidecarAsync(sidecarId, ct);
        if (existing is not null) return existing;

        var entry = ServiceRegistryEntry.CreateForSidecar(sidecarId);
        await repository.InsertAsync(entry, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_service_registry_sidecar_id"))
        {
            unitOfWork.Detach(entry);
            return (await GetForSidecarAsync(sidecarId, ct))!;
        }

        logger.LogInformation("Registered new sidecar with ID {SidecarId} in the service registry", sidecarId);
        return entry;
    }

    public async Task<ServiceRegistryEntry?> GetForSidecarAsync(Guid sidecarId, CancellationToken ct = default)
    {
        return await repository.GetForSidecarAsync(sidecarId, ct);
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string constraintName) =>
        ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } pgEx && pgEx.ConstraintName == constraintName;
}