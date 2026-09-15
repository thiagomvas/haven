using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Haven.Domain.Tests")]
[assembly: InternalsVisibleTo("Haven.Application.Tests")]
[assembly: InternalsVisibleTo("Haven.Integration.Tests")]
[assembly: InternalsVisibleTo("Haven.Architecture.Tests")]

namespace Haven.Domain;

internal sealed class DomainAssemblyInfo;