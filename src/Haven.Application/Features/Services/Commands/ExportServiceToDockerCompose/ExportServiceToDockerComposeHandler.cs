using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Exporting;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.Commands.ExportServiceToDockerCompose;

public sealed class ExportServiceToDockerComposeHandler(IServiceRepository serviceRepository, IExportFormatFactory exportFormatFactory)
    : Common.Messaging.ICommandHandler<ExportServiceToDockerComposeCommand, string>
{
    public async ValueTask<Result<string>> Handle(ExportServiceToDockerComposeCommand request, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), request.ServiceId);

        var exportFormat = exportFormatFactory.Create(ExportFormatType.DockerCompose);
        var composeFile = exportFormat.ExportService(service.ToExportModel());

        return Result<string>.Success(composeFile);
    }
}
