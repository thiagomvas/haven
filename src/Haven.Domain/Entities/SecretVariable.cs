using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public class SecretVariable : Entity
{
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }
    public string Key { get; set; }
    public EncryptedValue? Value { get; set; }
}