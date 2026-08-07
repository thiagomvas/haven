using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfigs;

public class GetNotificationChannelConfigsHandler(INotificationChannelConfigRepository repository)
    : IPagedQueryHandler<GetNotificationChannelConfigsQuery, NotificationChannelConfigDto>
{
    public async ValueTask<PagedResult<NotificationChannelConfigDto>> Handle(GetNotificationChannelConfigsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);
        return result.Project(c =>
        {
            var dto = c.ToDto();
            return c.Channel == NotificationChannel.Smtp
                ? dto with { Config = SmtpConfigJsonCodec.Mask(dto.Config) }
                : dto;
        });
    }
}