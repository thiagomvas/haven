namespace Haven.Application.Common.Contracts;

public class MeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool RequirePasswordChange { get; set; }
    public bool IsAdmin { get; set; }
    public string[] Permissions { get; set; }
}