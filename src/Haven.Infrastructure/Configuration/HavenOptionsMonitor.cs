namespace Haven.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

public sealed class HavenOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
{
    private readonly HavenConfigurationStore _store;
    private readonly string _sectionName;

    public HavenOptionsMonitor(HavenConfigurationStore store, string sectionName)
    {
        _store = store;
        _sectionName = sectionName;
    }

    public T CurrentValue => _store.GetCurrentValue<T>(_sectionName);

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) =>
        _store.RegisterOnChange(_sectionName, listener);
}
