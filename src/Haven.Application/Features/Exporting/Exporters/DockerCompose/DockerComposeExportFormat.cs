using System.Text.RegularExpressions;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class DockerComposeExportFormat : IExportFormat
{
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEventEmitter(next => new PortMappingQuotingEmitter(next))
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public ExportFormatType Format => ExportFormatType.DockerCompose;

    public string ExportService(ServiceExportModel serviceModel) =>
        _serializer.Serialize(serviceModel.ToComposeFile());

    public string ExportEnvironment(IReadOnlyList<ServiceExportModel> services) =>
        _serializer.Serialize(services.ToComposeFile());

    /// <summary>
    /// Quotes <c>host:container</c> style scalars. Left plain, YAML 1.1 parsers can read values such as
    /// <c>22:22</c> as base-60 numbers, which is why the Compose spec recommends quoting port mappings.
    /// </summary>
    private sealed class PortMappingQuotingEmitter(IEventEmitter next) : ChainedEventEmitter(next)
    {
        private static readonly Regex PortMapping = new(@"^[0-9a-fA-F.:\[\]/a-z-]*[0-9]:[0-9][0-9a-z/-]*$", RegexOptions.Compiled);

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (eventInfo.Source.Value is string value && PortMapping.IsMatch(value))
            {
                eventInfo = new ScalarEventInfo(eventInfo.Source)
                {
                    Style = ScalarStyle.DoubleQuoted,
                    IsPlainImplicit = false,
                    IsQuotedImplicit = true
                };
            }

            base.Emit(eventInfo, emitter);
        }
    }
}
