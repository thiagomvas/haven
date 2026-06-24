using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Backup;

public sealed class RestoreBackupHandler(
    IBackupManifestReader manifestReader,
    IManifestSerializer<Project> projectSerializer,
    HavenDbContext context,
    ILogger<RestoreBackupHandler> logger)
    : ICommandHandler<RestoreBackupCommand, RestoreBackupResult>
{
    public async ValueTask<Result<RestoreBackupResult>> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        string? tempDir = null;
        try
        {
            var sourceDir = await manifestReader.PrepareSourceDirectoryAsync(
                request.Source, request.SnapshotName, request.CommitSha, ct);

            if (request.Source == RestoreSource.Git)
                tempDir = sourceDir;

            var snapshotProjects = await projectSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotById = snapshotProjects.ToDictionary(p => p.Id);

            var currentProjects = await context.Projects.AsNoTracking().ToListAsync(ct);
            var currentById = currentProjects.ToDictionary(p => p.Id);

            var created = snapshotProjects
                .Where(p => !currentById.ContainsKey(p.Id))
                .Select(p => new ProjectRestoreItem(p.Id, p.Name))
                .ToList();

            var deleted = currentProjects
                .Where(p => !snapshotById.ContainsKey(p.Id))
                .Select(p => new ProjectRestoreItem(p.Id, p.Name))
                .ToList();

            var updated = snapshotProjects
                .Where(p => currentById.TryGetValue(p.Id, out var cur) && HasProjectChanges(p, cur))
                .Select(p => new ProjectRestoreItem(p.Id, p.Name))
                .ToList();

            if (!request.DryRun)
                await ApplyProjectChangesAsync(snapshotProjects, snapshotById, currentById, ct);

            logger.LogInformation(
                "Restore (DryRun={DryRun}): {Created} created, {Updated} updated, {Deleted} deleted projects",
                request.DryRun, created.Count, updated.Count, deleted.Count);

            return Result<RestoreBackupResult>.Success(new RestoreBackupResult
            {
                DryRun = request.DryRun,
                Projects = new EntityChangeSummary<ProjectRestoreItem>
                {
                    Created = created,
                    Updated = updated,
                    Deleted = deleted
                }
            });
        }
        finally
        {
            if (tempDir is not null && Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to clean up temporary restore directory {Dir}", tempDir);
                }
            }
        }
    }

    private async Task ApplyProjectChangesAsync(
        IReadOnlyList<Project> snapshotProjects,
        Dictionary<Guid, Project> snapshotById,
        Dictionary<Guid, Project> currentById,
        CancellationToken ct)
    {
        var existingTokens = await context.Services
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Token, ct);

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var deletedIds = currentById.Keys.Except(snapshotById.Keys).ToList();
            if (deletedIds.Count > 0)
                await context.Projects.Where(p => deletedIds.Contains(p.Id)).ExecuteDeleteAsync(ct);

            foreach (var snapshotProject in snapshotProjects)
            {
                if (!currentById.ContainsKey(snapshotProject.Id))
                {
                    context.Projects.Add(snapshotProject);
                }
                else if (HasProjectChanges(snapshotProject, currentById[snapshotProject.Id]))
                {
                    var tracked = await context.Projects.FindAsync([snapshotProject.Id], ct);
                    if (tracked is not null)
                    {
                        context.Entry(tracked).CurrentValues.SetValues(new
                        {
                            snapshotProject.Name,
                            snapshotProject.Alias,
                            snapshotProject.Description
                        });
                    }
                }
            }

            await context.SaveChangesAsync(ct);

            RestoreServiceTokens(existingTokens);
            await context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static bool HasProjectChanges(Project snapshot, Project current)
        => snapshot.Name != current.Name
           || snapshot.Alias != current.Alias
           || snapshot.Description != current.Description;

    private void RestoreServiceTokens(Dictionary<Guid, string> existingTokens)
    {
        foreach (var service in context.Services.Local)
        {
            if (existingTokens.TryGetValue(service.Id, out var token))
                service.Token = token;
        }
    }
}
