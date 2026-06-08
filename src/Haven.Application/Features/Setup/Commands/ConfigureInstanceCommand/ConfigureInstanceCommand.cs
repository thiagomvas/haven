using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Setup.Commands.ConfigureInstanceCommand;

public class ConfigureInstanceCommand : ICommand
{
    public string InstanceName { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public TimeFormat TimeFormat { get; set; } = TimeFormat.Hour12;
}
