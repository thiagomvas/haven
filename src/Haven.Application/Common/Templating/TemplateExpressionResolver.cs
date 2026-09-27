using System.Text.RegularExpressions;

namespace Haven.Application.Common.Templating;

/// <summary>
/// Looks up a single key within one namespace of a template expression. Returning <c>null</c>
/// means the key is unresolved (e.g. not yet available), and its placeholder is left untouched.
/// </summary>
public delegate string? TemplateNamespaceResolver(string key);

/// <summary>
/// Resolves <c>${{ namespace.key }}</c> placeholders (with an optional <c>| filter</c> suffix,
/// e.g. <c>${{ env.PASSWORD | urlencode }}</c>) against caller-supplied namespaces.
///
/// This is deliberately namespace-agnostic: a caller only supplies resolvers for the namespaces
/// it can resolve right now. Placeholders in namespaces that aren't supplied, or whose key isn't
/// found within a supplied namespace, are left in the output verbatim. This lets a value be
/// resolved in phases — e.g. a service template placeholder can resolve `inputs.*`/`container.*`
/// at instantiation time while leaving `env.*`/`runtime.*` for a later, read-time resolution — and
/// lets other features (e.g. cross-referencing values in an environment-variables editor) reuse
/// the same syntax and engine without depending on the service-templates feature.
/// </summary>
public static partial class TemplateExpressionResolver
{
    public static readonly IReadOnlyDictionary<string, Func<string, string>> DefaultFilters =
        new Dictionary<string, Func<string, string>>
        {
            ["urlencode"] = Uri.EscapeDataString
        };

    [GeneratedRegex(@"\$\{\{\s*([a-zA-Z0-9_]+)\.([^\s|}]+)\s*(?:\|\s*(\w+)\s*)?\}\}", RegexOptions.Compiled)]
    private static partial Regex ExpressionPattern();

    public static string Resolve(
        string input,
        IReadOnlyDictionary<string, TemplateNamespaceResolver> namespaces,
        IReadOnlyDictionary<string, Func<string, string>>? filters = null)
    {
        filters ??= DefaultFilters;

        return ExpressionPattern().Replace(input, match =>
        {
            var ns = match.Groups[1].Value;
            var key = match.Groups[2].Value;
            var filter = match.Groups[3].Success ? match.Groups[3].Value : null;

            if (!namespaces.TryGetValue(ns, out var resolve))
                return match.Value;

            var value = resolve(key);
            if (value is null)
                return match.Value;

            if (filter is not null && filters.TryGetValue(filter, out var apply))
                value = apply(value);

            return value;
        });
    }

    /// <summary>
    /// Returns the distinct keys referenced within <paramref name="namespace"/> in <paramref name="input"/>,
    /// without resolving anything. Useful for validation, e.g. checking whether a template references a
    /// particular input key before it's safe to instantiate.
    /// </summary>
    public static IReadOnlyCollection<string> FindKeys(string input, string @namespace)
    {
        return ExpressionPattern().Matches(input)
            .Where(m => m.Groups[1].Value == @namespace)
            .Select(m => m.Groups[2].Value)
            .Distinct()
            .ToList();
    }
}
