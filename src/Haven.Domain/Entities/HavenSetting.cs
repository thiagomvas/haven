namespace Haven.Domain.Entities;

public sealed class HavenSetting : Entity
{
    public string Category { get; private set; } = string.Empty;
    public string Value { get; private set; } = "{}";

    private HavenSetting() { }

    public static HavenSetting Create(string category, string value) =>
        new() { Id = Guid.CreateVersion7(), Category = category, Value = value };

    public void Update(string value) => Value = value;
}
