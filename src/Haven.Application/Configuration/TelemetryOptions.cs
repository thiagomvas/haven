namespace Haven.Application.Configuration;

public enum OtlpProtocol
{
    Grpc,
    HttpProtobuf
}

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";
    public bool Enabled { get; set; } = false;
    public string OtlpEndpoint { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public OtlpProtocol Protocol { get; set; } = OtlpProtocol.HttpProtobuf;
}