namespace Haven.Application.Common.Messaging;

/// <summary>
/// Marks a command whose successful execution changes state that must be reflected in the
/// live manifests directory (Projects, Environments, Services, Networks, Sidecars, EnvironmentVariables).
/// <see cref="Haven.Application.Common.Behaviors.ManifestSyncTriggerBehavior{TMessage,TResponse}"/>
/// requests a debounced manifest resync after any such command succeeds, so keeping manifests in
/// sync is a compile-time-visible property of the command instead of something that silently falls
/// through the cracks when a new domain event is added without a matching write handler.
/// </summary>
public interface IMutatesManifestState;
