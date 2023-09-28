using Microsoft.AspNetCore.Mvc;

namespace SelfService.Infrastructure.Api;

public class CustomObjectResult : ObjectResult
{
    private CustomObjectResult(int statusCode, object error)
        : base(error)
    {
        StatusCode = statusCode;
    }

    [NonAction]
    public static CustomObjectResult InternalServerError(object error) =>
        new(StatusCodes.Status500InternalServerError, error);

    [NonAction]
    public static CustomObjectResult MethodNotAllowedError(object error) =>
        new(StatusCodes.Status405MethodNotAllowed, error);

    [NonAction]
    public static CustomObjectResult NotImplemented(object error) => new(StatusCodes.Status501NotImplemented, error);
}
