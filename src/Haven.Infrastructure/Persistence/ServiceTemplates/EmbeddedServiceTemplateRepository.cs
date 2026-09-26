using System.Reflection;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.ServiceTemplates.Services;

namespace Haven.Infrastructure.Persistence.ServiceTemplates;

/// <summary>
/// Reads the built-in service templates (postgres, redis, etc.) that ship embedded in this
/// assembly under ServiceTemplates/BuiltIn/*.yaml. These are batteries-included templates
/// authored by Haven itself; there is no support for user-supplied templates yet, so a simple
/// in-memory cache loaded once from embedded resources is sufficient.
/// </summary>
public sealed class EmbeddedServiceTemplateRepository : IServiceTemplateRepository
{
    private const string ResourcePrefix = "Haven.Infrastructure.ServiceTemplates.BuiltIn.";

    private readonly ServiceTemplateSerializer _serializer = new();
    private readonly Lazy<Task<IReadOnlyList<ServiceTemplate>>> _templates;

    public EmbeddedServiceTemplateRepository()
    {
        _templates = new Lazy<Task<IReadOnlyList<ServiceTemplate>>>(LoadAllAsync);
    }

    public async Task<IReadOnlyList<ServiceTemplate>> GetAllAsync(CancellationToken cancellationToken)
        => await _templates.Value;

    public async Task<ServiceTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var templates = await _templates.Value;
        return templates.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<ServiceTemplate>> LoadAllAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".yaml", StringComparison.Ordinal));

        var templates = new List<ServiceTemplate>();
        foreach (var resourceName in resourceNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded service template resource not found: {resourceName}");

            templates.Add(await _serializer.DeserializeAsync(stream));
        }

        return templates
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
