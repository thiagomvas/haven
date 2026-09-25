using Haven.Domain.Enums;

namespace Haven.Application.Features.Secrets;

public class SecretVariableDto
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool HasValue { get; set; }
}