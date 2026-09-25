using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Deployment;

public class SecretVariableService(HavenDbContext db, IEncryptionService encryptionService) : ISecretVariableService
{
    public async Task<IEnumerable<EnvironmentVariables>> GetSecretsAsEnvironmentVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var secrets = await db.Secrets
            .AsNoTracking()
            .Where(s => s.ParentId == serviceId && s.ParentType == EnvironmentVariableParentType.Service)
            .Include(secretVariable => secretVariable.Value)
            .ToListAsync(cancellationToken);
        
        var decrypted = secrets.Select(s => new EnvironmentVariables
        {
            Key = s.Key,
            Value = encryptionService.Decrypt(s.Value ?? string.Empty)
        });

        return decrypted;
    }
}