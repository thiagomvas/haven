using FastEndpoints;
using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Responses;

namespace Haven.Presentation.Api.Extensions;

public static class ResultExtensions
{
    public static async Task SendResultAsync<T>(
        this IEndpoint ep,
        Result<T> result,
        CancellationToken ct = default)
    {
        var response = ApiResponse<T>.FromResult(result);
        await ep.HttpContext.Response.SendAsync(
            response,
            result.IsSuccess ? result.StatusCode : result.Error.ToStatusCode(),
            cancellation: ct);
    }

    public static async Task SendResultAsync(
        this IEndpoint ep,
        Result result,
        CancellationToken ct = default)
    {
        var response = ApiResponse.FromResult(result);
        await ep.HttpContext.Response.SendAsync(
            response,
            result.IsSuccess ? result.StatusCode : result.Error.ToStatusCode(),
            cancellation: ct);
    }
    
    
    public static async Task SendResultAsync<T>(
        this IEndpoint ep,
        PagedResult<T> result,
        CancellationToken ct = default)
    {
        await ep.HttpContext.Response.SendAsync(
            result,
            result.TotalCount > 0 ? 200 : 204,
            cancellation: ct);
    }
    
}