namespace Haven.Application.Configuration;

public enum SetupStage
{
    NotStarted = 0,
    InstanceConfigured = 1,
    SuperUserCreated = 2,
    Completed = 3,
}

public class SetupOptions
{
    public const string SectionName = "setup";
    public SetupStage Stage { get; set; } = SetupStage.NotStarted;
}