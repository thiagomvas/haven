using Haven.Application.Common;

namespace Haven.Presentation.Api.Extensions;

public static class ErrorExtensions
{
    public static int ToStatusCode(this Error error) => error.Code switch
    {
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "CONFLICT" => StatusCodes.Status409Conflict,
        "VALIDATION" => StatusCodes.Status422UnprocessableEntity,
        "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}