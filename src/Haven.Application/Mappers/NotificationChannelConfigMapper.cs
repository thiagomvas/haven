using Haven.Application.Features.NotificationChannels;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class NotificationChannelConfigMapper
{
    [MapProperty(nameof(NotificationChannelConfig.NotificationRules), nameof(NotificationChannelConfigDto.RulesCount), Use = nameof(MapRulesCount))]
    public static partial NotificationChannelConfigDto ToDto(this NotificationChannelConfig config);

    private static int MapRulesCount(ICollection<NotificationRule> rules) => rules.Count;
}