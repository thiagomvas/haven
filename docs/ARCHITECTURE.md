# Architecture & Design Conventions

This is the reference for how Haven's backend is structured and what new code should follow. It
exists so you don't have to reverse-engineer patterns from whatever file you happen to open first,
and so reviewers can point here instead of re-explaining the same thing on every PR.

This describes where we're heading, not a claim that every file already follows it. Where the
codebase deviates, follow this doc for new code rather than copying the older pattern. See
[Coding Standards](../CONTRIBUTING.md#coding-standards) in CONTRIBUTING for how that's handled in
review.

Some of these rules aren't just written down, they're checked by `Haven.Architecture.Tests`
(NetArchTest-based) and fail CI if broken. Those are called out below.

## Layers

Clean Architecture. Dependencies only point inward:

```
Haven.Presentation.Api  ─┐
                          ├──▶ Haven.Application ──▶ Haven.Domain
Haven.Infrastructure     ─┘
```

- **`Haven.Domain`**: entities, aggregates, value objects, domain events, domain exceptions. No
  dependency on anything outside the layer except `Mediator.Abstractions` (for the `INotification`
  marker interface domain events use).
- **`Haven.Application`**: CQRS commands/queries, validators, mappers, and the interfaces
  (repositories, domain services) that Infrastructure implements. Depends only on Domain.
- **`Haven.Infrastructure`**: EF Core persistence, repository implementations, encryption, manifest
  serialization, anything that talks to the outside world. Implements Application's interfaces.
- **`Haven.Presentation.Api`**: FastEndpoints endpoints, middleware, DI composition root.

`Haven.Architecture.Tests/DependencyRules/LayerDependencyRules.cs` enforces this: Domain can't
reference Application or Infrastructure, Application can't reference Infrastructure or
Presentation. If you're importing the wrong direction, that's a sign the abstraction belongs
somewhere else, not a case to special-case, and it won't build past CI anyway.

## Feature folder anatomy

Application-layer work is vertical slices under `Features/[Entity]/[Commands|Queries]/[Name]`, one
folder per operation. Real example, `Features/Projects/Commands/CreateProject/`:

```
CreateProjectCommand.cs     # ICommand<Guid>, request shape only
CreateProjectHandler.cs     # ICommandHandler<CreateProjectCommand, Guid>, orchestration only
CreateProjectValidator.cs   # FluentValidation rules
```

`CreateProjectCommand.cs` is genuinely just a property bag:

```csharp
[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CreateProjectCommand : ICommand<Guid>, IMutatesManifestState
{
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Description { get; set; }
}
```

Query folders follow the same shape (`Query`/`Handler`, validator optional) and usually own any
response DTOs specific to that one query, e.g. `ProjectDto.cs` sits next to `GetProjectsQuery.cs`.
DTOs shared across multiple operations in a feature live at the feature root instead, see
`Features/Projects/ProjectManifestDto.cs`.

Rules of thumb:
- **Command = request shape only.** No logic. Attributes like `[RequirePermission(...)]` and marker
  interfaces like `IMutatesManifestState` go here, nothing else.
- **Handler = orchestration only.** Load the aggregate through its repository, call domain methods
  to apply the change, persist, return a `Result<T>`. Business rules live on the aggregate, not the
  handler. `UpdateProjectHandler` is a clean example: load, check a conflict, call
  `project.Update(...)`, return.
- **One handler, one job.** A handler juggling several unrelated side effects is usually a sign some
  of that belongs in a domain event handler instead (see below).
- Mutating commands implement `IMutatesManifestState` when the change needs to show up in the YAML
  manifests. Read-only queries never do.

Mappers (Mapperly `[Mapper]` partial classes) live in `Haven.Application/Mappers/[Entity]Mapper.cs`,
one static class per aggregate/entity, shared across all of that entity's features rather than
duplicated per-command. `StructureRules/MapperStructureRules.cs` checks the shape.

## Domain modeling conventions

- **Aggregates own their invariants.** Public mutation methods (`Update`, `AddEnvironment`,
  `Delete`, ...) live on the aggregate root. Nothing outside `Haven.Domain` mutates entity state
  directly. Setters are `private set` or absent.
- **Construction goes through named factory methods**, not public constructors:
  `Project.Create(...)` for a new aggregate, `Project.Reconstitute(...)` for rehydrating one from
  persistence. Both static, constructor itself private/parameterless.
- **Every meaningful mutation raises a domain event** via `Raise(new XCreatedEvent(...))`, including
  from `Create` itself. Side effects that aren't the aggregate's own invariant (notifications,
  manifest sync, cross-aggregate updates) belong in a notification handler subscribed to that event,
  not inlined into the aggregate method or the command handler.
- **Partial updates use `Optional<T>`** (`Haven.Domain.Optional<T>`), never nullable-as-sentinel.
  It's what distinguishes "not provided" from "explicitly cleared." Update methods take
  `Optional<T> field = default` and check `.HasValue` before applying, see `Project.Update` for the
  canonical shape. Only raise the event if something actually changed.
- **Value objects are immutable**, built through a static `From(...)`/factory method that validates
  and throws a domain exception (e.g. `ValidationException`) on bad input, never a public
  constructor that can produce an invalid instance. Equality is structural, via `ValueObject`'s
  `GetEqualityComponents()`.
- **Collections are exposed read-only** (`IReadOnlyList<T>` backed by a private `List<T>`). The only
  way to mutate them is through the aggregate's own methods.

## Interface placement: feature-local vs. `Common`

Default new interfaces to living **inside the feature that owns them**
(e.g. `Features/Exporting/IExportFormatWriter.cs`), not in `Common/Interfaces`. Promote to
`Common/Interfaces` only when a **second, unrelated feature** actually needs it, not just because a
feature has multiple implementations of its own contract. Docker Compose / Ansible / Kubernetes
exporters are all still the *Exporting* feature; having three implementations doesn't justify
`Common` on its own.

`Common/Interfaces` holds `IManifestSerializer<T>` because manifest sync genuinely cuts across
Projects, Environments, Services, Sidecars, and Networks, each registers its own instantiation.
That's the bar, not "this might be reused someday."

If a feature-local interface does need to be shared later, moving it to `Common` is cheap and
mechanical. Keeping something in `Common` "just in case" isn't free, it's one more thing every
contributor has to check isn't actually feature-specific.

## Naming conventions

| Kind | Convention | Example |
|---|---|---|
| Command | `[Verb][Entity]Command` | `CreateProjectCommand` |
| Query | `Get[Entity(s)][Qualifier]Query` | `GetProjectsDashboardQuery` |
| Handler | `[Command/Query name without suffix]Handler` | `CreateProjectHandler` |
| Validator | `[Command/Query name without suffix]Validator` | `CreateProjectValidator` |
| Response DTO | `[Entity][Qualifier]Dto` | `ProjectDashboardDto`, `ServiceStatisticsDto` |
| Manifest DTO | `[Entity]ManifestDto` | `ProjectManifest` |
| Domain event | `[Entity][PastTenseVerb]Event` | `ProjectCreatedEvent`, `EnvironmentVariablesUpdatedEvent` |
| Mapper | `[Entity]Mapper` (static partial, one per entity) | `ProjectMapper` |
| Repository interface | `I[Entity]Repository` | `IProjectRepository` |

All of these are enforced, not just documented, by `Haven.Architecture.Tests/NamingConventions/`
(`CommandNamingRules.cs`, `QueryNamingRules.cs`, `ValidatorNamingRules.cs`, `MapperNamingRules.cs`,
`RepositoryNamingRules.cs`). A misnamed class fails the build, so don't guess at a variant.

Other conventions:
- Commands and queries are `sealed` classes implementing `ICommand<T>` / `IQuery<T>`, not records.
  This project uses mutable property-bag requests, not the immutable-record style you'll see in
  other Mediator/MediatR codebases. Don't switch a feature to records without discussing it first,
  consistency here matters more than which style is "more correct."
- DTOs are plain `record`/`class` property bags with no behavior. Mapping logic belongs in the
  `[Entity]Mapper`, not a constructor or method on the DTO.
- Handlers and command/query classes are `sealed` unless there's a specific reason for inheritance.
  `StructureRules/SealedTypeAssertions.cs` checks that every command, query, and handler is sealed.

## Testing

See [CONTRIBUTING.md § Testing](../CONTRIBUTING.md#testing) for the testing bar expected on PRs. On
the structural side: test projects mirror the source layer they cover 1:1 (`Haven.Domain.Tests`,
`Haven.Application.Tests`, `Haven.Infrastructure.Tests`, `Haven.Integration.Tests`), and a test
file's namespace/path mirrors the source file it's testing
(`Features/Projects/Commands/CreateProject/CreateProjectHandlerTests.cs` tests
`Features/Projects/Commands/CreateProject/CreateProjectHandler.cs`).

`Haven.Architecture.Tests` runs on every PR (see CI config) and covers the rules above: layer
dependency direction, naming conventions, command/query/mapper structure, and sealed-type
enforcement. If you're unsure whether a convention here is a soft guideline or a hard rule, check
whether there's a corresponding test in that project, if there is, it's not optional.
