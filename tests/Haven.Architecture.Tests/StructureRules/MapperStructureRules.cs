using System.Reflection;
using System.Runtime.CompilerServices;

using Riok.Mapperly.Abstractions;

namespace Haven.Architecture.Tests.StructureRules;

[TestFixture]
[Category("Architecture")]
public class MapperStructureRules
{
    [Test]
    public void Mappers_ShouldBeStatic()
    {
        var failures = GetMapperTypes()
            .Where(type => !(type.IsAbstract && type.IsSealed))
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            failures.Count == 0,
            $"The following mapper classes are not static: {string.Join(", ", failures)}"
        );
    }

    [Test]
    public void MapperPublicMethods_ShouldBeExtensionMethods()
    {
        var failures = GetMapperTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => !method.IsDefined(typeof(ExtensionAttribute), inherit: false))
                .Select(method => $"{type.Name}.{method.Name}"))
            .ToList();

        Assert.True(
            failures.Count == 0,
            $"The following mapper public methods are not extension methods: {string.Join(", ", failures)}"
        );
    }

    private static IEnumerable<Type> GetMapperTypes()
    {
        return Assemblies.Application.GetTypes()
            .Where(type => type.IsClass)
            .Where(type => type.IsDefined(typeof(MapperAttribute), inherit: false));
    }
}