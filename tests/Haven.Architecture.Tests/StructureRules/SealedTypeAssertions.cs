using System.Reflection;

namespace Haven.Architecture.Tests.StructureRules;

public static class SealedTypeAssertions
{
    public static void AssertAllSealed(Assembly assembly, Type interfaceType)
    {
        var failures = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => ImplementsInterface(type, interfaceType))
            .Where(type => !type.IsSealed)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            failures.Count == 0,
            $"The following classes implementing {interfaceType.Name} are not sealed: {string.Join(", ", failures)}"
        );
    }

    private static bool ImplementsInterface(Type type, Type interfaceType)
    {
        return type.GetInterfaces().Any(i =>
            i == interfaceType
            || (interfaceType.IsGenericTypeDefinition && i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType));
    }
}