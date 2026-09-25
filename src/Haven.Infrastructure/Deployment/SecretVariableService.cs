using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Deployment;

public class SecretVariableService(HavenDbContext db) : ISecretVariableService
{
    public async Task<IEnumerable<EnvironmentVariables>> GetSecretsAsEnvironmentVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var secrets = await db.Secrets
            .AsNoTracking()
            .Where(s => s.ParentId == serviceId && s.ParentType == EnvironmentVariableParentType.Service)
            .ToListAsync(cancellationToken);

        return secrets.Select(s => new EnvironmentVariables
        {
            Key = s.Key,
            Value = s.Value is null ? string.Empty : s.Value.Value
        });
    }
}