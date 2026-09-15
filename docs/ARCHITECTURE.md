# Architecture & Design Conventions

This document is the single reference for how Haven's backend is structured and the conventions
new code should follow. It exists so contributors (human or AI-assisted) don't have to reverse-engineer
patterns from whatever example file they happen to open first, and so reviewers have something concrete
to point to instead of re-explaining the same rules on every PR.

This is descriptive of where we're heading, not a claim that every existing file already follows it.
Where the codebase currently deviates, follow the convention here for new code rather than copying the
older pattern, see [Coding Standards](../CONTRIBUTING.md#coding-standards) in CONTRIBUTING for how that's handled in review.

## Layers

Haven follows Clean Architecture. Dependencies only point inward:

```
Haven.Presentation.Api  ─┐
                          ├──▶ Haven.Application ──▶ Haven.Domain
Haven.Infrastructure     ─┘
```

- **`Haven.Domain`**: entities, aggregates, value objects, domain events, domain exceptions. No
  dependency on anything outside the layer except `Mediator.Abstractions` (for the `INotification`
  marker interface used by domain events).
- **`Haven.Application`**: CQRS commands/queries, validators, mappers, and the interfaces
  (repositories, domain services) that Infrastructure implements. Depends only on Domain.
- **`Haven.Infrastructure`**: EF Core persistence, repository implementations, encryption,
  manifest serialization, and anything else that talks to the outside world. Implements
  Application's interfaces.
- **`Haven.Presentation.Api`**: FastEndpoints endpoints, middleware, DI composition root.

If you find yourself importing `Haven.Infrastructure` from `Haven.Application`, or `Haven.Application`
from `Haven.Domain`, that's a sign the abstraction belongs somewhere else, not a case to special-case.

## Feature folder anatomy

Application-layer work is organized as vertical slices under `Features/[Entity]/[Commands|Queries]/[Name]`,
one folder per operation. A typical command folder:

```
Features/Projects/Commands/CreateProject/
├── CreateProjectCommand.cs      # ICommand<TResult>, request shape only
├── CreateProjectHandler.cs      # ICommandHandler<TCommand, TResult>, orchestration only
└── CreateProjectValidator.cs    # FluentValidation rules (commands only, not queries)
```

Query folders follow the same shape (`Query`/`Handler`, validator optional) and typically also own
any response DTOs that are specific to that one query (e.g. `ProjectDto.cs` next to `GetProjectsQuery.cs`).
DTOs that are shared across multiple operations within the feature (e.g. a manifest DTO) live at the
feature root instead of inside one operation's folder — see `Features/Projects/ProjectManifestDto.cs`.

Rules of thumb:
- **Command = request shape only.** No logic. Attributes like `[RequirePermission(...)]` and marker
  interfaces like `IMutatesManifestState` go here.
- **Handler = orchestration only.** Load the aggregate via its repository, call domain methods to
  apply the change, persist, return a `Result<T>`. Business rules and invariants live on the
  aggregate/entity, not in the handler.
- **One handler, one job.** If a handler is coordinating multiple unrelated side effects, that's
  usually a sign some of that belongs in a domain event handler instead (see below).
- Mutating commands implement `IMutatesManifestState` when the change needs to be reflected in the
  YAML manifests; read-only queries never do.

Mappers (Mapperly-based, `[Mapper]` partial classes converting between domain types and DTOs) live in
`Haven.Application/Mappers/[Entity]Mapper.cs`, one static mapper class per aggregate/entity, shared
across all of that entity's features, not duplicated per-command.

## Domain modeling conventions

- **Aggregates own their invariants.** Public mutation methods (`Update`, `AddEnvironment`, `Delete`, …)
  live on the aggregate root; nothing outside `Haven.Domain` mutates entity state directly. Setters are
  `private set` or absent.
- **Construction goes through named factory methods**, not public constructors: `Project.Create(...)`
  for a genuinely new aggregate, `Project.Reconstitute(...)` for rehydrating one from persistence. Both
  are static; the constructor itself is private/parameterless.
- **Every meaningful mutation raises a domain event** via `Raise(new XCreatedEvent(...))`, including
  from `Create` itself. Side effects that aren't the aggregate's own invariant (notifications, manifest
  sync, cross-aggregate updates) belong in a notification handler subscribed to that event — not inlined
  into the aggregate method or the command handler.
- **Partial updates use `Optional<T>`** (`Haven.Domain.Optional<T>`), never nullable-as-sentinel. This is
  what distinguishes "field not provided" from "field explicitly cleared." Update methods take
  `Optional<T> field = default` parameters and check `.HasValue` before applying — see
  `Project.Update` for the canonical shape. Only raise the domain event if something actually changed.
- **Value objects are immutable**, constructed through a static `From(...)`/factory method that enforces
  validation and throws a domain exception (e.g. `ValidationException`) on invalid input — never a public
  constructor that can produce an invalid instance. Equality is structural, via `ValueObject`'s
  `GetEqualityComponents()`.
- **Collections are exposed read-only** (`IReadOnlyList<T>` backed by a private `List<T>`); the only way
  to mutate them is through the aggregate's own methods.

## Interface placement: feature-local vs. `Common`

Default new interfaces to living **inside the feature that owns them**
(e.g. `Features/Exporting/IExportFormatWriter.cs`), not in `Common/Interfaces`. Promote an interface to
`Common/Interfaces` only when a **second, unrelated feature** actually needs to depend on it —
not merely because a feature has multiple implementations of its own contract (e.g. Docker
Compose/Ansible/Kubernetes exporters are all still the *Exporting* feature; that alone doesn't
justify `Common`).

`Common/Interfaces` today holds contracts like `IManifestSerializer<T>` because manifest sync is
genuinely woven through many unrelated features (Projects, Environments, Services, Sidecars, Networks
each register an instantiation of it) — that's the bar to match, not "this interface might be reused
someday."

If it turns out a feature-local interface does need to be shared later, moving it to `Common` is a
cheap, mechanical change. Keeping something in `Common` "just in case" is not free — it's one more
thing every contributor has to check isn't actually feature-specific.

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

Other conventions:
- Commands and queries are `sealed` classes implementing `ICommand<T>` / `IQuery<T>`, not records,
  this project uses mutable property-bag requests, not the immutable-record style you'll see in some
  other Mediator/MediatR codebases. Don't switch a feature to records without discussing it first;
  consistency here matters more than which style is "more correct."
- DTOs are plain `record`/`class` property bags with no behavior — mapping logic belongs in the
  `[Entity]Mapper`, not in a constructor or a method on the DTO.
- Handlers and command/query classes are `sealed` unless there's a specific reason for inheritance.

## Testing

See [CONTRIBUTING.md § Testing](../CONTRIBUTING.md#testing) for the testing bar expected on PRs. On the
structural side: test projects mirror the source layer they cover 1:1
(`Haven.Domain.Tests`, `Haven.Application.Tests`, `Haven.Infrastructure.Tests`, `Haven.Integration.Tests`),
and a test file's namespace/path mirrors the source file it's testing
(`Features/Projects/Commands/CreateProject/CreateProjectHandlerTests.cs` tests
`Features/Projects/Commands/CreateProject/CreateProjectHandler.cs`).
