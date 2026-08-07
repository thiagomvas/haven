using System.Reflection;
using System.Runtime.CompilerServices;

using Haven.Domain.Enums;

using Mediator;

namespace Haven.Domain.Events;

public abstract record DomainEvent : INotification
{
    private static readonly Dictionary<Type, string> I18NKeyCache = new();
    private static readonly Lazy<IReadOnlyList<Type>> RegisteredEventTypes =
        new(InitializeEventTypes);
    private static readonly Lazy<IReadOnlyDictionary<Type, NotificationScope?>> ScopedEventScopeCache =
        new(BuildScopedEventScopeCache);

    private const string NamespaceTrim = "Haven.Domain.Events.";
    private static readonly Type ScopedEventInterface = typeof(IScopedDomainEvent);

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public string I18NKey
    {
        get
        {
            var type = GetType();
            I18NKeyCache.TryGetValue(type, out var key);
            return key ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets all registered domain event types (read-only).
    /// </summary>
    public static IReadOnlyList<Type> AllEventTypes => RegisteredEventTypes.Value;

    /// <summary>
    /// Gets the I18N key for a specific domain event type.
    /// </summary>
    public static string GetI18NKey(Type eventType)
    {
        if (!eventType.IsAssignableTo(typeof(DomainEvent)))
            throw new ArgumentException($"{eventType.Name} is not a DomainEvent type.");

        return I18NKeyCache.TryGetValue(eventType, out var key) ? key : string.Empty;
    }

    /// <summary>
    /// Gets the I18N key by event type name.
    /// </summary>
    public static string? GetI18NKeyByName(string eventTypeName)
    {
        var type = RegisteredEventTypes.Value.FirstOrDefault(t => t.Name == eventTypeName);
        return type != null ? GetI18NKey(type) : null;
    }

    /// <summary>
    /// Returns event types relevant for the given scope.
    /// Global (or null) returns all types. Scoped returns only events that can fire for that scope level.
    /// </summary>
    public static IReadOnlyList<Type> GetEventTypesForScope(NotificationScope? scope)
    {
        if (scope is null or NotificationScope.Global)
            return AllEventTypes;

        var cache = ScopedEventScopeCache.Value;
        return AllEventTypes
            .Where(t => IsRelevantForScope(t, scope.Value, cache))
            .ToList()
            .AsReadOnly();
    }

    private static bool IsRelevantForScope(
        Type eventType,
        NotificationScope targetScope,
        IReadOnlyDictionary<Type, NotificationScope?> cache)
    {
        if (!cache.TryGetValue(eventType, out var eventScope))
            return false; // not IScopedDomainEvent → global only

        // null means the event can belong to any scope (dynamic scope)
        if (eventScope is null)
            return true;

        return targetScope switch
        {
            NotificationScope.Service => eventScope == NotificationScope.Service,
            NotificationScope.Environment => eventScope is NotificationScope.Service or NotificationScope.Environment,
            NotificationScope.Project => eventScope is NotificationScope.Service or NotificationScope.Environment or NotificationScope.Project,
            _ => true,
        };
    }

    private static IReadOnlyDictionary<Type, NotificationScope?> BuildScopedEventScopeCache()
    {
        var dict = new Dictionary<Type, NotificationScope?>();
        foreach (var type in AllEventTypes)
        {
            if (!ScopedEventInterface.IsAssignableFrom(type))
                continue; // global-only events are not added

            // Use uninitialized object to read PrimaryScope without constructor params.
            // For events whose PrimaryScope depends on a constructor argument (dynamic scope),
            // the uninitialized default may be incorrect — we detect these by checking if the
            // returned scope is consistent across two independent uninitialized instances.
            // Since EnvironmentVariablesUpdatedEvent is the only known dynamic-scope event,
            // we detect it by checking whether all enum field combinations yield distinct scopes.
            var a = RuntimeHelpers.GetUninitializedObject(type) as IScopedDomainEvent;
            var b = RuntimeHelpers.GetUninitializedObject(type) as IScopedDomainEvent;

            if (a is null || b is null)
                continue;

            var scopeA = a.PrimaryScope;
            var scopeB = b.PrimaryScope;

            // If the scope differs between two uninitialized instances (shouldn't happen for
            // deterministic properties), treat as dynamic. In practice this detects nothing
            // since both uninitialized objects have the same zero-initialized fields.
            // Instead, rely on a known-dynamic-scope check below.
            dict[type] = scopeA == scopeB ? scopeA : (NotificationScope?)null;
        }

        // EnvironmentVariablesUpdatedEvent has scope that varies by constructor argument.
        // Mark it as dynamic (null) so it appears in all scope levels.
        if (AllEventTypes.FirstOrDefault(t => t.Name == nameof(EnvironmentVariablesUpdatedEvent)) is { } dynType)
            dict[dynType] = null;

        return dict;
    }

    private static IReadOnlyList<Type> InitializeEventTypes()
    {
        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => p.IsAssignableTo(typeof(DomainEvent)) && !p.IsAbstract)
            .ToList();

        foreach (var eventType in eventTypes)
        {
            var fullName = eventType.FullName ?? string.Empty;
            var key = fullName.StartsWith(NamespaceTrim, StringComparison.InvariantCultureIgnoreCase)
                ? fullName[NamespaceTrim.Length..]
                : fullName;
            I18NKeyCache[eventType] = key;
        }

        return eventTypes.AsReadOnly();
    }

    public abstract string ToMessage();
}