namespace Haven.Application.Features.Exporting;

public class ExportFormatFactory : IExportFormatFactory
{
    private readonly IEnumerable<IExportFormat> _exportFormats;

    public ExportFormatFactory(IEnumerable<IExportFormat> exportFormats)
    {
        _exportFormats = exportFormats;
    }

    public IExportFormat Create(ExportFormatType format) =>
        _exportFormats.FirstOrDefault(f => f.Format == format)
        ?? throw new ArgumentOutOfRangeException(nameof(format), format, "No exporter is registered for the requested export format.");
}