using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Setup.Commands.ConfigureNetworkCommand;

public class ConfigureNetworkCommand : ICommand
{
    public string? Domain { get; set; }
    public int? Port { get; set; }
    public bool EnableTls { get; set; }
}
