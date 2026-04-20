namespace Haven.Domain;

public enum RestartPolicy
{
    No,
    Always,
    UnlessStopped,
    OnFailure
}
