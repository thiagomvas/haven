using System.Reflection;

namespace Haven.Architecture.Tests.NamingConventions;

public static class HandlerNamingAssertions
{
    public static void HandlersShouldStartWithTheirRequestNames(
        Assembly assembly,
        Type[] openHandlerInterfaces,
        string requestSuffix)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => new
            {
                Type = type,
                RequestType = type.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Where(i => openHandlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault()
            })
            .Where(x => x.RequestType is not null)
            .ToList();

        var failures = handlerTypes
            .Where(x => !x.Type.Name.StartsWith(x.RequestType!.Name.Replace(requestSuffix, string.Empty), StringComparison.Ordinal))
            .Select(x => $"{x.Type.Name} (expected to start with '{x.RequestType!.Name.Replace(requestSuffix, string.Empty)}')")
            .ToList();

        Assert.True(
            failures.Count == 0,
            $"The following handlers do not start with their {requestSuffix.ToLowerInvariant()}'s name: {string.Join(", ", failures)}"
        );
    }
}
