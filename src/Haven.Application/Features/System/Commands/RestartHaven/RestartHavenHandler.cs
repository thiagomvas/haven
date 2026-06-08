using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Commands.RestartHaven;

public class RestartHavenHandler(IHavenRestartService restartService) : ICommandHandler<RestartHavenCommand>
{
    public ValueTask<Result> Handle(RestartHavenCommand command, CancellationToken cancellationToken)
    {
        restartService.Restart();
        return ValueTask.FromResult(Result.Success());
    }
}
