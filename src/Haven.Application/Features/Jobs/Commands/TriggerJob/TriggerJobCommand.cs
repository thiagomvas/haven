using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Jobs.Commands.TriggerJob;

[RequirePermission(Permissions.Jobs.Trigger)]
public class TriggerJobCommand : ICommand
{
    public string JobKey { get; set; }

    public TriggerJobCommand(string jobKey)
    {
        JobKey = jobKey;
    }
}