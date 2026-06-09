using System.Threading.Channels;

using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Infrastructure.Deployment.Events;

using Mediator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment;

public class ContainerMonitoringJobService : IHostedService
{
    private readonly IDockerClient _dockerClient;
    private readonly IDockerEventParser _eventParser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContainerMonitoringJobService> _logger;
    private readonly Channel<DockerEvent> _eventChannel;
    private CancellationTokenSource _cts;
    private Task _monitorTask;
    private Task _processorTask;

    public ContainerMonitoringJobService(
        IDockerClient dockerClient,
        IDockerEventParser eventParser,
        ILogger<ContainerMonitoringJobService> logger, IServiceScopeFactory scopeFactory)
    {
        _dockerClient = dockerClient;
        _eventParser = eventParser;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _eventChannel = Channel.CreateUnbounded<DockerEvent>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = MonitorAsync(_cts.Token);
        _processorTask = ProcessEventsAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task MonitorAsync(CancellationToken ct)
    {
        try
        {
            var parameters = new ContainerEventsParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "label", new Dictionary<string, bool>
                        {
                            { "haven.managed=true", true }
                        }
                    }
                }
            };

            var progress = new Progress<Message>(async msg =>
            {
                if (await _eventParser.ParseAsync(msg, ct) is { } @event)
                {
                    _eventChannel.Writer.TryWrite(@event);
                }
            });

            await _dockerClient.System.MonitorEventsAsync(parameters, progress, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Docker event monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring Docker events");
            throw;
        }
        finally
        {
            _eventChannel.Writer.TryComplete();
        }
    }

    private async Task ProcessEventsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var @event in _eventChannel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    _logger.LogInformation("Processing Docker event: {EventType} for container {ContainerId}",
                        @event.GetType().Name, @event.ContainerId);
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Publish(@event, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Docker event {EventType} for container {ContainerId}",
                        @event.GetType().Name, @event.ContainerId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Docker event processor cancelled");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _eventChannel.Writer.TryComplete();

        var allTasks = new[] { _monitorTask, _processorTask }.Where(t => t != null).ToArray();
        try
        {
            await Task.WhenAll(allTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Container monitoring service stopped");
        }
    }
}