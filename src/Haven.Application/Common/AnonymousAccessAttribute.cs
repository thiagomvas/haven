namespace Haven.Application.Common;

/// <summary>
/// Marks a command/query that can be sent without an authenticated caller (login, first-time setup,
/// accepting an invite...). Use sparingly: every message must declare how it is protected, and this is
/// the explicit "not protected" choice.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AnonymousAccessAttribute : Attribute { }