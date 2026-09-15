using System.Reflection;

using Haven.Application;
using Haven.Domain;
using Haven.Infrastructure;

namespace Haven.Architecture.Tests;

public static class Assemblies
{
    public static Assembly Domain => typeof(DomainAssemblyInfo).Assembly;
    public static Assembly Application => typeof(ApplicationAssemblyInfo).Assembly;
    public static Assembly Infrastructure => typeof(InfrastructureAssemblyInfo).Assembly;
    
}