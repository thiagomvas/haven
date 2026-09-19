namespace Haven.Domain.Enums;

public enum HealthCheckFailureReason
{
    None,
    Timeout,
    ConnectionRefused,
    DnsFailure,
    TlsError,
    UnexpectedStatusCode,
    UnexpectedExitCode,
    ContainerNotFound,
    ContainerNotRunning,
    ContainerUnhealthy,
    InvalidConfig,
    ProbeUnavailable,
    Error
}
