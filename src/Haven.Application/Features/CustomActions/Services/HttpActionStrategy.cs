using System.Net.Http.Headers;
using System.Text;

using Haven.Application.Common;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.CustomActions.Services;

public sealed class HttpActionStrategy(HttpClient httpClient, ILogger<HttpActionStrategy> logger) : IActionStrategy
{
    public bool CanHandle(CustomAction action) => action.Config is HttpActionConfig;

    public async Task<Result> ExecuteAsync(CustomAction action, CancellationToken ct)
    {
        if (action.Config is not HttpActionConfig config)
            return Error.NotSupported;

        if (!Uri.TryCreate(config.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Error.Validation($"Invalid HTTP action URL '{config.Url}'.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (action.Timeout > TimeSpan.Zero)
            timeoutCts.CancelAfter(action.Timeout);

        using var request = BuildRequest(config, uri);

        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            var statusCode = (int)response.StatusCode;
            var isSuccess = config.SuccessStatusCodes is { Count: > 0 }
                ? config.SuccessStatusCodes.Contains(statusCode)
                : response.IsSuccessStatusCode;

            // Result derives IsSuccess from the status code, so a configured non-2xx success code is reported as 200.
            if (isSuccess)
                return Result.Success(response.IsSuccessStatusCode ? statusCode : 200);

            logger.LogWarning("HTTP action {ActionId} ({ActionName}) returned unexpected status {StatusCode}",
                action.Id, action.ActionName, statusCode);
            return Error.Failed with { Message = $"HTTP action returned unexpected status code {statusCode}." };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Error.CancelledOperation;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("HTTP action {ActionId} ({ActionName}) timed out after {Timeout}",
                action.Id, action.ActionName, action.Timeout);
            return Error.Failed with { Message = $"HTTP action timed out after {action.Timeout}." };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP action {ActionId} ({ActionName}) failed", action.Id, action.ActionName);
            return Error.Failed with { Message = $"HTTP action request failed: {ex.Message}" };
        }
    }

    private static HttpRequestMessage BuildRequest(HttpActionConfig config, Uri uri)
    {
        var request = new HttpRequestMessage(config.Method, uri);

        if (config.Body is not null)
            request.Content = new StringContent(config.Body, Encoding.UTF8, "application/json");

        foreach (var (name, value) in config.Headers)
        {
            if (request.Headers.TryAddWithoutValidation(name, value))
                continue;

            if (request.Content is null)
                continue;

            request.Content.Headers.Remove(name);
            request.Content.Headers.TryAddWithoutValidation(name, value);
        }

        return request;
    }
}