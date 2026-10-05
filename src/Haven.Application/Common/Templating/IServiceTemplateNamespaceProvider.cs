namespace Haven.Application.Common.Templating;

/// <summary>
/// Builds the template namespaces (<c>env</c>, <c>secrets</c>, <c>runtime</c>, ...) available for a service,
/// for use with <see cref="TemplateExpressionResolver"/>.
/// </summary>
public interface IServiceTemplateNamespaceProvider
{
    /// <summary>
    /// Builds the template namespaces for the given service.
    /// </summary>
    /// <param name="serviceId">The identifier of the service to build namespaces for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The namespaces, or <c>null</c> if the service does not exist.</returns>
    Task<IReadOnlyDictionary<string, TemplateNamespaceResolver>?> BuildAsync(Guid serviceId,
        CancellationToken cancellationToken = default);
}