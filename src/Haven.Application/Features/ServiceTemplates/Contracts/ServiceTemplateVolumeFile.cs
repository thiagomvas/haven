namespace Haven.Application.Features.ServiceTemplates.Contracts;

/// <summary>
/// A file to seed into a <see cref="ServiceTemplateVolumeType.Managed"/> volume the first time
/// the service is created from the template. <see cref="Content"/> may reference
/// <c>${{ inputs.* }}</c> placeholders, resolved the same way as other template values.
/// </summary>
public class ServiceTemplateVolumeFile
{
    public string Path { get; set; } = null!;
    public string Content { get; set; } = null!;
}