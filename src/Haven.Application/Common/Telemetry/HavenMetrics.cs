using System.Diagnostics.Metrics;

namespace Haven.Application.Common.Telemetry;

public sealed class HavenMetrics : IDisposable
{
    public const string MeterName = "Haven";

    public const string TagService = "haven.service.name";
    public const string TagEnvironment = "haven.environment.name";
    public const string TagProject = "haven.project.name";
    public const string TagServiceType = "haven.service.type";
    public const string TagOperation = "haven.operation";
    public const string TagResult = "haven.result";

    private readonly Meter _meter;

    public Counter<long> DeploymentsStarted { get; }
    public Counter<long> DeploymentsSucceeded { get; }
    public Counter<long> DeploymentsFailed { get; }
    public Counter<long> DeploymentsCancelled { get; }
    public Histogram<double> DeploymentDurationSeconds { get; }

    public Counter<long> ServiceOperations { get; }
    public Histogram<double> ServiceOperationDurationSeconds { get; }
    
    public HavenMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        DeploymentsStarted = _meter.CreateCounter<long>(
            "haven.deployments.started",
            unit: "deployments",
            description: "Number of full deployments initiated");

        DeploymentsSucceeded = _meter.CreateCounter<long>(
            "haven.deployments.succeeded",
            unit: "deployments",
            description: "Number of full deployments that completed successfully");

        DeploymentsFailed = _meter.CreateCounter<long>(
            "haven.deployments.failed",
            unit: "deployments",
            description: "Number of full deployments that failed");

        DeploymentsCancelled = _meter.CreateCounter<long>(
            "haven.deployments.cancelled",
            unit: "deployments",
            description: "Number of full deployments that were cancelled");

        DeploymentDurationSeconds = _meter.CreateHistogram<double>(
            "haven.deployment.duration",
            unit: "s",
            description: "Elapsed time of a full deployment from start to completion");

        ServiceOperations = _meter.CreateCounter<long>(
            "haven.service_operations.total",
            unit: "operations",
            description: "Number of service lifecycle operations (start/stop/restart). Tag by haven.operation and haven.result");

        ServiceOperationDurationSeconds = _meter.CreateHistogram<double>(
            "haven.service_operation.duration",
            unit: "s",
            description: "Elapsed time of a service lifecycle operation (start/stop/restart)");
    }

    /// <summary>
    /// Creates an observable gauge backed by the provided value callback.
    /// The returned object must be kept alive for as long as the gauge should report values.
    /// </summary>
    public ObservableGauge<T> CreateGauge<T>(string name, Func<T> getValue, string? unit = null, string? description = null)
        where T : struct
        => _meter.CreateObservableGauge(name, getValue, unit, description);

    public void Dispose() => _meter.Dispose();
}
