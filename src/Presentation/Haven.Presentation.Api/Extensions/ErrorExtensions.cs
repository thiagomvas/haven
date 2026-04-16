using Haven.Application.Common;

namespace Haven.Presentation.Api.Extensions;

public static class ErrorExtensions
{
    public static int ToStatusCode(this Error error) => error.Code switch
    {
        "General.NotFound" => StatusCodes.Status404NotFound,
        "General.Conflict" => StatusCodes.Status409Conflict,
        "General.Validation" => StatusCodes.Status422UnprocessableEntity,
        "General.Unauthorized" => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}