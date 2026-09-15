using System.Reflection;

using FluentValidation;

using Haven.Application.Common.Messaging;

namespace Haven.Architecture.Tests.NamingConventions;

public static class NamingConventionAssertions
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

    public static void ValidatorsForCommandsOrQueriesShouldStartWithTheirRequestNames(Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => new
            {
                Type = type,
                RequestType = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault()
            })
            .Where(x => x.RequestType is not null && IsCommandOrQuery(x.RequestType!))
            .ToList();

        var failures = validatorTypes
            .Where(x => !x.Type.Name.StartsWith(GetRequestBaseName(x.RequestType!), StringComparison.Ordinal))
            .Select(x => $"{x.Type.Name} (expected to start with '{GetRequestBaseName(x.RequestType!)}')")
            .ToList();

        Assert.True(
            failures.Count == 0,
            $"The following validators do not start with their command/query's name: {string.Join(", ", failures)}"
        );
    }

    private static bool IsCommandOrQuery(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i == typeof(ICommand)
            || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
            || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)));
    }

    private static string GetRequestBaseName(Type type)
    {
        return type.Name.Replace("Command", string.Empty).Replace("Query", string.Empty);
    }
}
