using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeleteService;

public class DeleteServiceCommand : ICommand
{
    public Guid ServiceId { get; set; }
}