using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Jobs.Commands.TriggerJob;

public class TriggerJobHandler(IJobsService service) : ICommandHandler<TriggerJobCommand>
{
    public async ValueTask<Result> Handle(TriggerJobCommand command, CancellationToken cancellationToken)
    {
        return await service.TriggerJobAsync(command.JobKey, cancellationToken);
    }
}