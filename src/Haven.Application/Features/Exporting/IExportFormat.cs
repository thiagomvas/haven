namespace Haven.Application.Features.Exporting;

public interface IExportFormat
{
    ExportFormatType Format { get; }

    string ExportService(ServiceExportModel serviceModel);

    string ExportEnvironment(IReadOnlyList<ServiceExportModel> services);
}