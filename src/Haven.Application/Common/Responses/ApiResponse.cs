namespace Haven.Application.Common.Responses;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null)
{
    public static ApiResponse<T> FromResult(Result<T> result) =>
        result.IsSuccess
            ? new ApiResponse<T>(true, result.Value)
            : new ApiResponse<T>(false, default, result.Error.Message);
}

public record ApiResponse(
    bool Success,
    string? Message = null)
{
    public static ApiResponse FromResult(Result result) =>
        result.IsSuccess
            ? new ApiResponse(true)
            : new ApiResponse(false, result.Error.Message);
}