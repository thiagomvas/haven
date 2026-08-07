using Haven.Domain.Enums;

namespace Haven.Domain.Entities;

public class EnvironmentVariables : Entity
{
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }

    public string Key { get; set; }
    public string? Value { get; set; }

    public override string ToString()
    {
        return $"{Key}={Value}";
    }
}