namespace Haven.Domain.Enums;

public enum NetworkType
{
    ProjectEnvironment,
    Shared,
    External,

    /// <summary>
    /// The single Haven control-plane network every Sidecar auto-joins. Independent of any
    /// Project/Environment, and distinct from user-created <see cref="Shared"/> networks.
    /// </summary>
    System
}