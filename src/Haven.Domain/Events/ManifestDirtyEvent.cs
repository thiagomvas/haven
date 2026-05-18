namespace Haven.Domain.Events;

public record ManifestDirtyEvent : DomainEvent
{
    public override string ToMessage()
    {
        return "Manifests are dirty and needs to be regenerated.";
    }
}