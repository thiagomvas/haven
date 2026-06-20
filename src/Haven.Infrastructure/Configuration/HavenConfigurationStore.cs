using System.Collections.Concurrent;
using System.Text.Json;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Haven.Infrastructure.Configuration;

public sealed class HavenConfigurationStore(IServiceScopeFactory scopeFactory) : IHavenConfigurationStore
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _changeTokenSources = new();
    private readonly object _lockObj = new();

    public T GetCurrentValue<T>(string category) where T : class, new()
    {
        if (_cache.TryGetValue(category, out var cached) && cached is T value)
            return value;

        var loaded = LoadAsync<T>(category).GetAwaiter().GetResult();
        _cache[category] = loaded;
        return loaded;
    }

    private async Task<T> LoadAsync<T>(string category) where T : class, new()
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IHavenSettingRepository>();
        var json = await repo.GetAsync(category, CancellationToken.None);

        if (json is null)
            return new T();

        return JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    public void Invalidate(string category)
    {
        lock (_lockObj)
        {
            _cache.TryRemove(category, out _);
            TriggerChangeToken(category);
        }
    }

    private void TriggerChangeToken(string category)
    {
        if (_changeTokenSources.TryRemove(category, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
    }

    public IChangeToken GetChangeToken(string category)
    {
        var cts = _changeTokenSources.GetOrAdd(category, _ => new CancellationTokenSource());
        return new CancellationChangeToken(cts.Token);
    }

    public IDisposable? RegisterOnChange<T>(string category, Action<T, string?> listener) where T : class, new() =>
        new ChangeRegistration<T>(this, category, listener).Start();

    private sealed class ChangeRegistration<T>(
        HavenConfigurationStore store,
        string category,
        Action<T, string?> listener) : IDisposable where T : class, new()
    {
        private IDisposable? _tokenRegistration;
        private bool _disposed;

        public ChangeRegistration<T> Start() { Subscribe(); return this; }

        private void Subscribe()
        {
            if (_disposed) return;
            var token = store.GetChangeToken(category);
            _tokenRegistration = token.RegisterChangeCallback(_ => _ = OnChangedAsync(), null);
        }

        private async Task OnChangedAsync()
        {
            if (_disposed) return;
            var newValue = await store.LoadAsync<T>(category);
            listener(newValue, null);
            Subscribe();
        }

        public void Dispose()
        {
            _disposed = true;
            _tokenRegistration?.Dispose();
        }
    }
}