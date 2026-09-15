using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeleteService;

public sealed class DeleteServiceCommand : ICommand, IMutatesManifestState
{
    public Guid ServiceId { get; set; }
}