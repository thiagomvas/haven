using NetArchTest.Rules;

namespace Haven.Architecture.Tests.DependencyRules;

[TestFixture]
[Category("Architecture")]
public class LayerDependencyRules
{
    [Test]
    public void Domain_ShouldNotDependOnApplication()
    {
        AssertNoDependency(Assemblies.Domain, "Haven.Application");
    }

    [Test]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        AssertNoDependency(Assemblies.Domain, "Haven.Infrastructure");
    }

    [Test]
    public void Domain_ShouldNotDependOnPresentation()
    {
        AssertNoDependency(Assemblies.Domain, "Haven.Presentation");
    }

    [Test]
    public void Domain_ShouldNotDependOnEntityFrameworkCore()
    {
        AssertNoDependency(Assemblies.Domain, "Microsoft.EntityFrameworkCore");
    }

    [Test]
    public void Domain_ShouldNotDependOnAspNetCore()
    {
        AssertNoDependency(Assemblies.Domain, "Microsoft.AspNetCore");
    }

    [Test]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        AssertNoDependency(Assemblies.Application, "Haven.Infrastructure");
    }

    [Test]
    public void Application_ShouldNotDependOnPresentation()
    {
        AssertNoDependency(Assemblies.Application, "Haven.Presentation");
    }

    [Test]
    public void Application_ShouldNotDependOnEntityFrameworkCore()
    {
        AssertNoDependency(Assemblies.Application, "Microsoft.EntityFrameworkCore");
    }

    [Test]
    public void Application_ShouldNotDependOnAspNetCore()
    {
        AssertNoDependency(Assemblies.Application, "Microsoft.AspNetCore");
    }

    [Test]
    public void Application_ShouldNotDependOnFastEndpoints()
    {
        AssertNoDependency(Assemblies.Application, "FastEndpoints");
    }

    [Test]
    public void Infrastructure_ShouldNotDependOnPresentation()
    {
        AssertNoDependency(Assemblies.Infrastructure, "Haven.Presentation");
    }

    private static void AssertNoDependency(System.Reflection.Assembly assembly, string forbiddenNamespace)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"The following types in {assembly.GetName().Name} depend on '{forbiddenNamespace}', violating layer isolation: {string.Join(", ", result.FailingTypeNames ?? [])}"
        );
    }
}