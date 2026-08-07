using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfig;

public class GetNotificationChannelConfigHandler(INotificationChannelConfigRepository repository)
    : IQueryHandler<GetNotificationChannelConfigQuery, NotificationChannelConfigDto>
{
    public async ValueTask<Result<NotificationChannelConfigDto>> Handle(GetNotificationChannelConfigQuery query, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(NotificationChannelConfig), query.Id);

        var dto = config.ToDto();
        return config.Channel == NotificationChannel.Smtp
            ? dto with { Config = SmtpConfigJsonCodec.Mask(dto.Config) }
            : dto;
    }
}