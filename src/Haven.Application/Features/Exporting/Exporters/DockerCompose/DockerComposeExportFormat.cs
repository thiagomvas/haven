using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class DockerComposeExportFormat : IExportFormat
{
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public ExportFormatType Format => ExportFormatType.DockerCompose;

    public string ExportService(ServiceExportModel serviceModel) =>
        _serializer.Serialize(serviceModel.ToComposeFile());

    public string ExportEnvironment(IReadOnlyList<ServiceExportModel> services) =>
        _serializer.Serialize(services.ToComposeFile());
}