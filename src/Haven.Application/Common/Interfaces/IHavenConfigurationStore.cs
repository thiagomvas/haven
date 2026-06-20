namespace Haven.Application.Common.Interfaces;

public interface IHavenConfigurationStore
{
    T GetCurrentValue<T>(string category) where T : class, new();
    void Invalidate(string category);
    IDisposable? RegisterOnChange<T>(string category, Action<T, string?> listener) where T : class, new();
}