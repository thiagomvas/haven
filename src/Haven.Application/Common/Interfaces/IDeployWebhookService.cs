namespace Haven.Application.Common.Interfaces;

public interface IDeployWebhookService
{
    Task<Result> TryEnqueueWithTokenAsync(string token, CancellationToken ct);

}