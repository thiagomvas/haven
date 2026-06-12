using System.Reflection;

using Mediator;

namespace Haven.Domain.Events;

public abstract record DomainEvent : INotification
{
    private static readonly Dictionary<Type, string> I18NKeyCache = new();
    private static readonly Lazy<IReadOnlyList<Type>> RegisteredEventTypes =
        new(InitializeEventTypes);

    private const string NamespaceTrim = "Haven.Domain.Events.";

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

    private static IReadOnlyList<Type> InitializeEventTypes()
    {
        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => p.IsAssignableTo(typeof(DomainEvent)) && !p.IsAbstract)
            .ToList();

        foreach (var eventType in eventTypes)
        {
            var fullName = eventType.FullName?.ToLowerInvariant() ?? string.Empty;
            var key = fullName.StartsWith(NamespaceTrim)
                ? fullName[NamespaceTrim.Length..]
                : fullName;
            I18NKeyCache[eventType] = key;
        }

        return eventTypes.AsReadOnly();
    }

    public abstract string ToMessage();
}