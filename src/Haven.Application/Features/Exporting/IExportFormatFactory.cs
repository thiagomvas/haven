namespace Haven.Application.Features.Exporting;

public interface IExportFormatFactory
{
    IExportFormat Create(ExportFormatType format);
}